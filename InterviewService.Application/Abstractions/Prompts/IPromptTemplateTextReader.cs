namespace InterviewService.Application.Abstractions.Prompts;

public interface IPromptTemplateTextReader
{
    ValueTask<string> GetPromptTextAsync(string promptKey, CancellationToken ct);
}
