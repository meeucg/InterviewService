namespace InterviewService.Core.Models;

public record InterviewStep
{
    public required Question Question { get; init; }
    public required Answer Answer { get; init; }
}

