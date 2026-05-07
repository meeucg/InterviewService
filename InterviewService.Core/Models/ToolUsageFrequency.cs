using System.ComponentModel;

namespace InterviewService.Core.Models;

[Description("Частота использования инструмента пользователем в профессиональной деятельности.")]
/// <summary>
/// Describes how frequently the user appears to use a detected tool.
/// </summary>
public enum ToolUsageFrequency
{
    [Description("Основной инструмент, используемый практически ежедневно или " +
                 "являющийся центральным в рабочем процессе.")]
    Core,

    [Description("Регулярно используемый инструмент, который часто применяется в работе, " +
                 "но не обязательно является главным.")]
    Regular,

    [Description("Инструмент, который используется время от времени, " +
                 "в зависимости от типа проекта или задачи.")]
    Occasional,

    [Description("Редко используемый инструмент, с которым пользователь знаком и " +
                 "иногда обращается к нему при необходимости.")]
    Rare
}

