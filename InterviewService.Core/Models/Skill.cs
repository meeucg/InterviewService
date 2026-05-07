using System.ComponentModel;

namespace InterviewService.Core.Models;

[Description("Профессиональный навык пользователя. " +
             "Каждый навык должен отражать одну отдельную компетенцию.")]
/// <summary>
/// Represents a skill detected in the final user profile.
/// </summary>
public record Skill
{
    [Description("Краткое каноничное название навыка.")]
    public required string DisplayName { get; set; }

    [Description("Краткое, но информативное описание навыка, пригодное для семантической обработки, " +
                 "embeddings и матчинга вакансий.")]
    public required string Description { get; set; }

    [Description("Уровень важности или доминантности этого навыка в профессиональном профиле пользователя.")]
    public required SkillDominanceLevel DominanceLevel { get; set; }

    [Description("Альтернативные названия, синонимы, сокращения и распространённые варианты " +
                 "формулировки того же навыка.")]
    public List<string> AlternativeNames { get; set; } = [];
}

