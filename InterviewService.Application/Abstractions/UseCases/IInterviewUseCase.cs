using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Application.Abstractions.UseCases;

public interface IInterviewUseCase
{
    Task<Interview> CreateNewInterviewAsync(CancellationToken ct = default);

    Task SubmitAnswerAsync(Guid interviewId, Answer answer, CancellationToken ct = default);

    Task<FormElement?> GetNextStepAsync(Guid interviewId, CancellationToken ct = default);
}
