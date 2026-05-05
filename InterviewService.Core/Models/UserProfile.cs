using System.ComponentModel;

namespace InterviewService.Core.Models;

[Description("Полный структурированный профессиональный профиль пользователя, " +
             "используемый для рекомендаций вакансий и описания области его экспертизы.")]
public record UserProfile
{
    [Description("Основной профессиональный кластер пользователя, например Design или IT.")]
    public required string Cluster { get; set; }

    [Description("Список основных специализаций пользователя. " +
                 "Это профессиональные направления, а не отдельные навыки.")]
    public required List<Specialization> Specializations { get; set; }

    [Description("Список профессиональных навыков пользователя. " +
                 "Каждый навык должен описывать одну конкретную компетенцию.")]
    public required List<Skill> Skills { get; set; }

    [Description("Список инструментов, платформ, библиотек, сред, сервисов и программ, " +
                 "которыми пользователь реально пользуется в работе.")]
    public required List<Tool> Tools { get; set; }

    [Description("Список предпочитаемых или хорошо знакомых пользователю доменных областей бизнеса, " +
                 "например E-commerce, FinTech, Healthcare и другие.")]
    public required List<Domain> PreferredDomains { get; set; }
}

