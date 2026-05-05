namespace InterviewService.Infrastructure.Options;

public sealed class InterviewArchivingOptions
{
    public const string SectionName = "Archiving";

    public TimeSpan InactiveAfter { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromMinutes(5);

    public int BatchSize { get; set; } = 100;
}
