using InterviewService.Application.Abstractions;
using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Infrastructure.Models;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace InterviewService.Infrastructure.Stores;

/// <summary>
/// Scoped Redis OM storage unit for active interview documents and archival queries.
/// </summary>
public sealed class RedisInterviewStorage(IRedisConnectionProvider redisConnectionProvider)
    : IRepository<RedisInterviewDocument>
{
    private readonly IRedisCollection<RedisInterviewDocument> _interviews =
        redisConnectionProvider.RedisCollection<RedisInterviewDocument>(saveState: false);
    private readonly List<PendingRedisMutation> _pendingMutations = [];
    private bool _disposed;

    public async Task<RedisInterviewDocument?> GetAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await _interviews.FindByIdAsync(id.ToString("D"));
    }

    public Task SetAsync(RedisInterviewDocument entity, CancellationToken ct = default)
    {
        return QueueSaveAsync(entity, ct);
    }

    public Task UpdateAsync(RedisInterviewDocument entity, CancellationToken ct = default)
    {
        return QueueSaveAsync(entity, ct);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return QueueDeleteAsync(id, ignoreFailure: false, ct);
    }

    public Task DeleteBestEffortAsync(Guid id, CancellationToken ct = default)
    {
        return QueueDeleteAsync(id, ignoreFailure: true, ct);
    }

    public async Task<IReadOnlyCollection<RedisInterviewDocument>> GetArchivableInterviewsAsync(
        DateTimeOffset staleBefore,
        int take,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var finishedDocuments = await _interviews
            .Where(x => x.IsFinished == true)
            .Take(take)
            .ToListAsync();

        if (finishedDocuments.Count >= take)
        {
            return finishedDocuments.ToArray();
        }

        var staleBeforeUnix = staleBefore.ToUnixTimeMilliseconds();
        var staleDocuments = await _interviews
            .Where(x => x.IsFinished == false)
            .Where(x => x.LastTouchedAt <= staleBeforeUnix)
            .Take(take - finishedDocuments.Count)
            .ToListAsync();

        return finishedDocuments
            .Concat(staleDocuments)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToArray();
    }

    public async Task SaveChangesAsync()
    {
        EnsureNotDisposed();

        for (var index = 0; index < _pendingMutations.Count;)
        {
            var pendingMutation = _pendingMutations[index];
            try
            {
                await pendingMutation.ExecuteAsync(CancellationToken.None);
                _pendingMutations.RemoveAt(index);
            }
            catch when (pendingMutation.IgnoreFailure)
            {
                _pendingMutations.RemoveAt(index);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await SaveChangesAsync();
        _disposed = true;
    }

    private Task QueueSaveAsync(RedisInterviewDocument entity, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureNotDisposed();
        ct.ThrowIfCancellationRequested();

        _pendingMutations.Add(new PendingRedisMutation(
            _ => redisConnectionProvider.Connection.SetAsync(entity),
            IgnoreFailure: false));

        return Task.CompletedTask;
    }

    private Task QueueDeleteAsync(Guid id, bool ignoreFailure, CancellationToken ct)
    {
        EnsureNotDisposed();
        ct.ThrowIfCancellationRequested();

        _pendingMutations.Add(new PendingRedisMutation(
            async _ =>
            {
                var existing = await _interviews.FindByIdAsync(id.ToString("D"));
                if (existing is not null)
                {
                    await _interviews.DeleteAsync(existing);
                }
            },
            ignoreFailure));

        return Task.CompletedTask;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed record PendingRedisMutation(
        Func<CancellationToken, Task> ExecuteAsync,
        bool IgnoreFailure);
}
