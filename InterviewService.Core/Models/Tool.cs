using System.ComponentModel;

namespace InterviewService.Core.Models;

[Description("Инструмент, технология, платформа, библиотека, сервис, " +
             "рабочая программа или среда, которыми пользователь реально пользуется.")]
/// <summary>
/// Represents a tool or technology detected in the final user profile.
/// </summary>
public record Tool
{
    [Description("Каноничное или стандартное название инструмента.")]
    public required string ToolStandardName { get; set; }

    [Description("Частота или регулярность использования этого инструмента пользователем в работе.")]
    public required ToolUsageFrequency UsageFrequency { get; set; }

    [Description("Альтернативные названия, синонимы, сокращения или распространённые " +
                 "варианты написания этого инструмента, в том числе Русские варианты названия " +
                 "(например Photoshop -> фотошоп).")]
    public List<string> ToolAltNames { get; set; } = [];
}

