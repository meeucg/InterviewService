using AIServices.Abstractions;
using InterviewService.Core.Models;

namespace InterviewService.Application.Services.Validators;

/// <summary>
/// Validates AI-generated form elements before they become interview questions or conclusions.
/// </summary>
public sealed class FormElementAiValidator : IValidatorForAI<FormElement>
{
    public Type GetValidatorType()
    {
        return typeof(FormElement);
    }

    public ValueTask<bool> ValidateAsync(FormElement? request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(InterviewAiValidationRules.IsValidFormElement(request));
    }
}
