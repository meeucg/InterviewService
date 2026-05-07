using InterviewService.Core.Abstractions;

namespace InterviewService.Application.Abstractions.Prompts;

/// <summary>
/// Renders strongly typed prompt templates into final prompt text.
/// </summary>
public interface IPromptRenderer
{
    Task<string> RenderPromptAsync<T>(Prompt<T> prompt, CancellationToken ct) where T : PromptParameters;
}
