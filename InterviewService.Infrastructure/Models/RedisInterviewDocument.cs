using Redis.OM.Modeling;
using InterviewService.Core.Models;

namespace InterviewService.Infrastructure.Models;

[Document(StorageType = StorageType.Json, Prefixes = ["interviews:v4:active"])]
public sealed class RedisInterviewDocument
{
    [RedisIdField]
    [Indexed]
    public Guid Id { get; set; }

    [Indexed]
    public Guid SetupId { get; set; }

    public List<Answer> RequiredAnswers { get; set; } = [];

    public List<InterviewStep> CompletedDynamicSteps { get; set; } = [];

    public Question? CurrentQuestion { get; set; }

    public UserProfile? Conclusion { get; set; }

    [Indexed]
    public long LastTouchedAt { get; set; }

    [Indexed]
    public bool IsFinished { get; set; }
}
