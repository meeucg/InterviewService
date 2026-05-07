using AIServices.Models;
using InterviewService.Core.Entities;

namespace InterviewService.Application.Abstractions.Converters;

/// <summary>
/// Routes an interview in a setup group into a concrete AI request.
/// </summary>
public interface IInterviewAiConverter
{
    Task<TextAIRequest> ConvertToAiRequest(Interview interview, CancellationToken ct = default);

    string ForSetupGroupName();
}
