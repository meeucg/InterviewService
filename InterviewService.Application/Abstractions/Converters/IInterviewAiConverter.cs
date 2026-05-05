using AIServices.Models;
using InterviewService.Core.Entities;

namespace InterviewService.Application.Abstractions.Converters;

public interface IInterviewAiConverter
{
    Task<TextAIRequest> ConvertToAiRequest(Interview interview, CancellationToken ct = default);

    string ForSetupGroupName();
}
