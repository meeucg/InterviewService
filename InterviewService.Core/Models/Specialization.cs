using System.ComponentModel;

namespace InterviewService.Core.Models;

[Description("Профессиональная специализация пользователя. " +
             "Специализация отвечает на вопрос, в каком направлении работает человек как специалист.")]
/// <summary>
/// Represents the primary professional specialization inferred for the user.
/// </summary>
public record Specialization
{
    [Description("Каноничное название специализации пользователя.")]
    public required string Name { get; set; }

    [Description("Альтернативные названия, синонимы и близкие формулировки той же специализации.")]
    public List<string> AlternativeNames { get; set; } = [];
}

