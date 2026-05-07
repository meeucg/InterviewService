# Interview Service

ASP.NET gRPC microservice for running FitFlow onboarding interviews and producing a structured user profile.

The service exposes a stable gRPC contract, keeps active interviews in Redis, archives finished or stale interviews to PostgreSQL, and uses an AI interviewer to generate dynamic follow-up questions after the required setup step.

## Projects

- `InterviewGrpcContracts`: public `.proto` contract and generated gRPC models.
- `InterviewService.Core`: domain entities and models.
- `InterviewService.Application`: group-specific use cases, prompt rendering, setup catalog, AI validation, and repository contracts.
- `InterviewService.Infrastructure`: EF Core, PostgreSQL, Redis OM, migrations, storage, locking, startup, and archiving.
- `InterviewService.Api`: deployable gRPC host and DI composition root.

## Requirements

- .NET 10 SDK
- Docker Desktop
- PostgreSQL and Redis Stack when running outside Docker
- Text AI API credentials

## Configuration

Secrets are intentionally not committed. Provide them through environment variables or local `appsettings*.json` files ignored by Git.

```powershell
$env:TEXTAI_API_ENDPOINT = "https://polza.ai/api/v1"
$env:TEXTAI_API_KEY = "<your-api-key>"
$env:INTERVIEWER_MODEL_ALIAS = "Grok 4.20"
$env:INTERVIEWER_MODEL_NAME = "x-ai/grok-4.20"
```

Docker Compose also accepts:

```powershell
$env:INTERVIEW_POSTGRES_PORT = "55432"
$env:INTERVIEW_REDIS_PORT = "56379"
$env:INTERVIEW_REDIS_UI_PORT = "58001"
```

## Quick Start With Docker

From the repository root:

```powershell
dotnet restore .\InterviewService.sln
dotnet build .\InterviewService.sln
docker compose up --build
```

The gRPC API is exposed on:

```text
http://localhost:7297
```

On startup the API applies EF Core migrations, creates Redis OM indexes, and seeds the required interview setups from the embedded setup catalog.

## Local Development

Start PostgreSQL and Redis Stack:

```powershell
docker compose up interview-postgres interview-redis
```

Run the API locally:

```powershell
$env:ConnectionStrings__Postgres = "Host=localhost;Port=55432;Database=fitflow_interviews;Username=postgres;Password=postgres"
$env:ConnectionStrings__Redis = "localhost:56379"
dotnet run --project .\InterviewService.Api\InterviewService.Api.csproj
```

## EF Core Migrations

Create a new migration after persistence model changes:

```powershell
dotnet ef migrations add <MigrationName> `
  --project .\InterviewService.Infrastructure `
  --startup-project .\InterviewService.Api `
  --context InterviewServiceDbContext
```

Apply migrations manually when needed:

```powershell
dotnet ef database update `
  --project .\InterviewService.Infrastructure `
  --startup-project .\InterviewService.Api `
  --context InterviewServiceDbContext
```

The API also calls `MigrateAsync()` during infrastructure startup.

## gRPC API

The public service is `interview.InterviewGateway`.

```proto
service InterviewGateway {
  rpc CreateNewInterview (CreateNewInterviewRequest) returns (InterviewDisplayReply);
  rpc GetInterviewDisplay (GetInterviewDisplayRequest) returns (InterviewDisplayReply);
  rpc GetInterviewConclusion (GetInterviewConclusionRequest) returns (UserProfileReply);
  rpc Answer (AnswerRequest) returns (FormElementReply);
}
```

Typical flow:

1. Call `CreateNewInterview`.
2. Show `interview_display.current_question` to the user.
3. Send each user answer through `Answer`.
4. Continue until `Answer` returns `form_element.user_profile`.
5. Fetch the final profile through `GetInterviewConclusion` when needed.

## Documentation

Production source files include XML summary comments. The generated source reference lives in:

```text
DOCUMENTATION.md
```
