# InterviewService Changes

Date: 2026-05-09

This document summarizes the InterviewService changes made while wiring it into the ApiGateway auth/interview pipeline, fixing the AI key/configuration behavior, and clarifying interview setup identity.

## Summary

Interview setup persistence was moved from per-interview creation into service startup. On startup, InterviewService now applies database migrations, ensures Redis indexes exist, persists the setup catalog to PostgreSQL once, and warms the setup cache. Request handlers now assume setups are already available for the lifetime of the running service.

Docker configuration was also adjusted so the local container no longer overrides `TextAI:ApiKey` through environment variables. The service now uses the key from its normal `appsettings` configuration files in local development.

Setup identity was renamed from generic `Id` naming to `HashGuid`, because setup identity is not an arbitrary database id. It is a deterministic GUID derived from the last 16 bytes of the SHA256 hash of the setup group name and required question payload.

## Changed Files

- `.dockerignore`
- `compose.yaml`
- `InterviewGrpcContracts/Protos/interview.proto`
- `InterviewService.Api/InterviewService.Api.csproj`
- `InterviewService.Api/Profiles/InterviewGrpcMappingProfile.cs`
- `InterviewService.Application/Services/UseCases/GeneralInterviewUseCase.cs`
- `InterviewService.Core/Entities/InterviewSetup.cs`
- `InterviewService.Core/Invariants/InterviewSetupIdentity.cs`
- `InterviewService.Infrastructure/Data/InterviewServiceDbContext.cs`
- `InterviewService.Infrastructure/Migrations/20260509150940_RenameInterviewSetupIdToHashGuid.cs`
- `InterviewService.Infrastructure/Migrations/20260509150940_RenameInterviewSetupIdToHashGuid.Designer.cs`
- `InterviewService.Infrastructure/Migrations/InterviewServiceDbContextModelSnapshot.cs`
- `InterviewService.Infrastructure/Models/PostgresInterviewDto.cs`
- `InterviewService.Infrastructure/Models/PostgresInterviewSetupDto.cs`
- `InterviewService.Infrastructure/Models/RedisInterviewDocument.cs`
- `InterviewService.Infrastructure/Models/RedisInterviewSetupDocument.cs`
- `InterviewService.Infrastructure/Profiles/InterviewInfrastructureMappingProfile.cs`
- `InterviewService.Infrastructure/Repositories/InterviewRepository.cs`
- `InterviewService.Infrastructure/Repositories/InterviewSetupRepository.cs`
- `InterviewService.Infrastructure/Services/InfrastructureStartup.cs`
- `InterviewService.Infrastructure/Services/InterviewArchiver.cs`
- `InterviewService.Infrastructure/Stores/PostgresInterviewSetupStorage.cs`

## Setup Startup Flow

### `InfrastructureStartup`

`InfrastructureStartup.InitializeAsync` now performs startup initialization in this order:

1. Apply EF Core migrations to PostgreSQL.
2. Ensure Redis OM indexes exist for active interviews and setup cache documents.
3. Seed all setup catalog entries into setup storage and warm the cache.

A new private method, `SeedSetupsAndWarmCacheAsync`, loops over `setupCatalog.Setups.Values`, calls `setupRepository.SetAsync(setup, ct)` for each setup, and saves once at the end.

This matches the desired lifetime behavior: interview setups are treated as immutable during one service run. They can change only when the service is rebuilt/restarted with changed setup definitions.

## Interview Creation Change

### `GeneralInterviewUseCase`

`CreateNewInterviewAsync` no longer writes the setup to `IInterviewSetupRepository` on every new interview creation.

Before:

- Each `CreateNewInterviewAsync` call loaded the setup from the catalog.
- It wrote that setup to setup storage.
- Then it created and saved the interview.

After:

- Startup handles setup persistence and cache warmup.
- `CreateNewInterviewAsync` only checks cancellation, loads the setup from the catalog, creates a new `Interview`, and saves the interview.

The `IInterviewSetupRepository` constructor dependency was removed from `GeneralInterviewUseCase` because interview creation no longer needs it.

## Setup HashGuid Rename

### Domain naming

`InterviewSetup.Id` was renamed to `InterviewSetup.HashGuid`.

`InterviewSetupIdentity.ComputeId` was renamed to `ComputeHashGuid`.

The value did not change. It is still computed from the canonical setup payload:

1. Trimmed setup group name.
2. Required questions.
3. SHA256 hash of the serialized canonical payload.
4. `Guid` built from the first 16 bytes of that SHA256 hash.

