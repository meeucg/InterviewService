using InterviewService.Application.Abstractions.Utilities;
using InterviewService.Infrastructure.Models;

namespace InterviewService.Infrastructure.Abstractions;

/// <summary>
/// Storage contract for active interview documents kept in Redis.
/// </summary>
public interface IActiveInterviewStorage : IUnitOfWork
{
    Task<RedisInterviewDocument?> GetAsync(Guid id, CancellationToken ct = default);

    Task StoreAsync(RedisInterviewDocument entity, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task QueueBestEffortDeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyCollection<RedisInterviewDocument>> GetArchivableInterviewsAsync(
        DateTimeOffset staleBefore,
        int take,
        CancellationToken ct = default);
}
