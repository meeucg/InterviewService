using System.Text.Json;
using InterviewService.Application.Abstractions.Utilities;
using InterviewService.Infrastructure.Abstractions;
using InterviewService.Infrastructure.Models;
using InterviewService.Infrastructure.Models.Serialization;
using InterviewService.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InterviewService.Infrastructure.Services;

/// <summary>
/// Hosted archival worker that moves stale or finished Redis interviews into PostgreSQL.
/// </summary>
public sealed class InterviewArchiver(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<InterviewArchivingOptions> archivingOptions,
    IOptions<InfrastructureJsonOptions> jsonOptions,
    TimeProvider timeProvider,
    ILogger<InterviewArchiver> logger) : IHostedService, IDisposable
{
    private readonly InterviewArchivingOptions _options = archivingOptions.Value;
    private readonly JsonSerializerOptions _serializerOptions = jsonOptions.Value.SerializerOptions;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _executionTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer = new PeriodicTimer(_options.SweepInterval);
        _executionTask = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is null || _executionTask is null)
        {
            return;
        }

        await _cts.CancelAsync();

        try
        {
            await _executionTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _timer?.Dispose();
    }

    private async Task RunAsync(CancellationToken ct)
    {
        if (_timer is null)
        {
            return;
        }

        while (await _timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await ArchiveStaleInterviewsAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to archive stale interviews.");
            }
        }
    }

    private async Task ArchiveStaleInterviewsAsync(CancellationToken ct)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var redisStorage = scope.ServiceProvider.GetRequiredService<IActiveInterviewStorage>();
        var postgresStorage = scope.ServiceProvider.GetRequiredService<IArchivedInterviewStorage>();
        var interviewLockProvider = scope.ServiceProvider.GetRequiredService<IInterviewLockProvider>();

        var staleBefore = timeProvider.GetUtcNow() - _options.InactiveAfter;
        var documents =
            await redisStorage.GetArchivableInterviewsAsync(staleBefore, _options.BatchSize, ct);

        foreach (var document in documents)
        {
            var interviewId = document.Id;
            await using var interviewLock = await interviewLockProvider.AcquireAsync(interviewId, ct);

            var currentDocument = await redisStorage.GetAsync(interviewId, ct);
            if (currentDocument is null)
            {
                continue;
            }

            var currentLastTouchedAt = DateTimeOffset.FromUnixTimeMilliseconds(currentDocument.LastTouchedAt);
            if (!currentDocument.IsFinished && currentLastTouchedAt > staleBefore)
            {
                continue;
            }

            await postgresStorage.ArchiveAsync(CreatePostgresDto(currentDocument), ct);
            await redisStorage.QueueBestEffortDeleteAsync(interviewId, ct);
            await postgresStorage.SaveChangesAsync();
            await redisStorage.SaveChangesAsync();
        }
    }

    private PostgresInterviewDto CreatePostgresDto(RedisInterviewDocument document)
    {
        return new PostgresInterviewDto
        {
            Id = document.Id,
            SetupHashGuid = document.SetupHashGuid,
            PayloadJson = JsonSerializer.Serialize(
                new InterviewPayload
                {
                    RequiredAnswers = document.RequiredAnswers,
                    CompletedDynamicSteps = document.CompletedDynamicSteps,
                    CurrentQuestion = document.CurrentQuestion,
                    Conclusion = document.Conclusion,
                },
                _serializerOptions),
        };
    }
}
