using AIServices.ServiceBuilders;
using FitFlow.Interview.Grpc.Contracts;
using Google.Protobuf;
using Grpc.Core;
using InterviewGrpcClientTester.Models;
using InterviewGrpcClientTester.Profiles;
using InterviewGrpcClientTester.Services;
using InterviewService.Application.DependencyInjection;

var runAiTest = args.Any(x => string.Equals(x, "--ai-test", StringComparison.OrdinalIgnoreCase));
var builderArgs = args
    .Where(x => !string.Equals(x, "--ai-test", StringComparison.OrdinalIgnoreCase))
    .ToArray();

var builder = WebApplication.CreateBuilder(builderArgs);

var interviewGrpcAddress = builder.Configuration["InterviewGrpc:Address"] ?? "https://localhost:7297";

builder.Services.AddGrpcClient<InterviewGateway.InterviewGatewayClient>(
    options =>
    {
        options.Address = new Uri(interviewGrpcAddress);
    })
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(10);
    });
builder.Services.AddAIServices(
    builder.Configuration.GetSection("TextAI"),
    builder.Configuration.GetSection("AIModels"));
builder.Services.AddInterviewAiValidators();
builder.Services.AddOptions<AiTestingOptions>()
    .Bind(builder.Configuration.GetSection(AiTestingOptions.SectionName));
builder.Services.AddAutoMapper(_ => { }, typeof(GrpcBusinessMappingProfile).Assembly);
builder.Services.AddSingleton<TesterPromptRenderer>();
builder.Services.AddSingleton<AiGrpcInterviewTester>();

var app = builder.Build();

if (runAiTest)
{
    var aiApiKey = app.Configuration["TextAI:ApiKey"];
    if (string.IsNullOrWhiteSpace(aiApiKey))
    {
        var reportsPath = app.Configuration["AiTesting:ReportsPath"] ?? "/reports";
        Directory.CreateDirectory(reportsPath);

        var reportPath = Path.Combine(
            reportsPath,
            $"MissingTextAiApiKey_{DateTimeOffset.UtcNow:yyyy-MM-dd_HH-mm-ss}.txt");
        await File.WriteAllTextAsync(
            reportPath,
            "AI gRPC tester could not start because TextAI:ApiKey is empty. Set TEXTAI_API_KEY and rerun docker compose.",
            app.Lifetime.ApplicationStopping);

        Console.Error.WriteLine("AI gRPC tester could not start because TextAI:ApiKey is empty.");
        Console.Error.WriteLine($"Failure report saved to {reportPath}");
        return 1;
    }

    var tester = app.Services.GetRequiredService<AiGrpcInterviewTester>();
    return await tester.RunAsync(app.Lifetime.ApplicationStopping);
}

app.MapGet("/",
    () => Results.Text("Use GET /start to create a new interview via gRPC."));

app.MapGet("/start",
    async (InterviewGateway.InterviewGatewayClient client, CancellationToken cancellationToken) =>
    {
        try
        {
            var reply = await client.CreateNewInterviewAsync(
                new CreateNewInterviewRequest(),
                cancellationToken: cancellationToken);
            var responseJson = JsonFormatter.Default.Format(reply);

            Console.WriteLine($"CreateNewInterview response from {interviewGrpcAddress}:");
            Console.WriteLine(responseJson);

            return Results.Content(responseJson, "application/json");
        }
        catch (RpcException ex)
        {
            Console.Error.WriteLine($"gRPC call failed: {ex.Status.StatusCode} - {ex.Status.Detail}");

            return Results.Problem(
                title: "gRPC call failed",
                detail: ex.Status.Detail,
                statusCode: ToHttpStatusCode(ex.Status.StatusCode));
        }
    });

app.Run();

return 0;

static int ToHttpStatusCode(StatusCode statusCode)
{
    return statusCode switch
    {
        StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
        StatusCode.NotFound => StatusCodes.Status404NotFound,
        StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
        StatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
        StatusCode.Unavailable => StatusCodes.Status503ServiceUnavailable,
        StatusCode.DeadlineExceeded => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status500InternalServerError
    };
}
