using System.ComponentModel.DataAnnotations;

namespace InterviewService.Infrastructure.Models;

/// <summary>
/// PostgreSQL DTO for immutable interview setup state stored as JSONB.
/// </summary>
public sealed class PostgresInterviewSetupDto
{
    public Guid Id { get; set; }

    [MaxLength(256)]
    public string GroupName { get; set; } = string.Empty;

    [MaxLength(131072)]
    public string PayloadJson { get; set; } = string.Empty;

    public List<PostgresInterviewDto> Interviews { get; set; } = [];
}
