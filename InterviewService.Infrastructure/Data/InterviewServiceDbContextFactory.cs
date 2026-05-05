using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InterviewService.Infrastructure.Data;

public sealed class InterviewServiceDbContextFactory : IDesignTimeDbContextFactory<InterviewServiceDbContext>
{
    public InterviewServiceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                               ?? "Host=localhost;Port=5432;Database=fitflow_interviews;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<InterviewServiceDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new InterviewServiceDbContext(options);
    }
}
