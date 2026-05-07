using InterviewService.Application.Abstractions.UseCases;

namespace InterviewService.Application.Services.UseCases;

/// <summary>
/// Default implementation that indexes interview use cases by setup group name.
/// </summary>
public sealed class InterviewUseCaseFactory(IEnumerable<IInterviewUseCase> useCases) : IInterviewUseCaseFactory
{
    private const string DefaultGroupName = "general";

    private readonly IReadOnlyDictionary<string, IInterviewUseCase> _useCases = useCases
        .ToDictionary(x => x.ForGroupName, StringComparer.OrdinalIgnoreCase);

    public IInterviewUseCase GetByGroupName(string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        if (_useCases.TryGetValue(groupName.Trim(), out var useCase))
        {
            return useCase;
        }

        throw new KeyNotFoundException($"Interview use case for setup group '{groupName}' was not found.");
    }

    public IInterviewUseCase GetDefaultUseCase()
    {
        return GetByGroupName(DefaultGroupName);
    }
}
