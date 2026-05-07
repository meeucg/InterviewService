using InterviewService.Application.Abstractions.Utilities;
using InterviewService.Infrastructure.Models;

namespace InterviewService.Infrastructure.Abstractions;

/// <summary>
/// Storage contract for archived interview DTOs kept in PostgreSQL.
/// </summary>
public interface IArchivedInterviewStorage : IUnitOfWork
{
    Task<PostgresInterviewDto?> GetAsync(Guid id, CancellationToken ct = default);

    Task ArchiveAsync(PostgresInterviewDto entity, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
