using InterviewService.Core.Models;

namespace InterviewService.Infrastructure.Models.Serialization;

/// <summary>
/// JSON payload shape for persisted immutable interview setup state.
/// </summary>
internal sealed record InterviewSetupPayload
{
    public required string GroupName { get; init; }

    public List<Question> RequiredQuestions { get; init; } = [];
}
