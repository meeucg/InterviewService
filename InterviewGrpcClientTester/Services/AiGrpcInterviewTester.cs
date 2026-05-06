using System.Text;
using System.Text.Json;
using System.Diagnostics;
using AIServices.Abstractions;
using AIServices.Entities;
using AIServices.Models;
using AIServices.Models.Options;
using AutoMapper;
using FitFlow.Interview.Grpc.Contracts;
using Grpc.Core;
using InterviewGrpcClientTester.Models;
using Microsoft.Extensions.Options;
using CoreAnswer = InterviewService.Core.Models.Answer;
using CoreFormElement = InterviewService.Core.Models.FormElement;
using CoreOptionAnswer = InterviewService.Core.Models.OptionAnswer;
using CoreQuestion = InterviewService.Core.Models.Question;
using CoreUserProfile = InterviewService.Core.Models.UserProfile;
using GrpcFormElement = FitFlow.Interview.Grpc.Contracts.FormElement;

namespace InterviewGrpcClientTester.Services;

public sealed class AiGrpcInterviewTester(
    InterviewGateway.InterviewGatewayClient interviewClient,
    ITextAI textAI,
    IServiceProvider serviceProvider,
    IJsonSchemaHelper jsonSchemaHelper,
    IOptions<AIJsonOptions> aiJsonOptions,
    IOptions<AiTestingOptions> aiTestingOptions,
    TesterPromptRenderer promptRenderer,
    IMapper mapper,
    ILogger<AiGrpcInterviewTester> logger)
{
    private readonly JsonSerializerOptions _jsonOptions = aiJsonOptions.Value.JsonSerializerOptions;
    private readonly AiTestingOptions _options = aiTestingOptions.Value;

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_options.ReportsPath);

        var scenarios = _options.GetScenarios();
        var maxParallelScenarios = _options.MaxParallelScenarios <= 0
            ? scenarios.Count
            : Math.Min(_options.MaxParallelScenarios, scenarios.Count);
        var failedScenarios = 0;

        logger.LogInformation(
            "Starting {scenarioCount} AI gRPC interview scenarios with max parallelism {maxParallelScenarios}",
            scenarios.Count,
            maxParallelScenarios);

        await Parallel.ForEachAsync(
            scenarios,
            new ParallelOptions
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = maxParallelScenarios,
            },
            async (scenario, scenarioCt) =>
            {
                try
                {
                    logger.LogInformation("Starting AI gRPC interview scenario '{scenario}'", scenario.Name);
                    await RunScenarioAsync(scenario, scenarioCt);
                    logger.LogInformation("AI gRPC interview scenario '{scenario}' completed", scenario.Name);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedScenarios);
                    logger.LogError(ex, "AI gRPC interview scenario '{scenario}' failed", scenario.Name);
                    await SaveFailureReportAsync(scenario, ex, CancellationToken.None);
                }
            });

        return failedScenarios == 0 ? 0 : 1;
    }

    private async Task RunScenarioAsync(AiTestScenarioOptions scenario, CancellationToken ct)
    {
        var answerSchema = jsonSchemaHelper.GetJsonScheme<CoreAnswer>();
        var formElementSchema = jsonSchemaHelper.GetJsonScheme<CoreFormElement>();
        var testerPrompt = await promptRenderer.RenderAsync(
            formElementSchema,
            answerSchema,
            scenario,
            ct);

        var testerProfile = await textAI.OneShotResponse(testerPrompt, ct);
        if (!testerProfile.IsSuccess || string.IsNullOrWhiteSpace(testerProfile.RawResponse))
        {
            throw new InvalidOperationException("Tester AI failed to create the simulated user profile.");
        }

        var testerChat = new Chat(chatInitialState: new ChatInitialState
        {
            SystemPrompt = testerPrompt,
            InitialAssistantMessage = testerProfile.RawResponse,
        });

        var interview = await CreateInterviewWithRetryAsync(ct);
        var interviewId = interview.Id;
        var requiredQuestionCount = interview.Setup.RequiredQuestions.Count;
        var transcript = new List<TranscriptEntry>();
        CoreUserProfile? finalProfile = null;

        var currentQuestion = interview.CurrentQuestion is null
            ? null
            : mapper.Map<CoreQuestion>(interview.CurrentQuestion);
        if (currentQuestion is null)
        {
            throw new InvalidOperationException("Created interview did not return a current question.");
        }

        for (var turn = 0; turn < _options.MaxTurns; turn++)
        {
            var answer = turn == 0
                ? CreateDeterministicClusterAnswer(scenario)
                : await AskTesterAiForAnswerAsync(testerChat, currentQuestion, ct);

            var questionFormElementJson = Serialize(new CoreFormElement
            {
                Question = currentQuestion,
            });
            var rawAnswer = Serialize(answer);
            testerChat.AddChatPair(new ChatPair
            {
                Request = questionFormElementJson,
                Response = rawAnswer,
            });

            var answerRpcStopwatch = Stopwatch.StartNew();
            var reply = await interviewClient.AnswerAsync(
                new AnswerRequest
                {
                    Id = interviewId,
                    Answer = mapper.Map<Answer>(answer),
                },
                cancellationToken: ct);
            answerRpcStopwatch.Stop();

            transcript.Add(new TranscriptEntry(
                turn + 1,
                currentQuestion,
                answer,
                rawAnswer,
                answerRpcStopwatch.Elapsed,
                TriggersAiInterviewerStep(turn, requiredQuestionCount)));

            if (reply.FormElement.PayloadCase == GrpcFormElement.PayloadOneofCase.UserProfile)
            {
                finalProfile = mapper.Map<CoreUserProfile>(reply.FormElement.UserProfile);
                break;
            }

            if (reply.FormElement.PayloadCase != GrpcFormElement.PayloadOneofCase.Question)
            {
                throw new InvalidOperationException("Answer returned an empty FormElement.");
            }

            currentQuestion = mapper.Map<CoreQuestion>(reply.FormElement.Question);
        }

        if (finalProfile is null)
        {
            throw new InvalidOperationException(
                $"Interview did not finish within the configured max turn count ({_options.MaxTurns}).");
        }

        var finalProfileJson = Serialize(finalProfile);
        var reviewerTextAI = GetReviewerTextAI();
        var review = await reviewerTextAI.CompleteChat(
            new TextAIRequest
            {
                ChatContext = testerChat,
                RequestText = finalProfileJson,
            },
            ct);
        if (!review.IsSuccess || string.IsNullOrWhiteSpace(review.RawResponse))
        {
            throw new InvalidOperationException("Tester AI failed to review the final user profile.");
        }

        var conclusion = await interviewClient.GetInterviewConclusionAsync(
            new GetInterviewConclusionRequest { Id = interviewId },
            cancellationToken: ct);
        var display = await interviewClient.GetInterviewDisplayAsync(
            new GetInterviewDisplayRequest { Id = interviewId },
            cancellationToken: ct);

        await SaveSuccessReportAsync(
            scenario,
            interviewId,
            testerProfile.RawResponse,
            transcript,
            finalProfileJson,
            Serialize(mapper.Map<CoreUserProfile>(conclusion.UserProfile)),
            Serialize(ToBusinessDisplay(display.InterviewDisplay)),
            review.RawResponse,
            ct);
    }

    private async Task<InterviewDisplay> CreateInterviewWithRetryAsync(CancellationToken ct)
    {
        for (var attempt = 1; attempt <= _options.StartupRetryCount; attempt++)
        {
            try
            {
                var reply = await interviewClient.CreateNewInterviewAsync(
                    new CreateNewInterviewRequest(),
                    cancellationToken: ct);
                return reply.InterviewDisplay;
            }
            catch (RpcException ex) when (
                attempt < _options.StartupRetryCount
                && (ex.StatusCode == StatusCode.Unavailable
                    || ex.StatusCode == StatusCode.DeadlineExceeded
                    || ex.StatusCode == StatusCode.Unknown))
            {
                logger.LogInformation(
                    "Interview API is not ready yet, retrying create call {attempt}/{maxAttempts}: {status}",
                    attempt,
                    _options.StartupRetryCount,
                    ex.StatusCode);
                await Task.Delay(_options.StartupRetryDelay, ct);
            }
        }

        throw new InvalidOperationException("Interview API did not become reachable in time.");
    }

    private async Task<CoreAnswer> AskTesterAiForAnswerAsync(Chat testerChat, CoreQuestion question, CancellationToken ct)
    {
        var formElementJson = Serialize(new CoreFormElement
        {
            Question = question,
        });
        var aiAnswer = await textAI.CompleteChatTyped<CoreAnswer>(
            new TextAIRequest
            {
                ChatContext = testerChat,
                RequestText = formElementJson,
            },
            ct);

        if (!aiAnswer.IsSuccess || aiAnswer.Response is null || string.IsNullOrWhiteSpace(aiAnswer.RawResponse))
        {
            throw new InvalidOperationException("Tester AI failed to answer an interview question.");
        }

        return aiAnswer.Response;
    }

    private static CoreAnswer CreateDeterministicClusterAnswer(AiTestScenarioOptions scenario)
    {
        return new CoreAnswer
        {
            SelectedOptions =
            [
                new CoreOptionAnswer
                {
                    OptionId = scenario.ClusterOptionId,
                    SelectedLevel = null,
                },
            ],
            TextAnswer = null,
            IsSkipped = false,
        };
    }

    private async Task SaveSuccessReportAsync(
        AiTestScenarioOptions scenario,
        string interviewId,
        string simulatedProfile,
        IReadOnlyList<TranscriptEntry> transcript,
        string finalProfileJson,
        string conclusionJson,
        string displayJson,
        string review,
        CancellationToken ct)
    {
        var report = new StringBuilder();
        report.AppendLine($"Scenario: {scenario.Name}");
        report.AppendLine($"Field of work: {scenario.FieldOfWork}");
        report.AppendLine($"Interviewer model: {_options.InterviewerModelLabel}");
        report.AppendLine($"Tester model: {_options.TesterModelLabel}");
        report.AppendLine($"Reviewer model: {GetReviewerModelLabel()}");
        report.AppendLine($"Interview id: {interviewId}");
        report.AppendLine($"Finished at UTC: {DateTimeOffset.UtcNow:O}");
        report.AppendLine($"AI interviewer step timings ms: {FormatAiInterviewerStepTimings(transcript)}");
        report.AppendLine(
            $"Average AI interviewer step time ms: {CalculateAverageAiInterviewerStepTimeMs(transcript):F1}");
        report.AppendLine();
        report.AppendLine("SIMULATED REAL USER PROFILE");
        report.AppendLine(simulatedProfile);
        report.AppendLine();
        report.AppendLine("TRANSCRIPT");
        foreach (var entry in transcript)
        {
            report.AppendLine();
            report.AppendLine($"QUESTION #{entry.Turn}");
            report.AppendLine(
                $"Answer RPC duration ms: {entry.AnswerRpcDuration.TotalMilliseconds:F1}; AI interviewer step: {entry.TriggersAiInterviewerStep}");
            report.AppendLine(entry.Question.QuestionText);
            if (entry.Question.AnswerOptions.Count > 0)
            {
                report.AppendLine("Options:");
                for (var index = 0; index < entry.Question.AnswerOptions.Count; index++)
                {
                    report.AppendLine($"{index}. {entry.Question.AnswerOptions[index]}");
                }
            }

            if (entry.Question.AnswerLevels is { Count: > 0 } answerLevels)
            {
                report.AppendLine($"Levels: [{string.Join(", ", answerLevels)}]");
            }

            report.AppendLine("Answer:");
            report.AppendLine(entry.RawAnswer);
        }

        report.AppendLine();
        report.AppendLine("FINAL PROFILE FROM ANSWER RPC");
        report.AppendLine(finalProfileJson);
        report.AppendLine();
        report.AppendLine("CONCLUSION FETCH RESULT");
        report.AppendLine(conclusionJson);
        report.AppendLine();
        report.AppendLine("DISPLAY FETCH RESULT");
        report.AppendLine(displayJson);
        report.AppendLine();
        report.AppendLine("TESTER REVIEW");
        report.AppendLine(review);

        await SaveReportAsync(scenario, report.ToString(), ct);
    }

    private async Task SaveFailureReportAsync(
        AiTestScenarioOptions scenario,
        Exception exception,
        CancellationToken ct)
    {
        var report = new StringBuilder();
        report.AppendLine($"Scenario: {scenario.Name}");
        report.AppendLine($"Field of work: {scenario.FieldOfWork}");
        report.AppendLine($"Interviewer model: {_options.InterviewerModelLabel}");
        report.AppendLine($"Tester model: {_options.TesterModelLabel}");
        report.AppendLine($"Reviewer model: {GetReviewerModelLabel()}");
        report.AppendLine($"Failed at UTC: {DateTimeOffset.UtcNow:O}");
        report.AppendLine();
        report.AppendLine("FAILURE");
        report.AppendLine(exception.ToString());

        await SaveReportAsync(scenario, report.ToString(), ct);
    }

    private async Task SaveReportAsync(
        AiTestScenarioOptions scenario,
        string reportText,
        CancellationToken ct)
    {
        var safeScenarioName = string.Join(
            "_",
            scenario.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var fileName = $"{safeScenarioName}_{DateTimeOffset.UtcNow:yyyy-MM-dd_HH-mm-ss}.txt";
        var path = Path.Combine(_options.ReportsPath, fileName);

        await File.WriteAllTextAsync(path, reportText, Encoding.UTF8, ct);
        logger.LogInformation("Saved AI gRPC interview report to {path}", path);
    }

    private string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _jsonOptions);
    }

    private ITextAI GetReviewerTextAI()
    {
        if (string.IsNullOrWhiteSpace(_options.ReviewerModelAlias))
        {
            return textAI;
        }

        return serviceProvider.GetKeyedService<ITextAI>(_options.ReviewerModelAlias)
               ?? throw new InvalidOperationException(
                   $"Reviewer AI model with alias '{_options.ReviewerModelAlias}' is not registered.");
    }

    private string GetReviewerModelLabel()
    {
        if (!string.IsNullOrWhiteSpace(_options.ReviewerModelLabel))
        {
            return _options.ReviewerModelLabel;
        }

        return string.IsNullOrWhiteSpace(_options.ReviewerModelAlias)
            ? _options.TesterModelLabel
            : _options.ReviewerModelAlias;
    }

    private static bool TriggersAiInterviewerStep(int zeroBasedTurn, int requiredQuestionCount)
    {
        return zeroBasedTurn + 1 >= requiredQuestionCount;
    }

    private static double CalculateAverageAiInterviewerStepTimeMs(IReadOnlyList<TranscriptEntry> transcript)
    {
        var timings = transcript
            .Where(entry => entry.TriggersAiInterviewerStep)
            .Select(entry => entry.AnswerRpcDuration.TotalMilliseconds)
            .ToList();

        return timings.Count == 0 ? 0 : timings.Average();
    }

    private static string FormatAiInterviewerStepTimings(IReadOnlyList<TranscriptEntry> transcript)
    {
        var timings = transcript
            .Where(entry => entry.TriggersAiInterviewerStep)
            .Select(entry => $"{entry.Turn}:{entry.AnswerRpcDuration.TotalMilliseconds:F1}")
            .ToList();

        return timings.Count == 0 ? "none" : string.Join(", ", timings);
    }

    private object ToBusinessDisplay(InterviewDisplay source)
    {
        return new
        {
            source.Id,
            Setup = source.Setup is null
                ? null
                : new
                {
                    source.Setup.Id,
                    RequiredQuestions = source.Setup.RequiredQuestions
                        .Select(mapper.Map<CoreQuestion>)
                        .ToList(),
                },
            RequiredAnswers = source.RequiredAnswers
                .Select(mapper.Map<CoreAnswer>)
                .ToList(),
            CompletedSteps = source.CompletedSteps
                .Select(step => new
                {
                    Question = mapper.Map<CoreQuestion>(step.Question),
                    Answer = mapper.Map<CoreAnswer>(step.Answer),
                })
                .ToList(),
            CurrentQuestion = source.CurrentQuestion is null ? null : mapper.Map<CoreQuestion>(source.CurrentQuestion),
            Conclusion = source.Conclusion is null ? null : mapper.Map<CoreUserProfile>(source.Conclusion),
        };
    }

    private sealed record TranscriptEntry(
        int Turn,
        CoreQuestion Question,
        CoreAnswer Answer,
        string RawAnswer,
        TimeSpan AnswerRpcDuration,
        bool TriggersAiInterviewerStep);
}
