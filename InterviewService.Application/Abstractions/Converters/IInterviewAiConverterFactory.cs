namespace InterviewService.Application.Abstractions.Converters;

public interface IInterviewAiConverterFactory
{
    IInterviewAiConverter GetConverterForSetupGroupName(string groupName);
}
