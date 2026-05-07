using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Application.Abstractions.Repositories;

/// <summary>
/// Domain repository for interview aggregates with Redis-first and archive fallback behavior.
/// </summary>
public interface IInterviewRepository : IRepository<Interview>
{
    Task<UserProfile?> GetInterviewConclusionAsync(Guid id, CancellationToken ct = default);
}
