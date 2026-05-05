using AIServices.Abstractions;
using InterviewService.Core.Models;

namespace InterviewService.Application.Services.Validators;

public sealed class AnswerAiValidator : IValidatorForAI<Answer>
{
    public Type GetValidatorType()
    {
        return typeof(Answer);
    }

    public ValueTask<bool> ValidateAsync(Answer? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ValueTask.FromResult(false);
        }

        if (request.IsSkipped)
        {
            return ValueTask.FromResult(
                request.SelectedOptions.Count == 0
                && string.IsNullOrWhiteSpace(request.TextAnswer));
        }

        var hasTextAnswer = !string.IsNullOrWhiteSpace(request.TextAnswer);
        var hasSelectedOptions = request.SelectedOptions.Count > 0;
        if (!hasTextAnswer && !hasSelectedOptions)
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(request.SelectedOptions.All(IsValidOptionAnswer));
    }

    private static bool IsValidOptionAnswer(OptionAnswer optionAnswer)
    {
        return optionAnswer.OptionId >= 0
               && optionAnswer.SelectedLevel is null or >= 0;
    }
}
