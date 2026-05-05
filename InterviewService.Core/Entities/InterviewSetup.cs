using InterviewService.Core.Invariants;
using InterviewService.Core.Models;

namespace InterviewService.Core.Entities;

public sealed class InterviewSetup
{
    public InterviewSetup(string groupName, IReadOnlyList<Question> requiredQuestions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentNullException.ThrowIfNull(requiredQuestions);

        GroupName = groupName.Trim();
        RequiredQuestions = requiredQuestions.ToArray();
        Id = InterviewSetupIdentity.ComputeId(GroupName, RequiredQuestions);
    }

    public Guid Id { get; }

    public string GroupName { get; }

    public IReadOnlyList<Question> RequiredQuestions { get; }
}
