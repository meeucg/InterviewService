using InterviewService.Core.Invariants;
using InterviewService.Core.Models;

namespace InterviewService.Core.Entities;

/// <summary>
/// Immutable interview setup version identified by a content hash GUID and grouped by a stable business name.
/// </summary>
public sealed class InterviewSetup
{
    public InterviewSetup(string groupName, IReadOnlyList<Question> requiredQuestions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentNullException.ThrowIfNull(requiredQuestions);

        GroupName = groupName.Trim();
        RequiredQuestions = requiredQuestions.ToArray();
        HashGuid = InterviewSetupIdentity.ComputeHashGuid(GroupName, RequiredQuestions);
    }

    public Guid HashGuid { get; }

    public string GroupName { get; }

    public IReadOnlyList<Question> RequiredQuestions { get; }
}
