using InterviewService.Core.Abstractions;
using InterviewService.Core.Entities;

namespace InterviewService.Core.Models;

public record InterviewPromptParameters : PromptParameters
{
    public required InterviewSetup InterviewSetup { get; init; }
    public required string FormElementScheme { get; init; }
    public required string AnswerScheme { get; init; }
    public string? RequiredInterviewContext { get; init; }
}
