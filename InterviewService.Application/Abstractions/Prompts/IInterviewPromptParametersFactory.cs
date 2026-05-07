using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Application.Abstractions.Prompts;

/// <summary>
/// Builds prompt parameters from an interview transcript and schema information.
/// </summary>
public interface IInterviewPromptParametersFactory
{
    InterviewPromptParameters CreateInterviewPromptParameters(
        InterviewSetup setup, string? requiredInterviewContext = null);
}
