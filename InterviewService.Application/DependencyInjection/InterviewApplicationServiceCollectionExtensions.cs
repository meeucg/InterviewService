using AIServices.Abstractions;
using InterviewService.Application.Abstractions.Prompts;
using InterviewService.Application.Abstractions.Setups;
using InterviewService.Application.Abstractions.UseCases;
using InterviewService.Application.Services.Prompts;
using InterviewService.Application.Services.UseCases;
using InterviewService.Application.Services.Validators;
using InterviewService.Application.Setups;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewService.Application.DependencyInjection;

/// <summary>
/// Registers application-layer services, prompt services, AI validators, and use cases.
/// </summary>
public static class InterviewApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddInterviewApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPromptTemplateTextReader, ApplicationEmbeddedPromptTemplateTextReader>();
        services.AddSingleton<IPromptRenderer, PromptRenderer>();
        services.AddSingleton<IInterviewSetupCatalog, InterviewSetupCatalog>();
        services.AddInterviewAiValidators();
        services.AddScoped<IInterviewUseCase, GeneralInterviewUseCase>();
        services.AddScoped<IInterviewUseCaseFactory, InterviewUseCaseFactory>();
        return services;
    }

    private static IServiceCollection AddInterviewAiValidators(this IServiceCollection services)
    {
        services.AddSingleton<IValidatorForAI, FormElementAiValidator>();
        services.AddSingleton<IValidatorForAI, QuestionAiValidator>();
        services.AddSingleton<IValidatorForAI, UserProfileAiValidator>();
        services.AddSingleton<IValidatorForAI, AnswerAiValidator>();
        return services;
    }
}
