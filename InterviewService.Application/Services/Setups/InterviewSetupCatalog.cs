using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using InterviewService.Application.Abstractions.Setups;
using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Application.Setups;

/// <summary>
/// Loads setup definitions from embedded application JSON.
/// </summary>
public sealed class InterviewSetupCatalog : IInterviewSetupCatalog
{
    private const string ResourceName = "InterviewService.Application.Services.Setups.interview-setups.json";

    private readonly Lazy<IReadOnlyDictionary<string, InterviewSetup>> _lazySetups = new(LoadSetups);

    public IReadOnlyDictionary<string, InterviewSetup> Setups => _lazySetups.Value;

    public InterviewSetup GetByGroupName(string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        if (Setups.TryGetValue(groupName.Trim(), out var setup))
        {
            return setup;
        }

        throw new KeyNotFoundException($"Interview setup group '{groupName}' was not found in catalog.");
    }

    private static Dictionary<string, InterviewSetup> LoadSetups()
    {
        var assembly = typeof(InterviewSetupCatalog).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
                         ?? throw new FileNotFoundException(
                             $"Embedded interview setup catalog '{ResourceName}' was not found.");

        var setupDefinitions = JsonSerializer.Deserialize<List<InterviewSetupDefinition>>(
                                   stream,
                                   CreateSerializerOptions())
                               ?? throw new InvalidOperationException("Interview setup catalog could not be deserialized.");

        if (setupDefinitions.Count == 0)
        {
            throw new InvalidOperationException("Interview setup catalog must contain at least one setup.");
        }

        return setupDefinitions
            .Select(x => new InterviewSetup(x.GroupName, x.RequiredQuestions))
            .ToDictionary(x => x.GroupName, StringComparer.OrdinalIgnoreCase);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed record InterviewSetupDefinition
    {
        public required string GroupName { get; init; }

        public required List<Question> RequiredQuestions { get; init; }
    }
}
