using AIServices.Abstractions;
using InterviewService.Core.Models;

namespace InterviewService.Application.Services.Validators;

public sealed class FormElementAiValidator : IValidatorForAI<FormElement>
{
    public Type GetValidatorType()
    {
        return typeof(FormElement);
    }

    public ValueTask<bool> ValidateAsync(FormElement? request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(IsValidFormElement(request));
    }

    private static bool IsValidFormElement(FormElement? formElement)
    {
        if (formElement is null)
        {
            return false;
        }

        return formElement switch
        {
            { Question: not null, UserProfile: null } => IsValidQuestion(formElement.Question),
            { Question: null, UserProfile: not null } => IsValidUserProfile(formElement.UserProfile),
            _ => false,
        };
    }

    private static bool IsValidQuestion(Question question)
    {
        if (string.IsNullOrWhiteSpace(question.QuestionText))
        {
            return false;
        }

        if (question.AnswerOptions.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        if (question.AnswerOptions.Count == 0 && !question.PlainTextOptionPresent)
        {
            return false;
        }

        if (question.AnswerLevels is null)
        {
            return true;
        }

        return question.AnswerLevels.Count is > 0 and <= 5
               && question.AnswerLevels.All(level => !string.IsNullOrWhiteSpace(level))
               && !question.IsSingleChoice;
    }

    private static bool IsValidUserProfile(UserProfile userProfile)
    {
        return !string.IsNullOrWhiteSpace(userProfile.Cluster)
               && userProfile.Specializations.Count > 0
               && userProfile.Specializations.All(IsValidSpecialization)
               && userProfile.Skills.Count > 0
               && userProfile.Skills.All(IsValidSkill)
               && userProfile.Tools.Count > 0
               && userProfile.Tools.All(IsValidTool)
               && userProfile.PreferredDomains.All(IsValidDomain);
    }

    private static bool IsValidSpecialization(Specialization specialization)
    {
        return !string.IsNullOrWhiteSpace(specialization.Name)
               && specialization.AlternativeNames.All(name => !string.IsNullOrWhiteSpace(name));
    }

    private static bool IsValidSkill(Skill skill)
    {
        return !string.IsNullOrWhiteSpace(skill.DisplayName)
               && !string.IsNullOrWhiteSpace(skill.Description)
               && skill.AlternativeNames.All(name => !string.IsNullOrWhiteSpace(name));
    }

    private static bool IsValidTool(Tool tool)
    {
        return !string.IsNullOrWhiteSpace(tool.ToolStandardName)
               && tool.ToolAltNames.All(name => !string.IsNullOrWhiteSpace(name));
    }

    private static bool IsValidDomain(Domain domain)
    {
        return !string.IsNullOrWhiteSpace(domain.Name)
               && domain.AlternativeNames.All(name => !string.IsNullOrWhiteSpace(name));
    }
}
