using System.ComponentModel;

namespace InterviewService.Core.Models;

[Description("Выбранная опция")]
public record OptionAnswer
{
    [Description("Id выбранной опции, нумерация опций начинается с 0")]
    public int OptionId { get; set; }
    
    [Description("Поле заполняется если вопрос предполагает уровни ответа, " +
                 "ассоциированные с опцией, для обычных опций без уровней, это поле = null")]
    public int? SelectedLevel { get; set; }
}

