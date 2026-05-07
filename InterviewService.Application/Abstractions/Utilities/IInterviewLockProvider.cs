namespace InterviewService.Application.Abstractions.Utilities;

/// <summary>
/// Provides per-interview asynchronous locks for atomic answer processing and archiving.
/// </summary>
public interface IInterviewLockProvider
{
    ValueTask<IAsyncDisposable> AcquireAsync(Guid interviewId, CancellationToken ct = default);
}
