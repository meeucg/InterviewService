namespace InterviewService.Application.Abstractions.Prompts;

/// <summary>
/// Reads prompt template text by prompt name from the application prompt source.
/// </summary>
public interface IPromptTemplateTextReader
{
    ValueTask<string> GetPromptTextAsync(string promptKey, CancellationToken ct);
}
