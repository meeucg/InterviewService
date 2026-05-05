namespace InterviewGrpcClientTester.Models;

public sealed class AiTestingOptions
{
    public const string SectionName = "AiTesting";

    public string ReportsPath { get; set; } = "/reports";

    public int MaxTurns { get; set; } = 20;

    public int MaxParallelScenarios { get; set; } = 30;

    public int StartupRetryCount { get; set; } = 30;

    public TimeSpan StartupRetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    public string? ScenarioNameFilter { get; set; }

    public string InterviewerModelLabel { get; set; } = "Grok 4.20 (x-ai/grok-4.20)";

    public string TesterModelLabel { get; set; } = "GPT-OSS 120B (openai/gpt-oss-120b)";

    public string PromptVariantLabel { get; set; } = "Default";

    public List<AiTestScenarioOptions> Scenarios { get; set; } = [];

    public IReadOnlyList<AiTestScenarioOptions> GetScenarios()
    {
        var scenarios = Scenarios.Count > 0
            ? Scenarios
            :
            [
                new AiTestScenarioOptions
                {
                    Name = "IT",
                    FieldOfWork = ".NET backend developer",
                    ClusterOptionId = 0,
                },
                new AiTestScenarioOptions
                {
                    Name = "Design",
                    FieldOfWork = "product designer",
                    ClusterOptionId = 1,
                },
            ];

        if (string.IsNullOrWhiteSpace(ScenarioNameFilter))
        {
            return scenarios;
        }

        var allowedNames = ScenarioNameFilter
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return scenarios
            .Where(scenario => allowedNames.Contains(scenario.Name))
            .ToList();
    }
}

public sealed class AiTestScenarioOptions
{
    public required string Name { get; set; }

    public required string FieldOfWork { get; set; }

    public int ClusterOptionId { get; set; }
}
