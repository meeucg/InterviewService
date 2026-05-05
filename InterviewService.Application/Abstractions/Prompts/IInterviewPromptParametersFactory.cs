using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Application.Abstractions.Prompts;

public interface IInterviewPromptParametersFactory
{
    InterviewPromptParameters CreateInterviewPromptParameters(
        InterviewSetup setup, string? requiredInterviewContext = null);
}
