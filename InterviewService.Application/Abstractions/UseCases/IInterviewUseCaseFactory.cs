namespace InterviewService.Application.Abstractions.UseCases;

/// <summary>
/// Resolves interview use cases by setup group name and exposes the current default use case.
/// </summary>
public interface IInterviewUseCaseFactory
{
    IInterviewUseCase GetByGroupName(string groupName);

    IInterviewUseCase GetDefaultUseCase();
}
