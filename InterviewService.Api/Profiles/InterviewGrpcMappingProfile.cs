using AutoMapper;
using FitFlow.Interview.Grpc.Contracts;
using CoreAnswer = InterviewService.Core.Models.Answer;
using CoreDomain = InterviewService.Core.Models.Domain;
using CoreFormElement = InterviewService.Core.Models.FormElement;
using CoreInterview = InterviewService.Core.Entities.Interview;
using CoreInterviewSetup = InterviewService.Core.Entities.InterviewSetup;
using CoreInterviewStep = InterviewService.Core.Models.InterviewStep;
using CoreOptionAnswer = InterviewService.Core.Models.OptionAnswer;
using CoreQuestion = InterviewService.Core.Models.Question;
using CoreSkill = InterviewService.Core.Models.Skill;
using CoreSkillDominanceLevel = InterviewService.Core.Models.SkillDominanceLevel;
using CoreSpecialization = InterviewService.Core.Models.Specialization;
using CoreTool = InterviewService.Core.Models.Tool;
using CoreToolUsageFrequency = InterviewService.Core.Models.ToolUsageFrequency;
using CoreUserProfile = InterviewService.Core.Models.UserProfile;

namespace InterviewService.Api.Profiles;

public sealed class InterviewGrpcMappingProfile : Profile
{
    public InterviewGrpcMappingProfile()
    {
        CreateMap<CoreInterview, InterviewDisplay>()
            .ForMember(destination => destination.Id, options => options.MapFrom(source => source.Id.ToString()));

        CreateMap<CoreInterviewSetup, InterviewSetup>()
            .ForMember(destination => destination.Id, options => options.MapFrom(source => source.Id.ToString()));

        CreateMap<CoreInterviewStep, InterviewStep>();

        CreateMap<CoreQuestion, Question>();

        CreateMap<CoreAnswer, Answer>()
            .ForMember(destination => destination.TextAnswer, options => options.MapFrom(source => source.TextAnswer ?? string.Empty));

        CreateMap<CoreOptionAnswer, OptionAnswer>()
            .ForMember(destination => destination.SelectedLevel, options => options.MapFrom(source => source.SelectedLevel ?? -1));

        CreateMap<CoreUserProfile, UserProfile>();
        CreateMap<CoreSpecialization, Specialization>();

        CreateMap<CoreSkill, Skill>()
            .ForMember(
                destination => destination.DominanceLevel,
                options => options.MapFrom(source => source.DominanceLevel));

        CreateMap<CoreTool, Tool>()
            .ForMember(
                destination => destination.UsageFrequency,
                options => options.MapFrom(source => source.UsageFrequency));

        CreateMap<CoreDomain, Domain>();
        CreateMap<CoreFormElement, FormElement>()
            .ConvertUsing<CoreFormElementToGrpcFormElementConverter>();

        CreateMap<Answer, CoreAnswer>()
            .ForMember(destination => destination.SelectedOptions, options => options.MapFrom(source => source.SelectedOptions))
            .ForMember(
                destination => destination.TextAnswer,
                options => options.MapFrom(source => string.IsNullOrWhiteSpace(source.TextAnswer) ? null : source.TextAnswer))
            .ForMember(destination => destination.IsSkipped, options => options.MapFrom(source => source.IsSkipped));

        CreateMap<OptionAnswer, CoreOptionAnswer>()
            .ConvertUsing<GrpcOptionAnswerToCoreOptionAnswerConverter>();

        CreateMap<CoreSkillDominanceLevel, SkillDominanceLevel>()
            .ConvertUsing<CoreSkillDominanceLevelToGrpcConverter>();

        CreateMap<CoreToolUsageFrequency, ToolUsageFrequency>()
            .ConvertUsing<CoreToolUsageFrequencyToGrpcConverter>();
    }
}

file sealed class GrpcOptionAnswerToCoreOptionAnswerConverter : ITypeConverter<OptionAnswer, CoreOptionAnswer>
{
    public CoreOptionAnswer Convert(OptionAnswer source, CoreOptionAnswer destination, ResolutionContext context)
    {
        return new CoreOptionAnswer
        {
            OptionId = source.OptionId,
            SelectedLevel = source.SelectedLevel >= 0 ? source.SelectedLevel : null,
        };
    }
}

file sealed class CoreFormElementToGrpcFormElementConverter : ITypeConverter<CoreFormElement, FormElement>
{
    public FormElement Convert(CoreFormElement source, FormElement destination, ResolutionContext context)
    {
        if (source.Question is not null)
        {
            return new FormElement
            {
                Question = context.Mapper.Map<Question>(source.Question),
            };
        }

        if (source.UserProfile is not null)
        {
            return new FormElement
            {
                UserProfile = context.Mapper.Map<UserProfile>(source.UserProfile),
            };
        }

        return new FormElement();
    }
}

file sealed class CoreSkillDominanceLevelToGrpcConverter : ITypeConverter<CoreSkillDominanceLevel, SkillDominanceLevel>
{
    public SkillDominanceLevel Convert(
        CoreSkillDominanceLevel source,
        SkillDominanceLevel destination,
        ResolutionContext context)
    {
        return source switch
        {
            CoreSkillDominanceLevel.Core => SkillDominanceLevel.Core,
            CoreSkillDominanceLevel.Important => SkillDominanceLevel.Important,
            CoreSkillDominanceLevel.Secondary => SkillDominanceLevel.Secondary,
            CoreSkillDominanceLevel.Limited => SkillDominanceLevel.Limited,
            _ => SkillDominanceLevel.Unspecified,
        };
    }
}

file sealed class CoreToolUsageFrequencyToGrpcConverter : ITypeConverter<CoreToolUsageFrequency, ToolUsageFrequency>
{
    public ToolUsageFrequency Convert(
        CoreToolUsageFrequency source,
        ToolUsageFrequency destination,
        ResolutionContext context)
    {
        return source switch
        {
            CoreToolUsageFrequency.Core => ToolUsageFrequency.Core,
            CoreToolUsageFrequency.Regular => ToolUsageFrequency.Regular,
            CoreToolUsageFrequency.Occasional => ToolUsageFrequency.Occasional,
            CoreToolUsageFrequency.Rare => ToolUsageFrequency.Rare,
            _ => ToolUsageFrequency.Unspecified,
        };
    }
}
