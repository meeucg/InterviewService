using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Application.Setups;
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
    IInterviewSetupRepository setupRepository)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await dbContext.Database.MigrateAsync(ct);

        await EnsureRedisIndexAsync(typeof(RedisInterviewDocument));
        await EnsureRedisIndexAsync(typeof(RedisInterviewSetupDocument));

        foreach (var setup in InterviewSetupCatalog.RequiredSetups)
        {
            try
            {
                await setupRepository.SetAsync(setup, ct);
            }
            catch (InvalidOperationException)
            {
                //ignore
            }
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
            //ignore
        }

        await redisConnectionProvider.Connection.CreateIndexAsync(documentType);
    }
}
