using AIServices.Models;
using InterviewService.Core.Entities;

namespace InterviewService.Application.Abstractions.Converters;

/// <summary>
/// Builds text AI requests from prompt names and interview state.
/// </summary>
public interface IInterviewAiRequestBuilder
{
    Task<TextAIRequest> BuildAsync(
        string promptName,
        Interview interview,
        CancellationToken ct = default);
}
