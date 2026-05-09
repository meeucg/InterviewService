using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Application.Abstractions.Setups;
using InterviewService.Infrastructure.Data;
using InterviewService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Redis.OM;
using Redis.OM.Contracts;

namespace InterviewService.Infrastructure.Services;

/// <summary>
/// Runs infrastructure startup tasks such as migrations, Redis index creation, and setup seeding.
/// </summary>
public sealed class InfrastructureStartup(
    InterviewServiceDbContext dbContext,
    IRedisConnectionProvider redisConnectionProvider,
    IInterviewSetupCatalog setupCatalog,
    IInterviewSetupRepository setupRepository)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await dbContext.Database.MigrateAsync(ct);

        await EnsureRedisIndexAsync(typeof(RedisInterviewDocument));
        await EnsureRedisIndexAsync(typeof(RedisInterviewSetupDocument));

        await SeedSetupsAndWarmCacheAsync(ct);
    }

    private async Task SeedSetupsAndWarmCacheAsync(CancellationToken ct)
    {
        foreach (var setup in setupCatalog.Setups.Values)
        {
            // Setups are immutable during this service lifetime. Persist them once on startup
            // and warm Redis so request handlers can treat the catalog as already available.
            await setupRepository.SetAsync(setup, ct);
        }

        await setupRepository.SaveChangesAsync();
    }

    private async Task EnsureRedisIndexAsync(Type documentType)
    {
        if (await redisConnectionProvider.Connection.IsIndexCurrentAsync(documentType))
        {
            return;
        }

        try
        {
            await redisConnectionProvider.Connection.DropIndexAsync(documentType);
        }
        catch
        {
            // The index may not exist yet; creation below is the desired end state.
        }

        await redisConnectionProvider.Connection.CreateIndexAsync(documentType);
    }
}
