namespace InterviewService.Infrastructure.Models;

/// <summary>
/// PostgreSQL DTO for immutable interview setup state stored as JSONB.
/// </summary>
public sealed class PostgresInterviewSetupDto
{
    public Guid Id { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public List<PostgresInterviewDto> Interviews { get; set; } = [];
}
