using AIServices.Abstractions;
using InterviewService.Application.Abstractions.Prompts;
using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Application.Services;

public class InterviewPromptParametersFactory(
    IJsonSchemaHelper jsonSchemaHelper) : IInterviewPromptParametersFactory
{
    public InterviewPromptParameters CreateInterviewPromptParameters(
        InterviewSetup setup, string? comment = null)
    {
        return new InterviewPromptParameters
        {
            InterviewSetup = setup,
            Comment = comment,
            AnswerScheme = jsonSchemaHelper.GetJsonScheme<Answer>(),
            FormElementScheme = jsonSchemaHelper.GetJsonScheme<FormElement>(),
        };
    }
}
