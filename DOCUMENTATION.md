# Interview Service Documentation

Generated from production source XML summary comments. Testing-only projects and removed experiment artifacts are intentionally excluded.

## Architecture

- `InterviewGrpcContracts` contains the public protobuf/gRPC contract package.
- `InterviewService.Core` contains domain entities, value models, setup identity rules, and prompt marker types.
- `InterviewService.Application` contains use cases, repository contracts, prompt rendering, setup catalog loading, AI request building, and converter routing.
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

### `public record InterviewPrompt`

Prompt marker for interview prompt templates and their parameter type.

Source: `InterviewService.Core/Models/InterviewPrompt.cs`

### `public record InterviewPromptParameters`

Parameters rendered into an interview prompt before sending it to the text AI service.

Source: `InterviewService.Core/Models/InterviewPromptParameters.cs`

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

### `public interface IInterviewAiConverter`

Routes an interview in a setup group into a concrete AI request.

Source: `InterviewService.Application/Abstractions/Converters/IInterviewAiConverter.cs`

### `public interface IInterviewAiConverterFactory`

Resolves interview AI converters by setup group name.

Source: `InterviewService.Application/Abstractions/Converters/IInterviewAiConverterFactory.cs`

### `public interface IInterviewAiRequestBuilder`

Builds text AI requests from prompt names and interview state.

Source: `InterviewService.Application/Abstractions/Converters/IInterviewAiRequestBuilder.cs`

### `public interface IInterviewPromptParametersFactory`

Builds prompt parameters from an interview transcript and schema information.

Source: `InterviewService.Application/Abstractions/Prompts/IInterviewPromptParametersFactory.cs`

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

### `public interface IInterviewUseCase`

Application facade for creating interviews, submitting answers, and reading the next step.

Source: `InterviewService.Application/Abstractions/UseCases/IInterviewUseCase.cs`

### `public interface IInterviewLockProvider`

Provides per-interview asynchronous locks for atomic answer processing and archiving.

Source: `InterviewService.Application/Abstractions/Utilities/IInterviewLockProvider.cs`

### `public interface IUnitOfWork`

Represents an explicit async commit boundary for staged persistence work.

Source: `InterviewService.Application/Abstractions/Utilities/IUnitOfWork.cs`

### `public static class InterviewApplicationServiceCollectionExtensions`

Registers application-layer services, prompt services, AI converters, and use cases.

Source: `InterviewService.Application/DependencyInjection/InterviewApplicationServiceCollectionExtensions.cs`

### `public sealed class ApplicationEmbeddedPromptTemplateTextReader`

Reads embedded prompt template files from the Application assembly.

Source: `InterviewService.Application/Services/ApplicationEmbeddedPromptTemplateTextReader.cs`

### `public sealed class GeneralSetupConverter`

Routes general setup interviews to IT or design prompt templates based on the cluster answer.

Source: `InterviewService.Application/Services/GeneralSetupConverter.cs`

### `public sealed class InterviewAiConverterFactory`

Default implementation that indexes converters by setup group name.

Source: `InterviewService.Application/Services/InterviewAiConverterFactory.cs`

### `public sealed class InterviewAiRequestBuilder`

Default AI request builder that renders prompts and appends interview transcript chat history.

Source: `InterviewService.Application/Services/InterviewAiRequestBuilder.cs`

### `public class InterviewPromptParametersFactory`

Creates prompt parameters from interview setup, transcript, schemas, and required-step context.

Source: `InterviewService.Application/Services/InterviewPromptParametersFactory.cs`

### `public sealed class InterviewUseCase`

Coordinates interview creation, atomic answer handling, AI next-step generation, and read-only step lookup.

Source: `InterviewService.Application/Services/InterviewUseCase.cs`

### `public sealed class PromptRenderer`

Renders prompt templates with Scriban using strongly typed parameter objects.

Source: `InterviewService.Application/Services/PromptRenderer.cs`

### `public sealed class AnswerAiValidator`

Validates AI-generated answer payloads before they are accepted by the AI services pipeline.

Source: `InterviewService.Application/Services/Validators/AnswerAiValidator.cs`

### `public sealed class FormElementAiValidator`

Validates AI-generated form elements before they become interview questions or conclusions.

Source: `InterviewService.Application/Services/Validators/FormElementAiValidator.cs`

### `public static class InterviewSetupCatalog`

Loads required setup definitions from embedded application JSON and exposes the default setup.

Source: `InterviewService.Application/Setups/InterviewSetupCatalog.cs`

## Infrastructure

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

### `public static class InfrastructureJson`

Shared JSON serializer settings for infrastructure persistence payloads.

Source: `InterviewService.Infrastructure/Serialization/InfrastructureJson.cs`

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
