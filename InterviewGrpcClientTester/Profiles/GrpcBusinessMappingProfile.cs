using AutoMapper;
using FitFlow.Interview.Grpc.Contracts;
using CoreAnswer = InterviewService.Core.Models.Answer;
using CoreDomain = InterviewService.Core.Models.Domain;
using CoreFormElement = InterviewService.Core.Models.FormElement;
using CoreInterviewStep = InterviewService.Core.Models.InterviewStep;
using CoreOptionAnswer = InterviewService.Core.Models.OptionAnswer;
using CoreQuestion = InterviewService.Core.Models.Question;
using CoreSkill = InterviewService.Core.Models.Skill;
using CoreSkillDominanceLevel = InterviewService.Core.Models.SkillDominanceLevel;
using CoreSpecialization = InterviewService.Core.Models.Specialization;
using CoreTool = InterviewService.Core.Models.Tool;
using CoreToolUsageFrequency = InterviewService.Core.Models.ToolUsageFrequency;
using CoreUserProfile = InterviewService.Core.Models.UserProfile;

namespace InterviewGrpcClientTester.Profiles;

public sealed class GrpcBusinessMappingProfile : Profile
{
    public GrpcBusinessMappingProfile()
    {
        CreateMap<Question, CoreQuestion>().ConvertUsing<GrpcQuestionToCoreQuestionConverter>();
        CreateMap<CoreQuestion, Question>().ConvertUsing<CoreQuestionToGrpcQuestionConverter>();

        CreateMap<Answer, CoreAnswer>().ConvertUsing<GrpcAnswerToCoreAnswerConverter>();
        CreateMap<CoreAnswer, Answer>().ConvertUsing<CoreAnswerToGrpcAnswerConverter>();

        CreateMap<OptionAnswer, CoreOptionAnswer>().ConvertUsing<GrpcOptionAnswerToCoreOptionAnswerConverter>();
        CreateMap<CoreOptionAnswer, OptionAnswer>().ConvertUsing<CoreOptionAnswerToGrpcOptionAnswerConverter>();

        CreateMap<UserProfile, CoreUserProfile>().ConvertUsing<GrpcUserProfileToCoreUserProfileConverter>();
        CreateMap<CoreUserProfile, UserProfile>().ConvertUsing<CoreUserProfileToGrpcUserProfileConverter>();

        CreateMap<Specialization, CoreSpecialization>();
        CreateMap<CoreSpecialization, Specialization>();

        CreateMap<Skill, CoreSkill>();
        CreateMap<CoreSkill, Skill>();

        CreateMap<Tool, CoreTool>();
        CreateMap<CoreTool, Tool>();

        CreateMap<Domain, CoreDomain>();
        CreateMap<CoreDomain, Domain>();

        CreateMap<SkillDominanceLevel, CoreSkillDominanceLevel>()
            .ConvertUsing<GrpcSkillDominanceLevelToCoreConverter>();
        CreateMap<CoreSkillDominanceLevel, SkillDominanceLevel>()
            .ConvertUsing<CoreSkillDominanceLevelToGrpcConverter>();

        CreateMap<ToolUsageFrequency, CoreToolUsageFrequency>()
            .ConvertUsing<GrpcToolUsageFrequencyToCoreConverter>();
        CreateMap<CoreToolUsageFrequency, ToolUsageFrequency>()
            .ConvertUsing<CoreToolUsageFrequencyToGrpcConverter>();

        CreateMap<FormElement, CoreFormElement>().ConvertUsing<GrpcFormElementToCoreFormElementConverter>();
        CreateMap<CoreFormElement, FormElement>().ConvertUsing<CoreFormElementToGrpcFormElementConverter>();

        CreateMap<InterviewStep, CoreInterviewStep>();
        CreateMap<CoreInterviewStep, InterviewStep>();
    }
}

file sealed class GrpcQuestionToCoreQuestionConverter : ITypeConverter<Question, CoreQuestion>
{
    public CoreQuestion Convert(Question source, CoreQuestion destination, ResolutionContext context)
    {
        return new CoreQuestion
        {
            QuestionText = source.QuestionText,
            AnswerOptions = source.AnswerOptions.ToList(),
            AnswerLevels = source.AnswerLevels.Count == 0 ? null : source.AnswerLevels.ToList(),
            PlainTextOptionPresent = source.PlainTextOptionPresent,
            IsSingleChoice = source.IsSingleChoice,
            IsOptional = source.IsOptional,
        };
    }
}

file sealed class CoreQuestionToGrpcQuestionConverter : ITypeConverter<CoreQuestion, Question>
{
    public Question Convert(CoreQuestion source, Question destination, ResolutionContext context)
    {
        var result = new Question
        {
            QuestionText = source.QuestionText,
            PlainTextOptionPresent = source.PlainTextOptionPresent,
            IsSingleChoice = source.IsSingleChoice,
            IsOptional = source.IsOptional,
        };

        result.AnswerOptions.AddRange(source.AnswerOptions);
        if (source.AnswerLevels is not null)
        {
            result.AnswerLevels.AddRange(source.AnswerLevels);
        }

        return result;
    }
}

