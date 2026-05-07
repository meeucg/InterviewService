# Interview Service Documentation

Generated from production source XML summary comments. Testing-only projects and removed experiment artifacts are intentionally excluded.

## Architecture

- `InterviewGrpcContracts` contains the public protobuf/gRPC contract package.
- `InterviewService.Core` contains domain entities, value models, setup identity rules, and prompt base types.
- `InterviewService.Application` contains group-specific use cases, prompt rendering, setup catalog loading, AI validators, and repository contracts.
- `InterviewService.Infrastructure` contains PostgreSQL, Redis OM, EF migrations, storage implementations, startup initialization, locking, and archival.
- `InterviewService.Api` is the deployable gRPC host and composition root.

## Core

### `public abstract record Prompt<T>`

Base type for strongly typed prompt definitions used by interview AI request construction.

Source: `InterviewService.Core/Abstractions/Prompt.cs`

### `public abstract record PromptParameters`

Base type for strongly typed prompt parameter objects.

Source: `InterviewService.Core/Abstractions/PromptParameters.cs`

### `public class Interview`

Domain aggregate that owns interview progress, turn rules, required answers, dynamic steps, and conclusion state.

Source: `InterviewService.Core/Entities/Interview.cs`

### `public sealed class InterviewSetup`

Immutable interview setup version identified by a content hash and grouped by a stable business name.

Source: `InterviewService.Core/Entities/InterviewSetup.cs`

### `public static class InterviewSetupIdentity`

Computes deterministic setup identifiers from canonical setup payloads.

Source: `InterviewService.Core/Invariants/InterviewSetupIdentity.cs`

### `public record Answer`

Represents a user answer to a required or dynamic interview question.

Source: `InterviewService.Core/Models/Answer.cs`

### `public record Domain`

Describes a professional domain detected for the interviewed user.

Source: `InterviewService.Core/Models/Domain.cs`

### `public record FormElement`

Represents the next interview UI element: either a question or the final user profile.

Source: `InterviewService.Core/Models/FormElement.cs`

### `public record InterviewStep`

Pairs an interview question with the answer that was given for transcript construction.

Source: `InterviewService.Core/Models/InterviewStep.cs`

### `public record OptionAnswer`

Represents a selected option answer, including optional free text when supported by the question.

Source: `InterviewService.Core/Models/OptionAnswer.cs`

### `public record Question`

Represents a question shown to the user, including choice and free-text constraints.

Source: `InterviewService.Core/Models/Question.cs`

### `public record Skill`

Represents a skill detected in the final user profile.

Source: `InterviewService.Core/Models/Skill.cs`

### `public enum SkillDominanceLevel`

Describes how central a detected skill is to the user profile.

Source: `InterviewService.Core/Models/SkillDominanceLevel.cs`

### `public record Specialization`

Represents the primary professional specialization inferred for the user.

Source: `InterviewService.Core/Models/Specialization.cs`

### `public record Tool`

Represents a tool or technology detected in the final user profile.

Source: `InterviewService.Core/Models/Tool.cs`

### `public enum ToolUsageFrequency`

Describes how frequently the user appears to use a detected tool.

Source: `InterviewService.Core/Models/ToolUsageFrequency.cs`

### `public record UserProfile`

Final structured profile produced at the end of an interview.

Source: `InterviewService.Core/Models/UserProfile.cs`

## Application

### `public interface IPromptRenderer`

Renders strongly typed prompt templates into final prompt text.

Source: `InterviewService.Application/Abstractions/Prompts/IPromptRenderer.cs`

### `public interface IPromptTemplateTextReader`

Reads prompt template text by prompt name from the application prompt source.

Source: `InterviewService.Application/Abstractions/Prompts/IPromptTemplateTextReader.cs`

### `public interface IInterviewRepository`

Domain repository for interview aggregates with Redis-first and archive fallback behavior.

Source: `InterviewService.Application/Abstractions/Repositories/IInterviewRepository.cs`

### `public interface IInterviewSetupRepository`

Domain repository for immutable interview setups.

Source: `InterviewService.Application/Abstractions/Repositories/IInterviewSetupRepository.cs`

### `public interface IRepository<T>`

Generic repository contract with unit-of-work commit semantics.

Source: `InterviewService.Application/Abstractions/Repositories/IRepository.cs`

