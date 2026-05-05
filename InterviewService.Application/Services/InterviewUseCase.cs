using AIServices.Abstractions;
using InterviewService.Application.Abstractions;
using InterviewService.Application.Abstractions.Converters;
using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Application.Abstractions.UseCases;
using InterviewService.Application.Abstractions.Utilities;
using InterviewService.Application.Options;
using InterviewService.Application.Setups;
using InterviewService.Core.Entities;
using InterviewService.Core.Models;
using Microsoft.Extensions.Options;

namespace InterviewService.Application.Services;

public sealed class InterviewUseCase(
    IInterviewRepository interviewRepository,
    IInterviewSetupRepository interviewSetupRepository,
    IInterviewAiConverterFactory interviewAiConverterFactory,
    IInterviewLockProvider interviewLockProvider,
    ITextAI textAI,
    IOptions<InterviewPromptingOptions> promptingOptions) : IInterviewUseCase
{
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(10);
    private const int AiFormElementAttempts = 5;
    private readonly InterviewPromptingOptions _promptingOptions = promptingOptions.Value;

    public async Task<Interview> CreateNewInterviewAsync(CancellationToken ct = default)
    {
        var setup = InterviewSetupCatalog.Default;

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

        interview.AddAnswer(answer);

        if (interview is
            {
                AllRequiredQuestionsAnswered: true, 
                IsInterviewersTurn: true, 
                IsFinished: false
            })
        {
            var converter = interviewAiConverterFactory.GetConverterForSetupGroupName(interview.Setup.GroupName);
            var aiRequest = await converter.ConvertToAiRequest(interview, processingToken);
            var requireQuestion = interview.CompletedDynamicSteps.Count <
                                  _promptingOptions.GetMinimumDynamicAnswersBeforeConclusion();
            var nextStep = await GetValidAiFormElementAsync(aiRequest, requireQuestion, processingToken);

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

    private async Task<FormElement> GetValidAiFormElementAsync(
        AIServices.Models.TextAIRequest aiRequest,
        bool requireQuestion,
        CancellationToken ct)
    {
        var request = requireQuestion
            ? aiRequest with
            {
                RequestText = aiRequest.RequestText +
                              "\n\nСистемное ограничение текущего шага: финальный UserProfile пока запрещен. Верни только FormElement с Question, чтобы собрать больше данных перед завершением интервью."
            }
            : aiRequest;

        for (var attempt = 1; attempt <= AiFormElementAttempts; attempt++)
        {
            var aiResponse = await textAI.CompleteChatTyped<FormElement>(request, ct);
            if (!aiResponse.IsSuccess || aiResponse.Response is null)
            {
                ct.ThrowIfCancellationRequested();
                continue;
            }

            if (aiResponse.Response.Question is not null)
            {
                return aiResponse.Response;
            }

            if (!requireQuestion && aiResponse.Response.UserProfile is not null)
            {
                return aiResponse.Response;
            }
        }

        throw new InvalidOperationException("AI failure: FormElement response did not contain a Question or UserProfile.");
    }
}
