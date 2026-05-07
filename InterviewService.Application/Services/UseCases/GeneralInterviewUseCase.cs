using System.Text.Json;
using AIServices.Abstractions;
using AIServices.Entities;
using AIServices.Models;
using AIServices.Models.Options;
using InterviewService.Application.Abstractions.Prompts;
using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Application.Abstractions.Setups;
using InterviewService.Application.Abstractions.UseCases;
using InterviewService.Application.Abstractions.Utilities;
using InterviewService.Core.Abstractions;
using InterviewService.Core.Entities;
using InterviewService.Core.Models;
using Microsoft.Extensions.Options;

namespace InterviewService.Application.Services.UseCases;

/// <summary>
/// Use case for the general setup group, including prompt selection and AI next-step generation.
/// </summary>
public sealed class GeneralInterviewUseCase(
    IInterviewRepository interviewRepository,
    IInterviewSetupRepository interviewSetupRepository,
    IInterviewSetupCatalog setupCatalog,
    IInterviewLockProvider interviewLockProvider,
    ITextAI textAI,
    IPromptTemplateTextReader promptTemplateTextReader,
    IPromptRenderer promptRenderer,
    IJsonSchemaHelper jsonSchemaHelper,
    IOptions<AIJsonOptions> aiJsonOptions) : IInterviewUseCase
{
    private const string GeneralSetupGroupName = "general";
    private const string ItPromptName = "ItInterviewerPrompt";
    private const string DesignPromptName = "DesignInterviewerPrompt";
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(10);
    private readonly JsonSerializerOptions _jsonSerializerOptions = aiJsonOptions.Value.JsonSerializerOptions;

    public string ForGroupName => GeneralSetupGroupName;

    public async Task<Interview> CreateNewInterviewAsync(CancellationToken ct = default)
    {
        var setup = setupCatalog.GetByGroupName(ForGroupName);

        await interviewSetupRepository.SetAsync(setup, ct);
        await interviewSetupRepository.SaveChangesAsync();

        var interview = new Interview(Guid.NewGuid(), setup: setup);
        await interviewRepository.SetAsync(interview, ct);
        await interviewRepository.SaveChangesAsync();
        return interview;
    }

    public async Task SubmitAnswerAsync(Guid interviewId, Answer answer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(answer);

        await using var interviewLock = await interviewLockProvider.AcquireAsync(interviewId, ct);
        using var processingCts = new CancellationTokenSource(ProcessingTimeout);
        var processingToken = processingCts.Token;

        var interview = await interviewRepository.GetAsync(interviewId, processingToken);
        if (interview is null)
        {
            throw new KeyNotFoundException($"Interview '{interviewId}' was not found.");
        }

        if (!string.Equals(interview.Setup.GroupName, ForGroupName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Interview with setup group '{interview.Setup.GroupName}' is incompatible with use case '{ForGroupName}'.");
        }

        interview.AddAnswer(answer);

        if (interview is
            {
                AllRequiredQuestionsAnswered: true, 
                IsInterviewersTurn: true, 
                IsFinished: false
            })
        {
            var aiRequest = await BuildAiRequestAsync(interview, processingToken);
            var aiResponse = await textAI.CompleteChatTyped<FormElement>(aiRequest, processingToken);
            if (!aiResponse.IsSuccess || aiResponse.Response is null)
            {
                throw new InvalidOperationException("AI failure: FormElement response was unsuccessful or empty.");
            }

            var nextStep = aiResponse.Response;

            if (nextStep.IsQuestion)
            {
                interview.AddQuestion(
                    nextStep.Question
                    ?? throw new InvalidOperationException("Question payload is missing."));
            }
            else
            {
                interview.Conclude(
                    nextStep.UserProfile
                    ?? throw new InvalidOperationException("UserProfile payload is missing."));
            }
        }

        await interviewRepository.UpdateAsync(interview, processingToken);
        await interviewRepository.SaveChangesAsync();
    }

    public async Task<FormElement?> GetNextStepAsync(Guid interviewId, CancellationToken ct = default)
    {
        var interview = await interviewRepository.GetAsync(interviewId, ct);
        if (interview is null)
        {
            return null;
        }

        if (interview.Conclusion is not null)
        {
            return new FormElement
            {
                UserProfile = interview.Conclusion,
            };
        }

        if (interview.CurrentQuestion is not null)
        {
            return new FormElement
            {
                Question = interview.CurrentQuestion,
            };
        }

        return null;
    }

    private async Task<TextAIRequest> BuildAiRequestAsync(Interview interview, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(interview);
        ct.ThrowIfCancellationRequested();

        if (!interview.IsInterviewersTurn)
        {
            throw new InvalidOperationException("Cannot create questions in user's turn.");
        }

        if (interview.IsFinished)
        {
            throw new InvalidOperationException("Cannot create questions after interview is finished.");
        }

        if (!interview.AllRequiredQuestionsAnswered)
        {
            throw new InvalidOperationException("Cannot build an AI request before required questions are answered.");
        }

        var transcript = interview.GetTranscript();
        var promptName = ResolvePromptName(interview);
        var promptTemplate = await promptTemplateTextReader.GetPromptTextAsync(promptName, ct);
        var prompt = new GeneralInterviewPrompt
        {
            PromptName = promptName,
            TextTemplate = promptTemplate,
            PromptParameters = CreatePromptParameters(
                interview.Setup,
                BuildRequiredInterviewContext(interview, transcript)),
        };
        var renderedPrompt = await promptRenderer.RenderPromptAsync(prompt, ct);

        return new TextAIRequest
        {
            ChatContext = new Chat(
                chatPairs: BuildHistory(interview, transcript),
                chatInitialState: new ChatInitialState
                {
                    SystemPrompt = renderedPrompt,
                }),
            RequestText = JsonSerializer.Serialize(transcript[^1].Answer, _jsonSerializerOptions),
        };
    }

    private List<ChatPair> BuildHistory(Interview interview, IReadOnlyList<InterviewStep> transcript)
    {
        var history = new List<ChatPair>();
        for (var i = interview.Setup.RequiredQuestions.Count - 1; i < transcript.Count - 1; i++)
        {
            history.Add(new ChatPair
            {
                Request = JsonSerializer.Serialize(transcript[i].Answer, _jsonSerializerOptions),
                Response = JsonSerializer.Serialize(
                    new FormElement
                    {
                        Question = transcript[i + 1].Question,
                    },
                    _jsonSerializerOptions),
            });
        }

        return history;
    }

    private string BuildRequiredInterviewContext(Interview interview, IReadOnlyList<InterviewStep> transcript)
    {
        var requiredPart = string.Empty;
        var lastRequiredQuestionId = interview.Setup.RequiredQuestions.Count - 1;
        for (var i = 0; i < lastRequiredQuestionId; i++)
        {
            requiredPart += $"Ð’Ð¾Ð¿Ñ€Ð¾Ñ #{i + 1} :\n" +
                            $"{JsonSerializer.Serialize(transcript[i].Question, _jsonSerializerOptions)}\n\n" +
                            $"ÐžÑ‚Ð²ÐµÑ‚ : {JsonSerializer.Serialize(transcript[i].Answer, _jsonSerializerOptions)}\n\n";
        }

        requiredPart += $"Ð’Ð¾Ð¿Ñ€Ð¾Ñ #{lastRequiredQuestionId + 1} :\n" +
                        $"{JsonSerializer.Serialize(transcript[lastRequiredQuestionId].Question, _jsonSerializerOptions)}";

        return
            $"Ð’Ð¾Ñ‚ ÑƒÐ¶Ðµ Ð·Ð°Ð²ÐµÑ€ÑˆÐµÐ½Ð½Ð°Ñ Ñ‡Ð°ÑÑ‚ÑŒ Ð¸Ð½Ñ‚ÐµÑ€Ð²ÑŒÑŽ :\n{requiredPart}\n\n" +
            "Ð’ ÑÐ»ÐµÐ´ÑƒÑŽÑ‰ÐµÐ¼ ÑÐ¾Ð¾Ð±Ñ‰ÐµÐ½Ð¸Ð¸ Ð¿Ð¾Ð»ÑŒÐ·Ð¾Ð²Ð°Ñ‚ÐµÐ»ÑŒ Ð¾Ñ‚Ð²ÐµÑ‚Ð¸Ñ‚ Ð½Ð° Ð¿Ð¾ÑÐ»ÐµÐ´Ð½Ð¸Ð¹ Ð²Ð¾Ð¿Ñ€Ð¾Ñ.";
    }

    private GeneralInterviewPromptParameters CreatePromptParameters(
        InterviewSetup setup,
        string? requiredInterviewContext = null)
    {
        return new GeneralInterviewPromptParameters
        {
            InterviewSetup = setup,
            RequiredInterviewContext = requiredInterviewContext,
            AnswerScheme = jsonSchemaHelper.GetJsonScheme<Answer>(),
            FormElementScheme = jsonSchemaHelper.GetJsonScheme<FormElement>(),
        };
    }

    private static string ResolvePromptName(Interview interview)
    {
        var clusterQuestion = interview.Setup.RequiredQuestions.FirstOrDefault()
                              ?? throw new InvalidOperationException("Cluster question is missing from general setup.");
        var clusterAnswer = interview.RequiredAnswers.FirstOrDefault()
                            ?? throw new InvalidOperationException("Cluster answer is missing from general setup interview.");

        if (clusterAnswer.IsSkipped)
        {
            throw new InvalidOperationException("Cluster question cannot be skipped.");
        }

        if (clusterAnswer.SelectedOptions.Count != 1)
        {
            throw new InvalidOperationException("Cluster question must have exactly one selected option.");
        }

        var selectedOption = clusterAnswer.SelectedOptions[0];
        var selectedOptionText = selectedOption.OptionId >= 0 && selectedOption.OptionId < clusterQuestion.AnswerOptions.Count
            ? clusterQuestion.AnswerOptions[selectedOption.OptionId]
            : string.Empty;

        if (IsItCluster(selectedOption.OptionId, selectedOptionText))
        {
            return ItPromptName;
        }

        if (IsDesignCluster(selectedOption.OptionId, selectedOptionText))
        {
            return DesignPromptName;
        }

        throw new InvalidOperationException(
            $"Unsupported cluster answer option '{selectedOption.OptionId}' with text '{selectedOptionText}'.");
    }

    private static bool IsItCluster(int optionId, string optionText)
    {
        return optionId == 0 ||
               string.Equals(optionText.Trim(), "IT", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDesignCluster(int optionId, string optionText)
    {
        return optionId == 1 ||
               string.Equals(optionText.Trim(), "Design", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(optionText.Trim(), "Дизайн", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record GeneralInterviewPrompt : Prompt<GeneralInterviewPromptParameters>;

    private sealed record GeneralInterviewPromptParameters : PromptParameters
    {
        public required InterviewSetup InterviewSetup { get; init; }
        public required string FormElementScheme { get; init; }
        public required string AnswerScheme { get; init; }
        public string? RequiredInterviewContext { get; init; }
    }
}
