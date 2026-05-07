using InterviewService.Application.Abstractions.Converters;

namespace InterviewService.Application.Services;

/// <summary>
/// Default implementation that indexes converters by setup group name.
/// </summary>
public sealed class InterviewAiConverterFactory(IEnumerable<IInterviewAiConverter> converters)
    : IInterviewAiConverterFactory
{
    private readonly IReadOnlyDictionary<string, IInterviewAiConverter> _converters = converters
        .ToDictionary(x => x.ForSetupGroupName(), StringComparer.OrdinalIgnoreCase);

    public IInterviewAiConverter GetConverterForSetupGroupName(string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        if (_converters.TryGetValue(groupName.Trim(), out var converter))
        {
            return converter;
        }

        throw new KeyNotFoundException($"Interview AI converter for setup group '{groupName}' was not found.");
    }
}
