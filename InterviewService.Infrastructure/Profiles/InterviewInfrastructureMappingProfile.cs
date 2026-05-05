using AutoMapper;
using InterviewService.Core.Entities;
using InterviewService.Infrastructure.Models;
using InterviewService.Infrastructure.Serialization;

namespace InterviewService.Infrastructure.Profiles;

public sealed class InterviewInfrastructureMappingProfile : Profile
{
    public InterviewInfrastructureMappingProfile()
    {
        CreateMap<Interview, RedisInterviewDocument>()
            .ForMember(destination => destination.SetupId, options
                => options.MapFrom(source => source.Setup.Id))
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

        CreateMap<Interview, PostgresInterviewDto>()
            .ForMember(destination => destination.SetupId, options
                => options.MapFrom(source => source.Setup.Id))
            .ForMember(
                destination => destination.PayloadJson,
                options
                    => options.MapFrom(source => InterviewPersistencePayloadSerializer.SerializeInterview(source)))
            .ForMember(destination => destination.Setup, options
                => options.Ignore());

        CreateMap<RedisInterviewDocument, PostgresInterviewDto>()
            .ForMember(
                destination => destination.PayloadJson,
                options
                    => options.MapFrom(source => InterviewPersistencePayloadSerializer.SerializeInterviewState(
                    source.RequiredAnswers,
                    source.CompletedDynamicSteps,
                    source.CurrentQuestion,
                    source.Conclusion)))
            .ForMember(destination => destination.Setup, options
                => options.Ignore());

        CreateMap<PostgresInterviewDto, PostgresInterviewDto>()
            .ForMember(destination => destination.Setup, options
                => options.Ignore());

        CreateMap<InterviewSetup, RedisInterviewSetupDocument>()
            .ForMember(destination => destination.RequiredQuestions, options
                => options.MapFrom(source => source.RequiredQuestions.ToList()));

        CreateMap<InterviewSetup, PostgresInterviewSetupDto>()
            .ForMember(
                destination => destination.PayloadJson,
                options
                    => options.MapFrom(source => InterviewPersistencePayloadSerializer.SerializeInterviewSetup(source)))
            .ForMember(destination => destination.Interviews, options
                => options.Ignore());

        CreateMap<PostgresInterviewSetupDto, RedisInterviewSetupDocument>()
            .ForMember(
                destination => destination.RequiredQuestions,
                options
                    => options.MapFrom(source => InterviewPersistencePayloadSerializer
                    .DeserializeInterviewSetup(source.Id, source.PayloadJson)
                    .RequiredQuestions
                    .ToList()));

        CreateMap<RedisInterviewSetupDocument, PostgresInterviewSetupDto>()
            .ForMember(
                destination => destination.PayloadJson,
                options => options.MapFrom(source => InterviewPersistencePayloadSerializer.SerializeInterviewSetupState(
                    source.GroupName,
                    source.RequiredQuestions)))
            .ForMember(destination => destination.Interviews, options
                => options.Ignore());

        CreateMap<PostgresInterviewSetupDto, PostgresInterviewSetupDto>()
            .ForMember(destination => destination.Interviews, options
                => options.Ignore());
    }

}
