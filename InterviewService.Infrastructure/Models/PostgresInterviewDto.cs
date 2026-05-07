namespace InterviewService.Infrastructure.Models;

/// <summary>
/// PostgreSQL DTO for archived interview state stored as JSONB.
/// </summary>
public sealed class PostgresInterviewDto
{
    public Guid Id { get; set; }

    public Guid SetupId { get; set; }

    public PostgresInterviewSetupDto? Setup { get; set; }

    public string PayloadJson { get; set; } = string.Empty;
}
