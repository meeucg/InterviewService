using AIServices.Models;
using InterviewService.Application.Abstractions.Converters;
using InterviewService.Application.Setups;
using InterviewService.Core.Entities;
using InterviewService.Core.Models;

namespace InterviewService.Application.Services;

public sealed class GeneralSetupConverter(IInterviewAiRequestBuilder requestBuilder) : IInterviewAiConverter
{
    private const string ItPromptName = "ItInterviewerPrompt";
    private const string DesignPromptName = "DesignInterviewerPrompt";

    public string ForSetupGroupName() => InterviewSetupCatalog.GeneralGroupName;

    public Task<TextAIRequest> ConvertToAiRequest(Interview interview, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(interview);

        if (!string.Equals(interview.Setup.GroupName, ForSetupGroupName(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Interview with setup group '{interview.Setup.GroupName}' is incompatible with this AI converter.");
        }

        return requestBuilder.BuildAsync(ResolvePromptName(interview), interview, ct);
    }

    private static string ResolvePromptName(Interview interview)
    {
        var clusterQuestion = interview.Setup.RequiredQuestions.FirstOrDefault()
                              ?? throw new InvalidOperationException("Cluster question is missing from general setup.");
        var clusterAnswer = interview.RequiredAnswers.FirstOrDefault()
                            ?? throw new InvalidOperationException("Cluster answer is missing from general setup interview.");

        if (clusterAnswer.IsSkipped)
        {
            throw new InvalidOperationException("Cluster question cannot be skipped.");
        }

        if (clusterAnswer.SelectedOptions.Count != 1)
        {
            throw new InvalidOperationException("Cluster question must have exactly one selected option.");
        }

        var selectedOption = clusterAnswer.SelectedOptions[0];
        var selectedOptionText = selectedOption.OptionId >= 0 && selectedOption.OptionId < clusterQuestion.AnswerOptions.Count
            ? clusterQuestion.AnswerOptions[selectedOption.OptionId]
            : string.Empty;

        if (IsItCluster(selectedOption.OptionId, selectedOptionText))
        {
            return ItPromptName;
        }

        if (IsDesignCluster(selectedOption.OptionId, selectedOptionText))
        {
            return DesignPromptName;
        }

        throw new InvalidOperationException(
            $"Unsupported cluster answer option '{selectedOption.OptionId}' with text '{selectedOptionText}'.");
    }

    private static bool IsItCluster(int optionId, string optionText)
    {
        return optionId == 0 ||
               string.Equals(optionText.Trim(), "IT", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDesignCluster(int optionId, string optionText)
    {
        return optionId == 1 ||
               string.Equals(optionText.Trim(), "Design", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(optionText.Trim(), "Дизайн", StringComparison.OrdinalIgnoreCase);
    }
}