### `public interface IInterviewSetupCatalog`

Provides interview setup definitions loaded by the application layer.

Source: `InterviewService.Application/Abstractions/Setups/IInterviewSetupCatalog.cs`

### `public interface IInterviewUseCase`

Application facade for one interview setup group, covering creation, answer submission, and next-step lookup.

Source: `InterviewService.Application/Abstractions/UseCases/IInterviewUseCase.cs`

### `public interface IInterviewUseCaseFactory`

Resolves interview use cases by setup group name and exposes the current default use case.

Source: `InterviewService.Application/Abstractions/UseCases/IInterviewUseCaseFactory.cs`

### `public interface IInterviewLockProvider`

Provides per-interview asynchronous locks for atomic answer processing and archiving.

Source: `InterviewService.Application/Abstractions/Utilities/IInterviewLockProvider.cs`

### `public interface IUnitOfWork`

Represents an explicit async commit boundary for staged persistence work.

Source: `InterviewService.Application/Abstractions/Utilities/IUnitOfWork.cs`

### `public static class InterviewApplicationServiceCollectionExtensions`

Registers application-layer services, prompt services, AI validators, and use cases.

Source: `InterviewService.Application/DependencyInjection/InterviewApplicationServiceCollectionExtensions.cs`

### `public sealed class ApplicationEmbeddedPromptTemplateTextReader`

Reads embedded prompt template files from the Application assembly.

Source: `InterviewService.Application/Services/Prompts/ApplicationEmbeddedPromptTemplateTextReader.cs`

### `public sealed class PromptRenderer`

Renders prompt templates with Scriban using strongly typed parameter objects.

Source: `InterviewService.Application/Services/Prompts/PromptRenderer.cs`

### `public sealed class InterviewSetupCatalog`

Loads setup definitions from embedded application JSON.

Source: `InterviewService.Application/Services/Setups/InterviewSetupCatalog.cs`

### `public sealed class GeneralInterviewUseCase`

Use case for the general setup group, including prompt selection and AI next-step generation.

Source: `InterviewService.Application/Services/UseCases/GeneralInterviewUseCase.cs`

### `public sealed class InterviewUseCaseFactory`

Default implementation that indexes interview use cases by setup group name.

Source: `InterviewService.Application/Services/UseCases/InterviewUseCaseFactory.cs`

### `public sealed class AnswerAiValidator`

Validates AI-generated answer payloads before they are accepted by the AI services pipeline.

Source: `InterviewService.Application/Services/Validators/AnswerAiValidator.cs`

### `public sealed class FormElementAiValidator`

Validates AI-generated form elements before they become interview questions or conclusions.

Source: `InterviewService.Application/Services/Validators/FormElementAiValidator.cs`

### `internal static class InterviewAiValidationRules`

Shared validation rules for AI-generated interview payloads.

Source: `InterviewService.Application/Services/Validators/InterviewAiValidationRules.cs`

### `public sealed class QuestionAiValidator`

Validates AI-generated question payloads before they are accepted by the AI services pipeline.

Source: `InterviewService.Application/Services/Validators/QuestionAiValidator.cs`

### `public sealed class UserProfileAiValidator`

Validates AI-generated user profiles before they are accepted by the AI services pipeline.

Source: `InterviewService.Application/Services/Validators/UserProfileAiValidator.cs`

## Infrastructure

### `public interface IActiveInterviewStorage`

Storage contract for active interview documents kept in Redis.

Source: `InterviewService.Infrastructure/Abstractions/IActiveInterviewStorage.cs`

### `public interface IArchivedInterviewStorage`

Storage contract for archived interview DTOs kept in PostgreSQL.

Source: `InterviewService.Infrastructure/Abstractions/IArchivedInterviewStorage.cs`

### `public sealed class InterviewServiceDbContext`

EF Core DbContext for archived interviews and immutable interview setup rows.

Source: `InterviewService.Infrastructure/Data/InterviewServiceDbContext.cs`

### `public sealed class InterviewServiceDbContextFactory`

Design-time DbContext factory used by EF Core tooling.

Source: `InterviewService.Infrastructure/Data/InterviewServiceDbContextFactory.cs`

### `public static class InterviewInfrastructureServiceCollectionExtensions`

Registers infrastructure persistence, cache, mapping, startup, and archival services.

