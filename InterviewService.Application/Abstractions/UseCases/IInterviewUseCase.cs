using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Application.Abstractions.UseCases;

/// <summary>
/// Application facade for one interview setup group, covering creation, answer submission, and next-step lookup.
/// </summary>
public interface IInterviewUseCase
{
    /// <summary>
    /// Setup group name handled by this use case.
    /// </summary>
    string ForGroupName { get; }

    Task<Interview> CreateNewInterviewAsync(CancellationToken ct = default);

    Task SubmitAnswerAsync(Guid interviewId, Answer answer, CancellationToken ct = default);

    Task<FormElement?> GetNextStepAsync(Guid interviewId, CancellationToken ct = default);
}
