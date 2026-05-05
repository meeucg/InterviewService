using AIServices.ServiceBuilders;
using InterviewService.Api.Profiles;
using InterviewService.Api.Services;
using InterviewService.Application.DependencyInjection;
using InterviewService.Infrastructure.DependencyInjection;
using InterviewService.Infrastructure.Profiles;
using InterviewService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddAutoMapper(
    _ => { },
    typeof(InterviewGrpcMappingProfile).Assembly,
    typeof(InterviewInfrastructureMappingProfile).Assembly);
builder.Services.AddAIServices(
    builder.Configuration.GetSection("TextAI"),
    builder.Configuration.GetSection("AIModels"));
builder.Services.AddInterviewApplication();
builder.Services.AddInterviewInfrastructure(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var infrastructureStartup = scope.ServiceProvider.GetRequiredService<InfrastructureStartup>();
    await infrastructureStartup.InitializeAsync();
}

app.MapGrpcService<InterviewApiService>();
app.MapGet(
    "/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
