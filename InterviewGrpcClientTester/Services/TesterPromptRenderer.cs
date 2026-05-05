using System.Reflection;
using InterviewGrpcClientTester.Models;
using Scriban;

namespace InterviewGrpcClientTester.Services;

public sealed class TesterPromptRenderer
{
    private const string ResourceName = "InterviewGrpcClientTester.Prompts.TesterPrompt.txt";
    private readonly Assembly _assembly = typeof(TesterPromptRenderer).Assembly;

    public async Task<string> RenderAsync(
        string formElementScheme,
        string answerScheme,
        AiTestScenarioOptions scenario,
        CancellationToken ct = default)
    {
        await using var stream = _assembly.GetManifestResourceStream(ResourceName)
                                 ?? throw new InvalidOperationException(
                                     $"Embedded prompt resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);

        var templateText = await reader.ReadToEndAsync(ct);
        var template = Template.Parse(templateText, ResourceName);
        if (template.HasErrors)
        {
            var errors = string.Join(Environment.NewLine, template.Messages.Select(x => x.Message));
            throw new InvalidOperationException($"Tester prompt template is invalid:{Environment.NewLine}{errors}");
        }

        return template.Render(
            new
            {
                FormElementScheme = formElementScheme,
                AnswerScheme = answerScheme,
                FieldOfWork = scenario.FieldOfWork,
            },
            member => member.Name);
    }
}
