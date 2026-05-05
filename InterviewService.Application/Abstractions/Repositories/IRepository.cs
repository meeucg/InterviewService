using InterviewService.Application.Abstractions.Utilities;

namespace InterviewService.Application.Abstractions.Repositories;

public interface IRepository<T> : IUnitOfWork
{
    Task<T?> GetAsync(Guid id, CancellationToken ct = default);

    Task SetAsync(T entity, CancellationToken ct = default);

    Task UpdateAsync(T entity, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