Source: `InterviewService.Infrastructure/DependencyInjection/InterviewInfrastructureServiceCollectionExtensions.cs`

### `public sealed class PostgresInterviewDto`

PostgreSQL DTO for archived interview state stored as JSONB.

Source: `InterviewService.Infrastructure/Models/PostgresInterviewDto.cs`

### `public sealed class PostgresInterviewSetupDto`

PostgreSQL DTO for immutable interview setup state stored as JSONB.

Source: `InterviewService.Infrastructure/Models/PostgresInterviewSetupDto.cs`

### `public sealed class RedisInterviewDocument`

Redis OM document for active or not-yet-archived interview state.

Source: `InterviewService.Infrastructure/Models/RedisInterviewDocument.cs`

### `public sealed class RedisInterviewSetupDocument`

Redis OM document for cached immutable interview setup state.

Source: `InterviewService.Infrastructure/Models/RedisInterviewSetupDocument.cs`

### `internal sealed record InterviewPayload`

JSON payload shape for persisted interview aggregate state.

Source: `InterviewService.Infrastructure/Models/Serialization/InterviewPayload.cs`

### `internal sealed record InterviewSetupPayload`

JSON payload shape for persisted immutable interview setup state.

Source: `InterviewService.Infrastructure/Models/Serialization/InterviewSetupPayload.cs`

### `public sealed class InfrastructureJsonOptions`

Configures JSON serializer settings for infrastructure persistence payloads.

Source: `InterviewService.Infrastructure/Options/InfrastructureJsonOptions.cs`

### `public sealed class InterviewArchivingOptions`

Configures inactivity threshold and sweep cadence for Redis-to-PostgreSQL archival.

Source: `InterviewService.Infrastructure/Options/InterviewArchivingOptions.cs`

### `public sealed class InterviewInfrastructureMappingProfile`

AutoMapper profile for domain, PostgreSQL DTO, and Redis document conversions.

Source: `InterviewService.Infrastructure/Profiles/InterviewInfrastructureMappingProfile.cs`

### `public sealed class InterviewRepository`

Domain interview repository that composes Redis active storage and PostgreSQL archive storage.

Source: `InterviewService.Infrastructure/Repositories/InterviewRepository.cs`

### `public sealed class InterviewSetupRepository`

Domain setup repository that keeps PostgreSQL as source of truth and warms Redis cache.

Source: `InterviewService.Infrastructure/Repositories/InterviewSetupRepository.cs`

### `public sealed class InfrastructureStartup`

Runs infrastructure startup tasks such as migrations, Redis index creation, and setup seeding.

Source: `InterviewService.Infrastructure/Services/InfrastructureStartup.cs`

### `public sealed class InMemoryInterviewLockProvider`

In-process implementation of per-interview asynchronous locks.

Source: `InterviewService.Infrastructure/Services/InMemoryInterviewLockProvider.cs`

### `public sealed class InterviewArchiver`

Hosted archival worker that moves stale or finished Redis interviews into PostgreSQL.

Source: `InterviewService.Infrastructure/Services/InterviewArchiver.cs`

### `public sealed class PostgresInterviewSetupStorage`

Scoped PostgreSQL storage unit for setup DTO persistence.

Source: `InterviewService.Infrastructure/Stores/PostgresInterviewSetupStorage.cs`

### `public sealed class PostgresInterviewStorage`

Scoped PostgreSQL storage unit for archived interview DTO persistence.

Source: `InterviewService.Infrastructure/Stores/PostgresInterviewStorage.cs`

### `public sealed class RedisInterviewSetupStorage`

Scoped Redis OM storage unit for setup cache documents.

Source: `InterviewService.Infrastructure/Stores/RedisInterviewSetupStorage.cs`

### `public sealed class RedisInterviewStorage`

Scoped Redis OM storage unit for active interview documents and archival queries.

Source: `InterviewService.Infrastructure/Stores/RedisInterviewStorage.cs`

## Api

### `public sealed class InterviewGrpcMappingProfile`

AutoMapper profile that maps between domain models and gRPC contract models.

Source: `InterviewService.Api/Profiles/InterviewGrpcMappingProfile.cs`

### `public class InterviewApiService`

gRPC endpoint implementation for creating interviews, answering questions, and reading interview results.

Source: `InterviewService.Api/Services/InterviewApiService.cs`
