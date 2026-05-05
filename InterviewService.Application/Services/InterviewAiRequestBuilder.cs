using System.Text.Json;
using AIServices.Entities;
using AIServices.Models;
using AIServices.Models.Options;
using InterviewService.Application.Abstractions.Converters;
using InterviewService.Application.Abstractions.Prompts;
using InterviewService.Application.Options;
using InterviewService.Core.Entities;
using InterviewService.Core.Models;
using Microsoft.Extensions.Options;

namespace InterviewService.Application.Services;

public sealed class InterviewAiRequestBuilder(
    IPromptTemplateTextReader promptTemplateTextReader,
    IPromptRenderer promptRenderer,
    IInterviewPromptParametersFactory promptParametersFactory,
    IOptions<AIJsonOptions> aiJsonOptions,
    IOptions<InterviewPromptingOptions> promptingOptions) : IInterviewAiRequestBuilder
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = aiJsonOptions.Value.JsonSerializerOptions;
    private readonly InterviewPromptingOptions _promptingOptions = promptingOptions.Value;

    public async Task<TextAIRequest> BuildAsync(
        string promptName,
        Interview interview,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptName);
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
        var history = BuildHistory(interview, transcript);
        var comment = BuildComment(interview, transcript);
        var promptTemplate = await promptTemplateTextReader.GetPromptTextAsync(promptName, ct);
        var prompt = new InterviewPrompt
        {
            PromptName = promptName,
            TextTemplate = promptTemplate,
            PromptParameters = promptParametersFactory.CreateInterviewPromptParameters(interview.Setup, comment),
        };
        var renderedPrompt = await promptRenderer.RenderPromptAsync(prompt, ct);

        return new TextAIRequest
        {
            ChatContext = new Chat(
                chatPairs: history,
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

    private string BuildComment(Interview interview, IReadOnlyList<InterviewStep> transcript)
    {
        var requiredPartComment = BuildRequiredPartComment(interview, transcript);
        var variantComment = BuildVariantComment();

        return string.IsNullOrWhiteSpace(variantComment)
            ? requiredPartComment
            : $"{requiredPartComment}\n\n{variantComment}";
    }

    private string BuildRequiredPartComment(Interview interview, IReadOnlyList<InterviewStep> transcript)
    {
        var requiredPart = string.Empty;
        var lastRequiredQuestionId = interview.Setup.RequiredQuestions.Count - 1;
        for (var i = 0; i < lastRequiredQuestionId; i++)
        {
            requiredPart += $"Вопрос #{i + 1} :\n" +
                            $"{JsonSerializer.Serialize(transcript[i].Question, _jsonSerializerOptions)}\n\n" +
                            $"Ответ : {JsonSerializer.Serialize(transcript[i].Answer, _jsonSerializerOptions)}\n\n";
        }

        requiredPart += $"Вопрос #{lastRequiredQuestionId + 1} :\n" +
                        $"{JsonSerializer.Serialize(transcript[lastRequiredQuestionId].Question, _jsonSerializerOptions)}";

        return
            $"Вот уже завершенная часть интервью :\n{requiredPart}\n\n" +
            "В следующем сообщении пользователь ответит на последний вопрос.";
    }

    private string BuildVariantComment()
    {
        return _promptingOptions.Variant.Trim() switch
        {
            PromptVariants.Evidence3 => """
                                        Экспериментальный режим интервью: Evidence3.
                                        Не возвращай UserProfile слишком рано. После обязательных вопросов задай минимум 3 AI-сгенерированных уточняющих вопроса, если только пользователь явно не отказывается отвечать или данные уже предельно однозначны.
                                        Каждый уточняющий вопрос должен закрывать один из недостающих блоков профиля: специализация, реальные задачи, skills, tools, domains, смежные компетенции.
                                        Перед финальным UserProfile проверь, что профиль основан на явных ответах пользователя, а не на догадках по профессии.
                                        """,
            PromptVariants.Lean2 => """
                                     Экспериментальный режим интервью: Lean2.
                                     Стремись к короткому, но не поверхностному интервью: обычно достаточно 2-3 AI-сгенерированных уточняющих вопросов после обязательных вопросов.
                                     Каждый вопрос должен быть высокоинформативным и может собирать несколько связанных сигналов сразу, например задачи + инструменты или домены + смежные компетенции.
                                     Не завершай интервью, пока не получены явные сигналы по специализации, skills и tools. Domain-сигнал желательно получить, но не растягивай интервью только ради него.
                                     """,
            _ => string.Empty,
        };
    }
}
