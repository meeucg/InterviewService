namespace InterviewService.Application.Abstractions.Utilities;

public interface IInterviewLockProvider
{
    ValueTask<IAsyncDisposable> AcquireAsync(Guid interviewId, CancellationToken ct = default);
}
