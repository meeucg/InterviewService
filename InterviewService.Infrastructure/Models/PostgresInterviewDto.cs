namespace InterviewService.Infrastructure.Models;

public sealed class PostgresInterviewDto
{
    public Guid Id { get; set; }

    public Guid SetupId { get; set; }

    public PostgresInterviewSetupDto? Setup { get; set; }

    public string PayloadJson { get; set; } = string.Empty;
}
