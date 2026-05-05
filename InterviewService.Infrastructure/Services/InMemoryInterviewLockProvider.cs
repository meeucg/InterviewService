using System.Collections.Concurrent;
using InterviewService.Application.Abstractions;
using InterviewService.Application.Abstractions.Utilities;

namespace InterviewService.Infrastructure.Services;

public sealed class InMemoryInterviewLockProvider : IInterviewLockProvider
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async ValueTask<IAsyncDisposable> AcquireAsync(Guid interviewId, CancellationToken ct = default)
    {
        var semaphore = _locks.GetOrAdd(interviewId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
