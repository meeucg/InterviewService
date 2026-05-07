using InterviewService.Core.Entities;

namespace InterviewService.Application.Abstractions.Repositories;

/// <summary>
/// Domain repository for immutable interview setups.
/// </summary>
public interface IInterviewSetupRepository : IRepository<InterviewSetup>;
