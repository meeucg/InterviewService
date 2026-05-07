using InterviewService.Core.Entities;

namespace InterviewService.Application.Abstractions.Setups;

/// <summary>
/// Provides interview setup definitions loaded by the application layer.
/// </summary>
public interface IInterviewSetupCatalog
{
    IReadOnlyDictionary<string, InterviewSetup> Setups { get; }

    InterviewSetup GetByGroupName(string groupName);
}