file sealed class GrpcAnswerToCoreAnswerConverter : ITypeConverter<Answer, CoreAnswer>
{
    public CoreAnswer Convert(Answer source, CoreAnswer destination, ResolutionContext context)
    {
        return new CoreAnswer
        {
            SelectedOptions = source.SelectedOptions
                .Select(context.Mapper.Map<CoreOptionAnswer>)
                .ToList(),
            TextAnswer = string.IsNullOrWhiteSpace(source.TextAnswer) ? null : source.TextAnswer,
            IsSkipped = source.IsSkipped,
        };
    }
}

file sealed class CoreAnswerToGrpcAnswerConverter : ITypeConverter<CoreAnswer, Answer>
{
    public Answer Convert(CoreAnswer source, Answer destination, ResolutionContext context)
    {
        var result = new Answer
        {
            TextAnswer = source.TextAnswer ?? string.Empty,
            IsSkipped = source.IsSkipped,
        };

        result.SelectedOptions.AddRange(source.SelectedOptions.Select(context.Mapper.Map<OptionAnswer>));
        return result;
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

file sealed class CoreOptionAnswerToGrpcOptionAnswerConverter : ITypeConverter<CoreOptionAnswer, OptionAnswer>
{
    public OptionAnswer Convert(CoreOptionAnswer source, OptionAnswer destination, ResolutionContext context)
    {
        return new OptionAnswer
        {
            OptionId = source.OptionId,
            SelectedLevel = source.SelectedLevel ?? -1,
        };
    }
}

file sealed class GrpcUserProfileToCoreUserProfileConverter : ITypeConverter<UserProfile, CoreUserProfile>
{
    public CoreUserProfile Convert(UserProfile source, CoreUserProfile destination, ResolutionContext context)
    {
        return new CoreUserProfile
        {
            Cluster = source.Cluster,
            Specializations = source.Specializations
                .Select(context.Mapper.Map<CoreSpecialization>)
                .ToList(),
            Skills = source.Skills
                .Select(context.Mapper.Map<CoreSkill>)
                .ToList(),
            Tools = source.Tools
                .Select(context.Mapper.Map<CoreTool>)
                .ToList(),
            PreferredDomains = source.PreferredDomains
                .Select(context.Mapper.Map<CoreDomain>)
                .ToList(),
        };
    }
}

file sealed class CoreUserProfileToGrpcUserProfileConverter : ITypeConverter<CoreUserProfile, UserProfile>
{
    public UserProfile Convert(CoreUserProfile source, UserProfile destination, ResolutionContext context)
    {
        var result = new UserProfile
        {
            Cluster = source.Cluster,
        };

        result.Specializations.AddRange(source.Specializations.Select(context.Mapper.Map<Specialization>));
        result.Skills.AddRange(source.Skills.Select(context.Mapper.Map<Skill>));
        result.Tools.AddRange(source.Tools.Select(context.Mapper.Map<Tool>));
        result.PreferredDomains.AddRange(source.PreferredDomains.Select(context.Mapper.Map<Domain>));

        return result;
    }
}

file sealed class GrpcFormElementToCoreFormElementConverter : ITypeConverter<FormElement, CoreFormElement>
{
    public CoreFormElement Convert(FormElement source, CoreFormElement destination, ResolutionContext context)
    {
        return source.PayloadCase switch
        {
            FormElement.PayloadOneofCase.Question => new CoreFormElement
            {
                Question = context.Mapper.Map<CoreQuestion>(source.Question),
            },
            FormElement.PayloadOneofCase.UserProfile => new CoreFormElement
            {
                UserProfile = context.Mapper.Map<CoreUserProfile>(source.UserProfile),
            },
            _ => new CoreFormElement(),
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

file sealed class GrpcSkillDominanceLevelToCoreConverter
    : ITypeConverter<SkillDominanceLevel, CoreSkillDominanceLevel>
{
    public CoreSkillDominanceLevel Convert(
        SkillDominanceLevel source,
        CoreSkillDominanceLevel destination,
        ResolutionContext context)
    {
        return source switch
        {
            SkillDominanceLevel.Core => CoreSkillDominanceLevel.Core,
            SkillDominanceLevel.Important => CoreSkillDominanceLevel.Important,
            SkillDominanceLevel.Secondary => CoreSkillDominanceLevel.Secondary,
            SkillDominanceLevel.Limited => CoreSkillDominanceLevel.Limited,
            _ => CoreSkillDominanceLevel.Limited,
        };
    }
}

file sealed class CoreSkillDominanceLevelToGrpcConverter
    : ITypeConverter<CoreSkillDominanceLevel, SkillDominanceLevel>
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

file sealed class GrpcToolUsageFrequencyToCoreConverter
    : ITypeConverter<ToolUsageFrequency, CoreToolUsageFrequency>
{
    public CoreToolUsageFrequency Convert(
        ToolUsageFrequency source,
        CoreToolUsageFrequency destination,
        ResolutionContext context)
    {
        return source switch
        {
            ToolUsageFrequency.Core => CoreToolUsageFrequency.Core,
            ToolUsageFrequency.Regular => CoreToolUsageFrequency.Regular,
            ToolUsageFrequency.Occasional => CoreToolUsageFrequency.Occasional,
            ToolUsageFrequency.Rare => CoreToolUsageFrequency.Rare,
            _ => CoreToolUsageFrequency.Rare,
        };
    }
}

file sealed class CoreToolUsageFrequencyToGrpcConverter
    : ITypeConverter<CoreToolUsageFrequency, ToolUsageFrequency>
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
