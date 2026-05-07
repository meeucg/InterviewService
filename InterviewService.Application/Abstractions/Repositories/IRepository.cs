using InterviewService.Application.Abstractions.Utilities;

namespace InterviewService.Application.Abstractions.Repositories;

/// <summary>
/// Generic repository contract with unit-of-work commit semantics.
/// </summary>
public interface IRepository<T> : IUnitOfWork
{
    Task<T?> GetAsync(Guid id, CancellationToken ct = default);

    Task SetAsync(T entity, CancellationToken ct = default);

    Task UpdateAsync(T entity, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
