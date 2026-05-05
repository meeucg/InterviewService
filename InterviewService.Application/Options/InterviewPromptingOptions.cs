namespace InterviewService.Application.Options;

public sealed class InterviewPromptingOptions
{
    public const string SectionName = "InterviewPrompting";

    public string Variant { get; init; } = PromptVariants.Default;

    public int? MinimumDynamicAnswersBeforeConclusion { get; init; }

    public int GetMinimumDynamicAnswersBeforeConclusion()
    {
        if (MinimumDynamicAnswersBeforeConclusion is { } configuredValue)
        {
            return Math.Max(0, configuredValue);
        }

        return Variant.Trim() switch
        {
            PromptVariants.Evidence3 => 3,
            PromptVariants.Lean2 => 2,
            _ => 0,
        };
    }
}

public static class PromptVariants
{
    public const string Default = "Default";
    public const string Evidence3 = "Evidence3";
    public const string Lean2 = "Lean2";
}
