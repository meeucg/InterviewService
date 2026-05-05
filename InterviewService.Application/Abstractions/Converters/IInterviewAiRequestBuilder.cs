using AIServices.Models;
using InterviewService.Core.Entities;

namespace InterviewService.Application.Abstractions.Converters;

public interface IInterviewAiRequestBuilder
{
    Task<TextAIRequest> BuildAsync(
        string promptName,
        Interview interview,
        CancellationToken ct = default);
}
