using InterviewService.Core.Models;

namespace InterviewService.Infrastructure.Models.Serialization;

/// <summary>
/// JSON payload shape for persisted interview aggregate state.
/// </summary>
internal sealed record InterviewPayload
{
    public List<Answer> RequiredAnswers { get; init; } = [];

    public List<InterviewStep> CompletedDynamicSteps { get; init; } = [];

    public Question? CurrentQuestion { get; init; }

    public UserProfile? Conclusion { get; init; }
}
