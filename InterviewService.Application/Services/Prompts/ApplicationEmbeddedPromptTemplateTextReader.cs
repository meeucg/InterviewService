using System.Collections.Concurrent;
using System.Reflection;
using InterviewService.Application.Abstractions.Prompts;

namespace InterviewService.Application.Services.Prompts;

/// <summary>
/// Reads embedded prompt template files from the Application assembly.
/// </summary>
public sealed class ApplicationEmbeddedPromptTemplateTextReader : IPromptTemplateTextReader
{
    private const string ResourceNamespacePrefix = "InterviewService.Application.Prompts.";
    private const string PromptFileExtension = ".txt";
    private readonly Assembly _assembly = typeof(ApplicationEmbeddedPromptTemplateTextReader).Assembly;
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);

    public async ValueTask<string> GetPromptTextAsync(string promptKey, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptKey);

        var normalizedPromptKey = NormalizePromptKey(promptKey);

        if (_cache.TryGetValue(normalizedPromptKey, out var promptText))
        {
            return promptText;
        }

        promptText = await ReadPromptTextAsync(normalizedPromptKey, ct).ConfigureAwait(false);
        _cache[normalizedPromptKey] = promptText;
        return promptText;
    }

    private async Task<string> ReadPromptTextAsync(string promptKey, CancellationToken ct)
    {
        var resourceName = $"{ResourceNamespacePrefix}{promptKey}{PromptFileExtension}";

        await using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new FileNotFoundException($"Prompt '{promptKey}' was not found in the Application assembly.");
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
    }

    private static string NormalizePromptKey(string promptKey)
    {
        var normalizedPromptKey = promptKey.Trim()
            .Replace('/', '.')
            .Replace('\\', '.');

        if (normalizedPromptKey.EndsWith(PromptFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            normalizedPromptKey = normalizedPromptKey[..^PromptFileExtension.Length];
        }

        return normalizedPromptKey;
    }
}
