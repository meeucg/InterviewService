using InterviewService.Application.Abstractions;
using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Infrastructure.Models;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace InterviewService.Infrastructure.Stores;

public sealed class RedisInterviewSetupStorage(IRedisConnectionProvider redisConnectionProvider)
    : IRepository<RedisInterviewSetupDocument>
{
    private readonly IRedisCollection<RedisInterviewSetupDocument> _setups =
        redisConnectionProvider.RedisCollection<RedisInterviewSetupDocument>(saveState: false);
    private readonly List<PendingRedisMutation> _pendingMutations = [];
    private bool _disposed;

    public async Task<RedisInterviewSetupDocument?> GetAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await _setups.FindByIdAsync(id.ToString("D"));
    }

    public Task SetAsync(RedisInterviewSetupDocument entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureNotDisposed();
        ct.ThrowIfCancellationRequested();

        _pendingMutations.Add(new PendingRedisMutation(
            _ => redisConnectionProvider.Connection.SetAsync(entity)));

        return Task.CompletedTask;
    }

    public Task UpdateAsync(RedisInterviewSetupDocument entity, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Interview setup cache updates are not allowed.");
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        EnsureNotDisposed();
        ct.ThrowIfCancellationRequested();

        _pendingMutations.Add(new PendingRedisMutation(
            async _ =>
            {
                var existing = await _setups.FindByIdAsync(id.ToString("D"));
                if (existing is not null)
                {
                    await _setups.DeleteAsync(existing);
                }
            }));

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        EnsureNotDisposed();

        for (var index = 0; index < _pendingMutations.Count;)
        {
            await _pendingMutations[index].ExecuteAsync(CancellationToken.None);
            _pendingMutations.RemoveAt(index);
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

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed record PendingRedisMutation(Func<CancellationToken, Task> ExecuteAsync);
}
