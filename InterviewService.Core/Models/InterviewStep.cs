namespace InterviewService.Core.Models;

/// <summary>
/// Pairs an interview question with the answer that was given for transcript construction.
/// </summary>
public record InterviewStep
{
    public required Question Question { get; init; }
    public required Answer Answer { get; init; }
}

