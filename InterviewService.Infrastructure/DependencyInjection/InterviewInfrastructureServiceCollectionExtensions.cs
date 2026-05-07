using InterviewService.Application.Abstractions;
using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Application.Abstractions.Utilities;
using InterviewService.Infrastructure.Abstractions;
using InterviewService.Infrastructure.Data;
using InterviewService.Infrastructure.Options;
using InterviewService.Infrastructure.Repositories;
using InterviewService.Infrastructure.Services;
using InterviewService.Infrastructure.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Redis.OM;
using Redis.OM.Contracts;

namespace InterviewService.Infrastructure.DependencyInjection;

/// <summary>
/// Registers infrastructure persistence, cache, mapping, startup, and archival services.
/// </summary>
public static class InterviewInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInterviewInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddDbContext<InterviewServiceDbContext>(
            options =>
            {
                var postgresConnectionString = configuration.GetConnectionString("Postgres")
                                             ?? throw new InvalidOperationException("Connection string 'Postgres' is missing.");
                options
                    .UseNpgsql(postgresConnectionString)
                    .UseSnakeCaseNamingConvention();
            });

        services.AddSingleton<IRedisConnectionProvider>(
            _ =>
            {
                var redisConnectionString = configuration.GetConnectionString("Redis")
                                          ?? throw new InvalidOperationException("Connection string 'Redis' is missing.");
                return new RedisConnectionProvider(NormalizeRedisConnectionString(redisConnectionString));
            });

        services.AddOptions<InterviewArchivingOptions>()
            .Bind(configuration.GetSection(InterviewArchivingOptions.SectionName));
        services.AddOptions<InfrastructureJsonOptions>();

        services.AddScoped<RedisInterviewStorage>();
        services.AddScoped<IActiveInterviewStorage>(provider => provider.GetRequiredService<RedisInterviewStorage>());
        services.AddScoped<RedisInterviewSetupStorage>();
        services.AddScoped<PostgresInterviewStorage>();
        services.AddScoped<IArchivedInterviewStorage>(provider => provider.GetRequiredService<PostgresInterviewStorage>());
        services.AddScoped<PostgresInterviewSetupStorage>();
        services.AddScoped<InfrastructureStartup>();
        services.AddScoped<IInterviewSetupRepository, InterviewSetupRepository>();
        services.AddScoped<IInterviewRepository, InterviewRepository>();
        services.AddSingleton<IInterviewLockProvider, InMemoryInterviewLockProvider>();
        services.AddHostedService<InterviewArchiver>();

        return services;
    }

    private static string NormalizeRedisConnectionString(string connectionString)
    {
        return connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
               || connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase)
            ? connectionString
            : $"redis://{connectionString}";
    }
}
