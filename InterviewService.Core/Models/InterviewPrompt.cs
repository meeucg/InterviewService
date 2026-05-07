using InterviewService.Core.Abstractions;

namespace InterviewService.Core.Models;

/// <summary>
/// Prompt marker for interview prompt templates and their parameter type.
/// </summary>
public record InterviewPrompt : Prompt<InterviewPromptParameters>;
