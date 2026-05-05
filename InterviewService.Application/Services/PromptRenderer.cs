using InterviewService.Application.Abstractions.Prompts;
using InterviewService.Core.Abstractions;
using Scriban;

namespace InterviewService.Application.Services;

public sealed class PromptRenderer : IPromptRenderer
{
    public Task<string> RenderPromptAsync<T>(Prompt<T> prompt, CancellationToken ct) where T : PromptParameters
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ct.ThrowIfCancellationRequested();

        var template = Template.Parse(prompt.TextTemplate, prompt.PromptName);
        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                $"Prompt '{prompt.PromptName}' could not be parsed: {string.Join("; ", template.Messages)}");
        }

        return Task.FromResult(template.Render(prompt.PromptParameters, member => member.Name));
    }
}
