using Redis.OM.Modeling;
using InterviewService.Core.Models;

namespace InterviewService.Infrastructure.Models;

[Document(StorageType = StorageType.Json, Prefixes = ["interviews:v4:setup"])]
public sealed class RedisInterviewSetupDocument
{
    [RedisIdField]
    [Indexed]
    public Guid Id { get; set; }

    [Indexed]
    public string GroupName { get; set; } = string.Empty;

    public List<Question> RequiredQuestions { get; set; } = [];
}
