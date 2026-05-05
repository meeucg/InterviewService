using InterviewService.Core.Abstractions;

namespace InterviewService.Application.Abstractions.Prompts;

public interface IPromptRenderer
{
    Task<string> RenderPromptAsync<T>(Prompt<T> prompt, CancellationToken ct) where T : PromptParameters;
}
