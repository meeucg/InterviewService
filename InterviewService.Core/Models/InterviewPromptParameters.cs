using InterviewService.Core.Abstractions;
using InterviewService.Core.Entities;

namespace InterviewService.Core.Models;

/// <summary>
/// Parameters rendered into an interview prompt before sending it to the text AI service.
/// </summary>
public record InterviewPromptParameters : PromptParameters
{
    public required InterviewSetup InterviewSetup { get; init; }
    public required string FormElementScheme { get; init; }
    public required string AnswerScheme { get; init; }
    public string? RequiredInterviewContext { get; init; }
}
