using AIServices.Abstractions;
using InterviewService.Core.Models;

namespace InterviewService.Application.Services.Validators;

/// <summary>
/// Validates AI-generated question payloads before they are accepted by the AI services pipeline.
/// </summary>
public sealed class QuestionAiValidator : IValidatorForAI<Question>
{
    public Type GetValidatorType()
    {
        return typeof(Question);
    }

    public ValueTask<bool> ValidateAsync(Question? request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(InterviewAiValidationRules.IsValidQuestion(request));
    }
}
