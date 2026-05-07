using AIServices.Abstractions;
using InterviewService.Core.Models;

namespace InterviewService.Application.Services.Validators;

/// <summary>
/// Validates AI-generated user profiles before they are accepted by the AI services pipeline.
/// </summary>
public sealed class UserProfileAiValidator : IValidatorForAI<UserProfile>
{
    public Type GetValidatorType()
    {
        return typeof(UserProfile);
    }

    public ValueTask<bool> ValidateAsync(UserProfile? request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(InterviewAiValidationRules.IsValidUserProfile(request));
    }
}
