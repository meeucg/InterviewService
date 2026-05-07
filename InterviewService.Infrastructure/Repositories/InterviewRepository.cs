using System.Text.Json;
using AutoMapper;
using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Core.Entities;
using InterviewService.Core.Models;
using InterviewService.Infrastructure.Abstractions;
using InterviewService.Infrastructure.Models;
using InterviewService.Infrastructure.Models.Serialization;
using InterviewService.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace InterviewService.Infrastructure.Repositories;

/// <summary>
/// Domain interview repository that composes Redis active storage and PostgreSQL archive storage.
/// </summary>
public sealed class InterviewRepository(
    IActiveInterviewStorage redisStorage,
    IArchivedInterviewStorage postgresStorage,
    IInterviewSetupRepository interviewSetupRepository,
    IMapper mapper,
    IOptions<InfrastructureJsonOptions> jsonOptions,
    TimeProvider timeProvider) : IInterviewRepository
{
    private readonly JsonSerializerOptions _serializerOptions = jsonOptions.Value.SerializerOptions;

    public async Task<Interview?> GetAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cachedDocument = await redisStorage.GetAsync(id, ct);
        if (cachedDocument is not null)
        {
            return await CreateInterviewFromRedisDocumentAsync(cachedDocument, ct);
        }

        var storedDto = await postgresStorage.GetAsync(id, ct);
        if (storedDto is null)
        {
            return null;
        }

        var interview = CreateInterviewFromPostgresDto(storedDto);
        if (!interview.IsFinished)
        {
            var cacheDocument = mapper.Map<RedisInterviewDocument>(interview);
            cacheDocument.LastTouchedAt = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            await redisStorage.StoreAsync(cacheDocument, ct);
        }

        return interview;
    }

    public async Task SetAsync(Interview entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ct.ThrowIfCancellationRequested();

        if (await redisStorage.GetAsync(entity.Id, ct) is not null)
        {
            throw new InvalidOperationException($"Interview '{entity.Id}' already exists in Redis.");
        }

        if (await postgresStorage.GetAsync(entity.Id, ct) is not null)
        {
            throw new InvalidOperationException($"Interview '{entity.Id}' already exists in PostgreSQL.");
        }

        var document = mapper.Map<RedisInterviewDocument>(entity);
        document.LastTouchedAt = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        await redisStorage.StoreAsync(document, ct);
    }

    public async Task UpdateAsync(Interview entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ct.ThrowIfCancellationRequested();

        var existingRedisDocument = await redisStorage.GetAsync(entity.Id, ct);
        var existingPostgresDto = existingRedisDocument is null
            ? await postgresStorage.GetAsync(entity.Id, ct)
            : null;

        if (existingRedisDocument is null && existingPostgresDto is null)
        {
            throw new KeyNotFoundException($"Interview '{entity.Id}' was not found.");
        }

        var document = mapper.Map<RedisInterviewDocument>(entity);
        document.LastTouchedAt = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

        if (!entity.IsFinished)
        {
            await redisStorage.StoreAsync(document, ct);
            return;
        }

        var postgresDto = CreatePostgresDto(entity);
        await postgresStorage.ArchiveAsync(postgresDto, ct);
        await redisStorage.StoreAsync(document, ct);
        await redisStorage.QueueBestEffortDeleteAsync(entity.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await postgresStorage.DeleteAsync(id, ct);
        await redisStorage.DeleteAsync(id, ct);
    }

    public async Task<UserProfile?> GetInterviewConclusionAsync(Guid id, CancellationToken ct = default)
    {
        var interview = await GetAsync(id, ct);
        return interview?.Conclusion;
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

    //why this is not done with a mapper?
    private async Task<Interview> CreateInterviewFromRedisDocumentAsync(
        RedisInterviewDocument document,
        CancellationToken ct)
    {
        var setupId = document.SetupId;
        var setup = await interviewSetupRepository.GetAsync(setupId, ct)
                    ?? throw new KeyNotFoundException($"Interview setup '{setupId}' was not found.");

        return new Interview(
            document.Id,
            document.RequiredAnswers,
            document.CompletedDynamicSteps,
            document.CurrentQuestion,
            document.Conclusion,
            setup);
    }

    //why this is not done with a mapper?
    private Interview CreateInterviewFromPostgresDto(PostgresInterviewDto dto)
    {
        var storedSetup = dto.Setup is null
            ? throw new KeyNotFoundException($"Interview setup '{dto.SetupId}' was not loaded.")
            : DeserializeInterviewSetup(dto.Setup.Id, dto.Setup.PayloadJson);

        var payload = JsonSerializer.Deserialize<InterviewPayload>(dto.PayloadJson, _serializerOptions)
                      ?? throw new InvalidOperationException(
                          $"Interview '{dto.Id}' payload could not be deserialized.");

        return new Interview(
            dto.Id,
            payload.RequiredAnswers,
            payload.CompletedDynamicSteps,
            payload.CurrentQuestion,
            payload.Conclusion,
            storedSetup);
    }

    private PostgresInterviewDto CreatePostgresDto(Interview interview)
    {
        return new PostgresInterviewDto
        {
            Id = interview.Id,
            SetupId = interview.Setup.Id,
            PayloadJson = JsonSerializer.Serialize(
                new InterviewPayload
                {
                    RequiredAnswers = interview.RequiredAnswers.ToList(),
                    CompletedDynamicSteps = interview.CompletedDynamicSteps.ToList(),
                    CurrentQuestion = interview.CurrentQuestion,
                    Conclusion = interview.Conclusion,
                },
                _serializerOptions),
        };
    }

    private InterviewSetup DeserializeInterviewSetup(Guid setupId, string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<InterviewSetupPayload>(payloadJson, _serializerOptions)
                      ?? throw new InvalidOperationException(
                          $"Interview setup '{setupId}' payload could not be deserialized.");

        var setup = new InterviewSetup(payload.GroupName, payload.RequiredQuestions);
        if (setup.Id != setupId)
        {
            throw new InvalidOperationException(
                $"Interview setup '{setupId}' payload hash does not match computed id '{setup.Id}'.");
        }

        return setup;
    }

}
