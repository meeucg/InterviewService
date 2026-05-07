using System.ComponentModel;

namespace InterviewService.Core.Models;

[Description("Единый элемент ответа ИИ в процессе онбординга. " +
             "Это либо следующий вопрос, либо финальный собранный профиль навыков пользователя. " +
             "Конкретный тип ответа определяется полем IsQuestion.")]
/// <summary>
/// Represents the next interview UI element: either a question or the final user profile.
/// </summary>
public record FormElement
{
    [Description("Значение true означает, что текущий ответ содержит следующий вопрос анкеты. " +
                 "Значение false означает, что текущий ответ содержит финальный профиль пользователя.")]
    public bool IsQuestion => Question != null;

    [Description("Следующий вопрос, который нужно задать пользователю. " +
                 "Должен быть заполнен только если IsQuestion = true, иначе должен быть null.")]
    public Question? Question { get; set; }

    [Description("Финальный нормализованный профессиональный профиль пользователя, собранный по итогам интервью. " +
                 "Должен быть заполнен только если IsQuestion = false, иначе должен быть null.")]
    public UserProfile? UserProfile { get; set; }
}
