namespace InterviewService.Application.Abstractions.Converters;

/// <summary>
/// Resolves interview AI converters by setup group name.
/// </summary>
public interface IInterviewAiConverterFactory
{
    IInterviewAiConverter GetConverterForSetupGroupName(string groupName);
}
