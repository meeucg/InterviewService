using System.ComponentModel;

namespace InterviewService.Core.Models;

[Description("Уровень важности или доминантности навыка в профессии пользователя.")]
/// <summary>
/// Describes how central a detected skill is to the user profile.
/// </summary>
public enum SkillDominanceLevel
{
    [Description("Ключевой, определяющий навык, который является одной из " +
                 "центральных частей профессионального профиля пользователя.")]
    Core,

    [Description("Важный, регулярно применяемый навык, но не самый определяющий для профессии пользователя.")]
    Important,

    [Description("Дополнительный или смежный навык, который используется заметно, но не является центральным.")]
    Secondary,

    [Description("Ограниченно используемый или эпизодический навык, " +
                 "который присутствует в опыте пользователя, но не является сильной стороной.")]
    Limited
}

