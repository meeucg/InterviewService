using System.Text.Json;
using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Infrastructure.Serialization;

internal static class InterviewPersistencePayloadSerializer
{
    public static string SerializeInterview(Interview interview)
    {
        return SerializeInterviewState(
            interview.RequiredAnswers,
            interview.CompletedDynamicSteps,
            interview.CurrentQuestion,
            interview.Conclusion);
    }

    public static string SerializeInterviewSetup(InterviewSetup setup)
    {
        return SerializeInterviewSetupState(setup.GroupName, setup.RequiredQuestions);
    }

    public static string SerializeInterviewState(
        IReadOnlyList<Answer> requiredAnswers,
        IReadOnlyList<InterviewStep> completedDynamicSteps,
        Question? currentQuestion,
        UserProfile? conclusion)
    {
        return JsonSerializer.Serialize(
            new InterviewPayload
            {
                RequiredAnswers = requiredAnswers.ToList(),
                CompletedDynamicSteps = completedDynamicSteps.ToList(),
                CurrentQuestion = currentQuestion,
                Conclusion = conclusion,
            },
            InfrastructureJson.SerializerOptions);
    }

    public static string SerializeInterviewSetupState(string groupName, IReadOnlyList<Question> requiredQuestions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        return JsonSerializer.Serialize(
            new InterviewSetupPayload
            {
                GroupName = groupName.Trim(),
                RequiredQuestions = requiredQuestions.ToList(),
            },
            InfrastructureJson.SerializerOptions);
    }

    public static Interview DeserializeInterview(Guid interviewId, string payloadJson, InterviewSetup setup)
    {
        var payload = JsonSerializer.Deserialize<InterviewPayload>(payloadJson, InfrastructureJson.SerializerOptions)
                      ?? throw new InvalidOperationException(
                          $"Interview '{interviewId}' payload could not be deserialized.");

        return new Interview(
            interviewId,
            payload.RequiredAnswers,
            payload.CompletedDynamicSteps,
            payload.CurrentQuestion,
            payload.Conclusion,
            setup);
    }

    public static InterviewSetup DeserializeInterviewSetup(Guid setupId, string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<InterviewSetupPayload>(payloadJson, InfrastructureJson.SerializerOptions)
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

    private sealed record InterviewPayload
    {
        public List<Answer> RequiredAnswers { get; init; } = [];

        public List<InterviewStep> CompletedDynamicSteps { get; init; } = [];

        public Question? CurrentQuestion { get; init; }

        public UserProfile? Conclusion { get; init; }
    }

    private sealed record InterviewSetupPayload
    {
        public required string GroupName { get; init; }

        public List<Question> RequiredQuestions { get; init; } = [];
    }
}
