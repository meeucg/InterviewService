using AutoMapper;
using InterviewService.Application.Abstractions;
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
    private bool _disposed;
    private bool _hasPendingChanges;

    public async Task<PostgresInterviewSetupDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        return await dbContext.InterviewSetups
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task SetAsync(PostgresInterviewSetupDto entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureNotDisposed();

        await dbContext.InterviewSetups.AddAsync(entity, ct);
        _hasPendingChanges = true;
    }

    public Task UpdateAsync(PostgresInterviewSetupDto entity, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Interview setup storage updates are not allowed.");
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        var dto = await dbContext.InterviewSetups.SingleOrDefaultAsync(x => x.Id == id, ct);
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

        await dbContext.SaveChangesAsync();
        _hasPendingChanges = false;
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
