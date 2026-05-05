using AutoMapper;
using InterviewService.Application.Abstractions;
using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Core.Entities;
using InterviewService.Infrastructure.Models;
using InterviewService.Infrastructure.Serialization;
using InterviewService.Infrastructure.Stores;

namespace InterviewService.Infrastructure.Repositories;

public sealed class InterviewSetupRepository(
    RedisInterviewSetupStorage redisStorage,
    PostgresInterviewSetupStorage postgresStorage,
    IMapper mapper) : IInterviewSetupRepository
{
    public async Task<InterviewSetup?> GetAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cachedDocument = await redisStorage.GetAsync(id, ct);
        if (cachedDocument is not null)
        {
            return CreateInterviewSetupFromRedisDocument(cachedDocument);
        }

        var storedDto = await postgresStorage.GetAsync(id, ct);
        if (storedDto is null)
        {
            return null;
        }

        var setup = InterviewPersistencePayloadSerializer.DeserializeInterviewSetup(
            storedDto.Id,
            storedDto.PayloadJson);
        await redisStorage.SetAsync(mapper.Map<RedisInterviewSetupDocument>(setup), ct);
        return setup;
    }

    public async Task SetAsync(InterviewSetup entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ct.ThrowIfCancellationRequested();

        await postgresStorage.UpsertAsync(mapper.Map<PostgresInterviewSetupDto>(entity), ct);
        await redisStorage.SetAsync(mapper.Map<RedisInterviewSetupDocument>(entity), ct);
    }

    public Task UpdateAsync(InterviewSetup entity, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Interview setups are immutable. Create a new setup payload to get a new setup id.");
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await postgresStorage.DeleteAsync(id, ct);
        await redisStorage.DeleteAsync(id, ct);
    }

    public async Task SaveChangesAsync()
    {
        await postgresStorage.SaveChangesAsync();
        await redisStorage.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await postgresStorage.DisposeAsync();
        await redisStorage.DisposeAsync();
    }

    private static InterviewSetup CreateInterviewSetupFromRedisDocument(RedisInterviewSetupDocument document)
    {
        var setup = new InterviewSetup(document.GroupName, document.RequiredQuestions);
        if (setup.Id != document.Id)
        {
            throw new InvalidOperationException(
                $"Interview setup '{document.Id}' Redis payload hash does not match computed id '{setup.Id}'.");
        }

        return setup;
    }
}
