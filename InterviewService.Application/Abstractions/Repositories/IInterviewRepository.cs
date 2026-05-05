using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Application.Abstractions.Repositories;

public interface IInterviewRepository : IRepository<Interview>
{
    Task<UserProfile?> GetInterviewConclusionAsync(Guid id, CancellationToken ct = default);
}
