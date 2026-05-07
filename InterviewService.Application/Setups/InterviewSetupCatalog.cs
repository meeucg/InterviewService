using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Application.Setups;

/// <summary>
/// Loads required setup definitions from embedded application JSON and exposes the default setup.
/// </summary>
public static class InterviewSetupCatalog
{
    public const string GeneralGroupName = "general";
    private const string ResourceName = "InterviewService.Application.Setups.interview-setups.json";

    private static readonly Lazy<IReadOnlyDictionary<string, InterviewSetup>> LazySetups = new(LoadSetups);

    public static IReadOnlyDictionary<string, InterviewSetup> Setups => LazySetups.Value;

    public static InterviewSetup Default => GetByGroupName(GeneralGroupName);

    public static IReadOnlyCollection<InterviewSetup> RequiredSetups => Setups.Values.ToArray();

    public static InterviewSetup GetByGroupName(string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        if (Setups.TryGetValue(groupName.Trim(), out var setup))
        {
            return setup;
        }

        throw new KeyNotFoundException($"Interview setup group '{groupName}' was not found in catalog.");
    }

    private static IReadOnlyDictionary<string, InterviewSetup> LoadSetups()
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
