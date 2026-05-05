using AutoMapper;
using FitFlow.Interview.Grpc.Contracts;
using Grpc.Core;
using InterviewService.Application.Abstractions;
using InterviewService.Application.Abstractions.Repositories;
using InterviewService.Application.Abstractions.UseCases;

namespace InterviewService.Api.Services;

public class InterviewApiService(
    IInterviewUseCase interviewUseCase,
    IInterviewRepository interviewRepository,
    IMapper mapper) : InterviewGateway.InterviewGatewayBase
{
    public override async Task<InterviewDisplayReply> CreateNewInterview(
        CreateNewInterviewRequest request,
        ServerCallContext context)
    {
        var interview = await interviewUseCase.CreateNewInterviewAsync(context.CancellationToken);

        return new InterviewDisplayReply
        {
            InterviewDisplay = mapper.Map<InterviewDisplay>(interview),
        };
    }

    public override async Task<InterviewDisplayReply> GetInterviewDisplay(
        GetInterviewDisplayRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var interviewId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid interview id"));
        }

        var interview = await interviewRepository.GetAsync(interviewId, context.CancellationToken);
        if (interview is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Interview '{interviewId}' was not found"));
        }

        return new InterviewDisplayReply
        {
            InterviewDisplay = mapper.Map<InterviewDisplay>(interview),
        };
    }

    public override async Task<UserProfileReply> GetInterviewConclusion(
        GetInterviewConclusionRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var interviewId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid interview id"));
        }

        var conclusion = await interviewRepository.GetInterviewConclusionAsync(interviewId, context.CancellationToken);
        if (conclusion is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Interview '{interviewId}' conclusion was not found"));
        }

        return new UserProfileReply
        {
            UserProfile = mapper.Map<UserProfile>(conclusion),
        };
    }

    public override async Task<FormElementReply> Answer(AnswerRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var interviewId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid interview id"));
        }

        if (request.Answer is null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Answer payload is required"));
        }

        InterviewService.Core.Models.FormElement? result;
        try
        {
            await interviewUseCase.SubmitAnswerAsync(
                interviewId,
                mapper.Map<InterviewService.Core.Models.Answer>(request.Answer),
                context.CancellationToken);

            result = await interviewUseCase.GetNextStepAsync(interviewId, context.CancellationToken);
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Interview '{interviewId}' was not found"));
        }

        if (result is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Interview '{interviewId}' next step was not found"));
        }

        return new FormElementReply
        {
            FormElement = mapper.Map<FormElement>(result),
        };
    }
}
