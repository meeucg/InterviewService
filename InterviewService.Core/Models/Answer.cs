using System.ComponentModel;

namespace InterviewService.Core.Models;

[Description("Ответ пользователя на один вопрос анкеты. " +
             "Содержит выбранные номера вариантов ответа и, при необходимости, текст собственного варианта.")]
/// <summary>
/// Represents a user answer to a required or dynamic interview question.
/// </summary>
public record Answer
{
    [Description("Список номеров выбранных пользователем вариантов ответа. " +
                 "Нумерация соответствует порядку вариантов, показанных в вопросе.")]
    public List<OptionAnswer> SelectedOptions { get; set; } = [];

    [Description("Текст собственного ответа пользователя, если в вопросе был выбран вариант «Другое»" +
                 "или в вопросе был только текстовый ввод без вариантов ответа. " +
                 "Если пользователь не указывал свой вариант, значение должно быть null.")]
    public string? TextAnswer { get; set; }
    
    [Description("true - если вопрос пропущен, что возможно только если вопрос необязательный")]
    public bool IsSkipped { get; set; }
}

