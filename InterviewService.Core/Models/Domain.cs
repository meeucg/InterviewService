using System.ComponentModel;

namespace InterviewService.Core.Models;

[Description("Предпочитаемая или знакомая пользователю доменная область бизнеса или типа продукта.")]
/// <summary>
/// Describes a professional domain detected for the interviewed user.
/// </summary>
public record Domain
{
    [Description("Каноничное название доменной области, например FinTech, E-commerce, " +
                 "Healthcare, Game Development и другие.")]
    public required string Name { get; set; }

    [Description("Альтернативные названия, синонимы и близкие формулировки той же доменной области.")]
    public List<string> AlternativeNames { get; set; } = [];
}

