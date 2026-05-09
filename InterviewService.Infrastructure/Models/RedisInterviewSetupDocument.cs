using Redis.OM.Modeling;
using InterviewService.Core.Models;

namespace InterviewService.Infrastructure.Models;

/// <summary>
/// Redis OM document for cached immutable interview setup state.
/// </summary>
[Document(StorageType = StorageType.Json, Prefixes = ["interviews:v5:setup"])]
public sealed class RedisInterviewSetupDocument
{
    [RedisIdField]
    [Indexed]
    public Guid HashGuid { get; set; }

    [Indexed]
    public string GroupName { get; set; } = string.Empty;

    public List<Question> RequiredQuestions { get; set; } = [];
}
