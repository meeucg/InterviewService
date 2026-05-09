using AutoMapper;
using InterviewService.Core.Entities;
using InterviewService.Infrastructure.Models;

namespace InterviewService.Infrastructure.Profiles;

/// <summary>
/// AutoMapper profile for domain, PostgreSQL DTO, and Redis document conversions.
/// </summary>
public sealed class InterviewInfrastructureMappingProfile : Profile
{
    public InterviewInfrastructureMappingProfile()
    {
        CreateMap<Interview, RedisInterviewDocument>()
            .ForMember(destination => destination.SetupHashGuid, options
                => options.MapFrom(source => source.Setup.HashGuid))
            .ForMember(destination => destination.RequiredAnswers, options
                => options.MapFrom(source => source.RequiredAnswers.ToList()))
            .ForMember(destination => destination.CompletedDynamicSteps, options
                => options.MapFrom(source => source.CompletedDynamicSteps.ToList()))
            .ForMember(destination => destination.CurrentQuestion, options
                => options.MapFrom(source => source.CurrentQuestion))
            .ForMember(destination => destination.Conclusion, options
                => options.MapFrom(source => source.Conclusion))
            .ForMember(destination => destination.LastTouchedAt, options
                => options.Ignore())
            .ForMember(destination => destination.IsFinished, options
                => options.MapFrom(source => source.IsFinished));

        CreateMap<PostgresInterviewDto, PostgresInterviewDto>()
            .ForMember(destination => destination.Setup, options
                => options.Ignore());

        CreateMap<InterviewSetup, RedisInterviewSetupDocument>()
            .ForMember(destination => destination.RequiredQuestions, options
                => options.MapFrom(source => source.RequiredQuestions.ToList()));

        CreateMap<PostgresInterviewSetupDto, PostgresInterviewSetupDto>()
            .ForMember(destination => destination.Interviews, options
                => options.Ignore());
    }

}
