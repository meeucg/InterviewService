using AIServices.Abstractions;
using InterviewService.Application.Abstractions.Converters;
using InterviewService.Application.Abstractions.Prompts;
using InterviewService.Application.Abstractions.UseCases;
using InterviewService.Application.Services;
using InterviewService.Application.Services.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewService.Application.DependencyInjection;

public static class InterviewApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddInterviewApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPromptTemplateTextReader, ApplicationEmbeddedPromptTemplateTextReader>();
        services.AddSingleton<IPromptRenderer, PromptRenderer>();
        services.AddSingleton<IInterviewPromptParametersFactory, InterviewPromptParametersFactory>();
        services.AddSingleton<IInterviewAiRequestBuilder, InterviewAiRequestBuilder>();
        services.AddSingleton<IInterviewAiConverter, GeneralSetupConverter>();
        services.AddSingleton<IInterviewAiConverterFactory, InterviewAiConverterFactory>();
        services.AddInterviewAiValidators();
        services.AddScoped<IInterviewUseCase, InterviewUseCase>();
        return services;
    }

    public static IServiceCollection AddInterviewAiValidators(this IServiceCollection services)
    {
        services.AddSingleton<IValidatorForAI, FormElementAiValidator>();
        services.AddSingleton<IValidatorForAI, AnswerAiValidator>();
        return services;
    }
}
