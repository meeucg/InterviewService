using InterviewService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewService.Infrastructure.Data;

public sealed class InterviewServiceDbContext(DbContextOptions<InterviewServiceDbContext> options) : DbContext(options)
{
    public DbSet<PostgresInterviewDto> Interviews => Set<PostgresInterviewDto>();

    public DbSet<PostgresInterviewSetupDto> InterviewSetups => Set<PostgresInterviewSetupDto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var interviewSetup = modelBuilder.Entity<PostgresInterviewSetupDto>();
        interviewSetup.ToTable("interview_setups");
        interviewSetup.HasKey(x => x.Id);
        interviewSetup.Property(x => x.GroupName).HasMaxLength(128);
        interviewSetup.Property(x => x.PayloadJson).HasColumnType("jsonb");
        interviewSetup.HasIndex(x => x.GroupName);

        var interview = modelBuilder.Entity<PostgresInterviewDto>();
        interview.ToTable("interviews");
        interview.HasKey(x => x.Id);
        interview.Property(x => x.PayloadJson).HasColumnType("jsonb");
        interview.HasIndex(x => x.SetupId);
        interview
            .HasOne(x => x.Setup)
            .WithMany(x => x.Interviews)
            .HasForeignKey(x => x.SetupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
