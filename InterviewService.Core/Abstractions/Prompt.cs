namespace InterviewService.Core.Abstractions;

/// <summary>
/// Base type for strongly typed prompt definitions used by interview AI request construction.
/// </summary>
public abstract record Prompt<T> where T : PromptParameters
{
    public required string PromptName { get; init; }
    public required string TextTemplate { get; init; }
    public required T PromptParameters { get; init; }
}
