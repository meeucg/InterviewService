using AutoMapper;
using InterviewService.Application.Abstractions;
using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Infrastructure.Data;
using InterviewService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewService.Infrastructure.Stores;

/// <summary>
/// Scoped PostgreSQL storage unit for archived interview DTO persistence.
/// </summary>
public sealed class PostgresInterviewStorage(
    InterviewServiceDbContext dbContext,
    IMapper mapper) : IRepository<PostgresInterviewDto>
{
    private bool _disposed;
    private bool _hasPendingChanges;

    public async Task<PostgresInterviewDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        return await dbContext.Interviews
            .Include(x => x.Setup)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task SetAsync(PostgresInterviewDto entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureNotDisposed();

        if (await dbContext.Interviews.AnyAsync(x => x.Id == entity.Id, ct))
        {
            throw new InvalidOperationException($"Interview '{entity.Id}' already exists in PostgreSQL.");
        }

        dbContext.Interviews.Add(entity);
        _hasPendingChanges = true;
    }

    public Task UpdateAsync(PostgresInterviewDto entity, CancellationToken ct = default)
    {
        return UpsertAsync(entity, ct);
    }

    public async Task UpsertAsync(PostgresInterviewDto entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureNotDisposed();

        var existing = await dbContext.Interviews
            .SingleOrDefaultAsync(x => x.Id == entity.Id, ct);

        if (existing is null)
        {
            dbContext.Interviews.Add(entity);
            _hasPendingChanges = true;
            return;
        }

        mapper.Map(entity, existing);
        _hasPendingChanges = true;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        var dto = await dbContext.Interviews.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (dto is not null)
        {
            dbContext.Interviews.Remove(dto);
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
