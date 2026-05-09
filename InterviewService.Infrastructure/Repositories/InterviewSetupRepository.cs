using System.Text.Json;
using AutoMapper;
using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Core.Entities;
using InterviewService.Infrastructure.Models;
using InterviewService.Infrastructure.Models.Serialization;
using InterviewService.Infrastructure.Options;
using InterviewService.Infrastructure.Stores;
using Microsoft.Extensions.Options;

namespace InterviewService.Infrastructure.Repositories;

/// <summary>
/// Domain setup repository that keeps PostgreSQL as source of truth and warms Redis cache.
/// </summary>
public sealed class InterviewSetupRepository(
    RedisInterviewSetupStorage redisStorage,
    PostgresInterviewSetupStorage postgresStorage,
    IMapper mapper,
    IOptions<InfrastructureJsonOptions> jsonOptions) : IInterviewSetupRepository
{
    private readonly JsonSerializerOptions _serializerOptions = jsonOptions.Value.SerializerOptions;

    public async Task<InterviewSetup?> GetAsync(Guid hashGuid, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cachedDocument = await redisStorage.GetAsync(hashGuid, ct);
        if (cachedDocument is not null)
        {
            return CreateInterviewSetupFromRedisDocument(cachedDocument);
        }

        var storedDto = await postgresStorage.GetAsync(hashGuid, ct);
        if (storedDto is null)
        {
            return null;
        }

        var setup = DeserializeInterviewSetup(storedDto.HashGuid, storedDto.PayloadJson);
        await redisStorage.SetAsync(mapper.Map<RedisInterviewSetupDocument>(setup), ct);
        return setup;
    }

    public async Task SetAsync(InterviewSetup entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ct.ThrowIfCancellationRequested();

        await postgresStorage.SetAsync(CreatePostgresDto(entity), ct);
        await redisStorage.SetAsync(mapper.Map<RedisInterviewSetupDocument>(entity), ct);
    }

    public Task UpdateAsync(InterviewSetup entity, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Interview setups are immutable. Create a new setup payload to get a new setup hash GUID.");
    }

    public async Task DeleteAsync(Guid hashGuid, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await postgresStorage.DeleteAsync(hashGuid, ct);
        await redisStorage.DeleteAsync(hashGuid, ct);
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
        if (setup.HashGuid != document.HashGuid)
        {
            throw new InvalidOperationException(
                $"Interview setup '{document.HashGuid}' Redis payload hash does not match computed hash GUID '{setup.HashGuid}'.");
        }

        return setup;
    }

    private PostgresInterviewSetupDto CreatePostgresDto(InterviewSetup setup)
    {
        return new PostgresInterviewSetupDto
        {
            HashGuid = setup.HashGuid,
            GroupName = setup.GroupName,
            PayloadJson = JsonSerializer.Serialize(
                new InterviewSetupPayload
                {
                    GroupName = setup.GroupName,
                    RequiredQuestions = setup.RequiredQuestions.ToList(),
                },
                _serializerOptions),
        };
    }

    private InterviewSetup DeserializeInterviewSetup(Guid setupHashGuid, string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<InterviewSetupPayload>(payloadJson, _serializerOptions)
                      ?? throw new InvalidOperationException(
                          $"Interview setup '{setupHashGuid}' payload could not be deserialized.");

        var setup = new InterviewSetup(payload.GroupName, payload.RequiredQuestions);
        if (setup.HashGuid != setupHashGuid)
        {
            throw new InvalidOperationException(
                $"Interview setup '{setupHashGuid}' payload hash does not match computed hash GUID '{setup.HashGuid}'.");
        }

        return setup;
    }
}
