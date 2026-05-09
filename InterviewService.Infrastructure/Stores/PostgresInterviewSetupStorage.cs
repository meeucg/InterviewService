using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Infrastructure.Data;
using InterviewService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewService.Infrastructure.Stores;

/// <summary>
/// Scoped PostgreSQL storage unit for setup DTO persistence.
/// </summary>
public sealed class PostgresInterviewSetupStorage(
    InterviewServiceDbContext dbContext) : IRepository<PostgresInterviewSetupDto>
{
    private readonly List<PostgresInterviewSetupDto> _pendingSetups = [];
    private bool _disposed;
    private bool _hasPendingChanges;

    public async Task<PostgresInterviewSetupDto?> GetAsync(Guid hashGuid, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        return await dbContext.InterviewSetups
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.HashGuid == hashGuid, ct);
    }

    public Task SetAsync(PostgresInterviewSetupDto entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureNotDisposed();
        ct.ThrowIfCancellationRequested();

        _pendingSetups.Add(entity);
        _hasPendingChanges = true;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PostgresInterviewSetupDto entity, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Interview setup storage updates are not allowed.");
    }

    public async Task DeleteAsync(Guid hashGuid, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        var dto = await dbContext.InterviewSetups.SingleOrDefaultAsync(x => x.HashGuid == hashGuid, ct);
        if (dto is not null)
        {
            dbContext.InterviewSetups.Remove(dto);
            _hasPendingChanges = true;
        }
    }

    public async Task SaveChangesAsync()
    {
        EnsureNotDisposed();
        if (!_hasPendingChanges)
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (var setup in _pendingSetups)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                     INSERT INTO interview_setups (hash_guid, group_name, payload_json)
                     VALUES ({setup.HashGuid}, {setup.GroupName}, {setup.PayloadJson}::jsonb)
                     ON CONFLICT (hash_guid) DO NOTHING
                     """);
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            _pendingSetups.Clear();
            _hasPendingChanges = false;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_hasPendingChanges)
        {
            await SaveChangesAsync();
        }

        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

}
