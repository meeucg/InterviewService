namespace InterviewService.Application.Options;

public sealed class InterviewPromptingOptions
{
    public const string SectionName = "InterviewPrompting";

    public string Variant { get; init; } = PromptVariants.Default;
}

public static class PromptVariants
{
    public const string Default = "Default";
    public const string Evidence3 = "Evidence3";
    public const string Lean2 = "Lean2";
}
