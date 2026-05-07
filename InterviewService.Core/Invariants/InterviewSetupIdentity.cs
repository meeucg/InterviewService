using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using InterviewService.Core.Models;

namespace InterviewService.Core.Invariants;

/// <summary>
/// Computes deterministic setup identifiers from canonical setup payloads.
/// </summary>
public static class InterviewSetupIdentity
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static InterviewSetupIdentity()
    {
        SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static Guid ComputeId(string groupName, IReadOnlyList<Question> requiredQuestions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentNullException.ThrowIfNull(requiredQuestions);

        var json = JsonSerializer.Serialize(
            new InterviewSetupPayload
            {
                GroupName = groupName.Trim(),
                RequiredQuestions = requiredQuestions.ToList(),
            },
            SerializerOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));

        return new Guid(hash[..16]);
    }

    private sealed record InterviewSetupPayload
    {
        public required string GroupName { get; init; }

        public List<Question> RequiredQuestions { get; init; } = [];
    }
}