The rename is semantic: callers should understand that this value is content-derived and effectively collision-resistant, not a normal generated entity id.

### Persistence naming

PostgreSQL setup identity fields were renamed:

- `interview_setups.id` -> `interview_setups.hash_guid`
- `interviews.setup_id` -> `interviews.setup_hash_guid`

The EF migration `RenameInterviewSetupIdToHashGuid` performs data-preserving column/index/foreign-key renames.

Redis setup-related fields were also renamed:

- `RedisInterviewSetupDocument.Id` -> `HashGuid`
- `RedisInterviewDocument.SetupId` -> `SetupHashGuid`

The Redis document prefixes were bumped from `interviews:v4:*` to `interviews:v5:*` so the service does not read stale cached JSON documents with the old field names.

### gRPC naming

The `InterviewSetup` proto field was renamed from `id` to `hash_guid` while keeping protobuf field number `1`.

That keeps the wire field compatible, because protobuf compatibility depends on field number and type, not the source-level field name.

## Idempotent Setup Persistence

### `PostgresInterviewSetupStorage`

`SetAsync` now stages the setup DTO in the repository.

`SaveChangesAsync` commits staged setup inserts with PostgreSQL `ON CONFLICT (hash_guid) DO NOTHING`. Because `HashGuid` is derived from the setup content and group name, a duplicate primary key practically means the exact same setup has already been seeded.

This keeps the repository unit-of-work boundary intact: `SetAsync` does not write to the database immediately, and startup persistence still happens when the setup repository calls `SaveChangesAsync`.

The `SaveChangesAsync` commit path wraps the staged setup inserts and EF tracked changes in one EF-managed transaction.

This keeps startup reseeding simple:

1. Stage the setup in `SetAsync`.
2. Try to insert the setup into PostgreSQL during `SaveChangesAsync`.
3. If the insert succeeds, the setup was new.
4. If the primary key already exists, the same setup is already present, so startup can continue.

## Docker And AI Key Configuration

### `compose.yaml`

Removed the `TextAI__ApiKey` environment override from the InterviewService compose service.

The service still accepts Docker-level overrides for:

- `TextAI__ApiEndpoint`
- default model alias/name
- model capability flags
- connection strings

But the API key is now resolved from normal ASP.NET Core configuration, including `appsettings.json` and `appsettings.Development.json` in local development.

### `.dockerignore`

Removed the `**/appsettings*.json` ignore rule.

This was necessary because the Docker image previously did not contain `appsettings.json` or `appsettings.Development.json`, so removing the Docker API key override alone would have left the container without the appsettings key.

Important: this is acceptable for local development because the current request was to use the key from appsettings. For production, the API key should come from a real secret provider or deployment secret, not from a file baked into the image.

## Runtime Effect

On startup, InterviewService now prepares setup storage once and then serves interview creation without repeatedly writing setup definitions.

For `/my-interview` through ApiGateway, the request path is now:

1. ApiGateway authenticates the user and resolves their local user record.
2. ApiGateway calls InterviewService gRPC `CreateNewInterview` only when the user has no stored interview.
3. InterviewService creates the interview using the already-loaded setup catalog.
4. Setup persistence is not repeated during that request.

For AI calls, Docker no longer injects `test-key` or an empty `TextAI__ApiKey`; local Docker uses the appsettings value instead.

## Verification Performed

Commands/checks run:

- `docker compose config --quiet` for the InterviewService compose file.
- Rebuilt and recreated `interview-grpc-api` from the ApiGateway compose stack.
- Confirmed `TextAI__ApiKey` is absent from the running container environment.
- Confirmed `appsettings.json` and `appsettings.Development.json` are present inside the rebuilt container.
- Confirmed `appsettings.Development.json` contains an `ApiKey` entry without printing the secret value.
- Confirmed the `interview-grpc-api` container starts successfully.
- Generated EF migration `RenameInterviewSetupIdToHashGuid`.
- Ran `dotnet build .\InterviewService.sln`.
- Ran `dotnet test .\InterviewService.sln` after the first setup-startup changes; there are currently no test projects in this solution, so the command only restored projects.

## Notes

- The setup data is still owned by InterviewService; ApiGateway remains unaware of setup internals.
- Redis is still used for active interview/setup cache behavior.
- PostgreSQL is still the persisted setup/interview backing store.
- The appsettings API key behavior is local-development friendly but should not be treated as a production secret strategy.
