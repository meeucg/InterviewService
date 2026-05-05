namespace InterviewService.Core.Abstractions;

public abstract record Prompt<T> where T : PromptParameters
{
    public required string PromptName { get; init; }
    public required string TextTemplate { get; init; }
    public required T PromptParameters { get; init; }
}
