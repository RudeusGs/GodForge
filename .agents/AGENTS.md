# GodForge — Agent Rules

> Mandatory rules for any AI coding agent working on the GodForge codebase. These rules are intentionally strict. If a user request conflicts with this document, follow this document unless the user explicitly updates the project conventions.

---

## 1. Source of Truth and Required Reading

Before implementing, debugging, refactoring, migrating, or generating tests, read the relevant SRS sections under `docs/SRS/`.

Use this reading order:

1. Identify the feature or bug scope and map it to the relevant `FR-*`, `NFR-*`, workflow, database table, API endpoint, and acceptance criteria.
2. Read `docs/SRS/02-architecture.md` for architectural boundaries and async-processing expectations.
3. Read `docs/SRS/03-functional/` for the target module requirements.
4. Read `docs/SRS/04-database.md` before changing entities, EF configurations, indexes, or migrations.
5. Read `docs/SRS/05-api.md` before creating or changing endpoints, request/response contracts, error codes, pagination, or status codes.
6. Read `docs/SRS/06-security.md` before implementing authentication, authorization, RBAC, secrets, tokens, Git credentials, or project-scoped access.
7. Read `docs/SRS/07-non-functional.md` for performance, observability, logging, testing, and operational constraints.
8. Read `docs/SRS/08-workflows.md` for end-to-end behavior.
9. Read `docs/SRS/10-traceability.md` and `docs/SRS/11-testing-acceptance.md` when planning tests.
10. Read `docs/SRS/12-worker-processing.md` before implementing clone, fetch, parse, analyze, diff, preview, notification dispatch, retry, DLQ, timeout, or cancellation logic.
11. Read `docs/SRS/13-deployment-operations.md` before changing Docker, environment variables, deployment, observability, or production configuration.

Do not invent behavior that is not supported by the SRS. If the SRS is unclear, implement the smallest safe behavior and leave a short TODO or ask for clarification.

---

## 2. Monorepo Layout

```text
GodForge/
├── GodForge-BE/                         # Backend — ASP.NET Core .NET 9
│   ├── src/
│   │   ├── GodForge.Api/                # API controllers, middleware, filters, DI
│   │   ├── GodForge.Application/        # CQRS use cases, DTOs, interfaces, validators
│   │   ├── GodForge.Domain/             # Entities, value objects, domain events, enums
│   │   ├── GodForge.Infrastructure/     # EF Core, PostgreSQL, Redis, RabbitMQ, MinIO, Git implementations
│   │   └── GodForge.Worker/             # Background worker host and logical workers
│   └── tests/
│       └── GodForge.UnitTests/          # xUnit tests
├── GodForge-FE/                         # Frontend — Vue 3
└── docs/
    └── SRS/                             # Software Requirements Specification
```

Do not change this layout unless the user explicitly requests an architecture change and the corresponding documentation is updated.

---

## 3. Clean Architecture Dependency Rules

These dependency rules are mandatory:

```text
GodForge.Domain         -> no project dependency
GodForge.Application    -> GodForge.Domain only
GodForge.Infrastructure -> GodForge.Application and GodForge.Domain indirectly
GodForge.Api            -> GodForge.Application + GodForge.Infrastructure
GodForge.Worker         -> GodForge.Application + GodForge.Infrastructure
```

Never add a reference from `GodForge.Domain` or `GodForge.Application` to `GodForge.Infrastructure`, `GodForge.Api`, or `GodForge.Worker`.

Never import EF Core, Redis, RabbitMQ, MinIO, Git libraries, HTTP clients, file-system implementations, or other infrastructure-specific packages in `GodForge.Domain` or `GodForge.Application`.

Declare abstractions in `GodForge.Application/Common/Interfaces` or the relevant feature module. Implement those abstractions in `GodForge.Infrastructure`.

Keep business rules in Domain/Application. Controllers, workers, infrastructure adapters, and `Program.cs` must orchestrate only; they must not own business decisions.

---

## 4. Backend Layer Responsibilities

### Domain

Use `GodForge.Domain` for enterprise rules and state transitions.

Allowed:

- Entities with private setters and domain methods.
- Value objects.
- Domain enums.
- Domain events.
- Domain-specific exceptions or error primitives.

Forbidden:

- EF Core attributes or configuration.
- Persistence logic.
- Redis, RabbitMQ, MinIO, Git, logging, HTTP, or configuration access.
- Direct clock access when testability requires an injected clock at the application boundary.

### Application

Use `GodForge.Application` for CQRS use cases, validation, authorization decisions, transaction boundaries, DTO mapping, and orchestration.

Allowed:

- Commands and queries.
- MediatR handlers.
- FluentValidation validators.
- DTOs.
- Repository and service interfaces.
- Result pattern and error mapping primitives.
- RBAC checks using application-level authorization abstractions.
- Activity-log intent creation for write operations.

Forbidden:

- EF Core queries directly in handlers.
- Direct calls to Redis, RabbitMQ, MinIO, Git libraries, or file system.
- Returning domain entities to API callers.
- Long-running work in request handlers when it should become a job.

### Infrastructure

Use `GodForge.Infrastructure` for implementation details.

Allowed:

- EF Core `DbContext`, entity configurations, migrations, repositories.
- PostgreSQL, Redis, RabbitMQ, MinIO, Git implementations.
- External service adapters.
- Encryption implementation for Git credentials and secrets.

Forbidden:

- Business rules that belong in Domain/Application.
- Permission bypasses.
- Logging secrets, raw tokens, or full sensitive payloads.

### API

Use `GodForge.Api` for HTTP concerns only.

Allowed:

- Controllers.
- Request contracts.
- Middleware.
- Filters.
- Authentication and authorization middleware registration.
- Correlation ID middleware.
- Response and error formatting.

Forbidden:

- Business logic in controllers.
- Direct database access.
- Direct Git, Redis, RabbitMQ, MinIO, or file-system logic.
- `try-catch` blocks for general exception handling in controllers.
- Returning entities directly.
- Running long tasks inside HTTP requests.

### Worker

Use `GodForge.Worker` for background job execution.

Allowed:

- Queue consumers.
- Job handlers.
- Progress reporting.
- Retry, timeout, cancellation, heartbeat, and DLQ handling.
- Calling Application services and Infrastructure implementations.

Forbidden:

- Business rules directly inside the generic host or `Program.cs`.
- One giant worker class that handles multiple unrelated queues.
- Running two conflicting jobs on the same repository without a distributed lock.
- Non-idempotent side effects.

---

## 5. Worker Architecture

`src/GodForge.Worker` is a shared worker host for MVP deployment, but it must be structured internally as multiple logical workers so they can be split into independent processes later.

Create one queue, one consumer, and one handler/service abstraction per logical job type:

- Repository Clone Worker.
- Repository Sync/Git Worker.
- Metadata Parser Worker.
- Metadata Analyzer Worker.
- Scene Diff Worker.
- Asset Preview Worker.
- Notification Dispatch Worker, if enabled for the current milestone.

Required worker rules:

- API creates a durable job record and publishes a RabbitMQ message. The API must return a lightweight response, usually `202 Accepted` with a `jobId`, for long-running work.
- PostgreSQL is the source of truth for job state. RabbitMQ is transport, not job state storage.
- Every job message must include `schemaVersion`, `messageId`, `jobId`, `projectId`, `correlationId`, `createdAt`, `attemptCount`, and enough input references to reproduce the job safely.
- Use `inputHash` or an equivalent idempotency key when repeated work could duplicate outputs.
- Each job must update status, progress, heartbeat, attempts, timestamps, and error information where applicable.
- Each job must be idempotent. Retrying a job must not duplicate metadata, activity logs, notifications, artifacts, or Git side effects.
- Use retry with backoff for transient failures. Use DLQ for poison messages, schema errors, non-retryable errors, and retry exhaustion.
- Use cancellation tokens throughout.
- Apply timeouts to long-running jobs and mark timed-out jobs clearly.
- Publish completion events only after the database and artifact outputs have been committed.
- For Git operations, acquire a Redis repository lock with an owner token and TTL, release only if the owner token matches, and renew long-running locks safely.

Never implement worker business logic directly in `Program.cs` or a shared host class. Place logic under clear `Consumers`, `Handlers`, and feature-specific services.

---

## 6. CQRS Rules

Each use case must be represented as exactly one command or query.

Commands:

- Change state.
- Return `Result` or `Result<T>`.
- Use validators for input rules.
- Execute authorization and project-scope checks before state changes.
- Record activity-log intent for important write operations.
- Produce jobs/events when work is long-running.

Queries:

- Read state only.
- Return DTOs or paged DTOs.
- Never return domain entities.
- Apply RBAC and project scope before returning data.
- Enforce pagination, query length limits, and timeouts for search and metadata queries.

Folder convention:

```text
GodForge.Application/Features/{Module}/
├── Commands/
│   └── {Action}{Entity}/
│       ├── {Action}{Entity}Command.cs
│       ├── {Action}{Entity}CommandHandler.cs
│       └── {Action}{Entity}CommandValidator.cs
├── Queries/
│   └── {Get}{Entity}/
│       ├── {Get}{Entity}Query.cs
│       └── {Get}{Entity}QueryHandler.cs
└── DTOs/
    └── {Entity}Dto.cs
```

---

## 7. API Rules

All API endpoints must follow `docs/SRS/05-api.md`.

Required:

- Use `/api/v1` routes.
- Require authentication unless the SRS explicitly marks the endpoint as public.
- Apply RBAC before processing.
- Validate request contracts before creating commands or queries.
- Include correlation ID in logs and error responses.
- Return `data` and optional `meta` for successful responses.
- Return standardized error responses with `code`, `message`, `correlationId`, and safe `details` only.
- Use pagination for list endpoints.
- Cap `pageSize`, normally at 100 unless the SRS says otherwise.
- Return `202 Accepted` and `jobId` for long-running tasks.

Forbidden:

- Returning stack traces or internal exception details in production.
- Returning domain entities directly.
- Running clone, parse, analyze, diff, preview, or large Git operations inside the HTTP request.
- Using controller-level `try-catch` to hide errors.

---

## 8. Error Handling Rules

Do not throw raw `Exception` for expected business failures.

Use one of the following:

- Domain exceptions for invalid domain state transitions.
- `Result` or `Result<T>` for expected application-level errors.
- Typed infrastructure exceptions mapped to application error codes.

Error codes must be `SCREAMING_SNAKE_CASE` and should use a domain prefix:

- `AUTH_*`
- `USER_*`
- `PROJECT_*`
- `REPOSITORY_*`
- `GIT_*`
- `METADATA_*`
- `SCENE_*`
- `ASSET_*`
- `DEPENDENCY_*`
- `HEALTH_*`
- `DIFF_*`
- `DASHBOARD_*`
- `NOTIFICATION_*`
- `ACTIVITY_*`
- `JOB_*`
- `SECURITY_*`

Do not swallow exceptions. Empty `catch { }` blocks are forbidden. When catching exceptions, log with structured fields and either map to a safe error result or rethrow for global middleware/worker supervisor handling.

---

## 9. Observability and Logging Rules

Every request and job must carry a `correlationId` across API, Application, Infrastructure, Worker, logs, activities, and notifications where applicable.

Use structured logs. Include relevant fields such as:

- `correlationId`
- `userId` or `actorId`, when available
- `projectId`, when available
- `repositoryId`, when available
- `jobId`, when available
- `jobType`, when available
- `operation`
- `durationMs`
- `status`
- `errorCode`

Never log:

- Passwords.
- JWT access tokens.
- Refresh tokens.
- Git PATs or credentials.
- Connection strings.
- Full repository URLs containing credentials.
- Full file contents.
- Sensitive raw payloads.

For Git errors, log sanitized stderr/stdout summaries only, plus correlation ID and error code.

---

## 10. Entity and Domain Model Rules

Required entity conventions:

- `Id` is `Guid`.
- `CreatedAt` and `UpdatedAt` are `DateTimeOffset` in UTC.
- Soft-deletable entities use nullable `DeletedAt`.
- Important properties must not have public setters.
- State changes must go through domain methods.
- Constructors used only by EF Core should be private or protected.
- Enforce valid state transitions in Domain or Application services, not in controllers.

Do not create anemic update flows that set arbitrary properties from requests.

---

## 11. Database and EF Core Rules

Use PostgreSQL with EF Core migrations. Do not use `EnsureCreated()`.

Required:

- Use code-first migrations.
- Keep table and column names in `snake_case`.
- Configure entities with Fluent API in `GodForge.Infrastructure/Persistence/Configurations/`.
- Declare indexes explicitly.
- Use global query filters for soft-delete when appropriate.
- Configure delete behavior intentionally.
- Use PostgreSQL-native types where applicable: `uuid`, `varchar(n)`, `text`, `integer`, `bigint`, `boolean`, `timestamptz`, `jsonb`.
- Store core business data and metadata data according to the SRS separation.
- Store only credential references or encrypted secrets, never plain text credentials.

Do not edit a migration that has already been applied to a shared database. Create a new corrective migration.

---

## 12. Redis Rules

Redis is allowed for:

- Dashboard cache.
- Short-lived job state cache when useful.
- Distributed locks.
- Rate limiting.
- Session/token metadata where specified.

Redis is not a primary database.

Key pattern:

```text
{purpose}:{entity_id}
```

Examples:

```text
dashboard:{project_id}
lock:repo:{repository_id}
rate:user:{user_id}
job:{job_id}:progress
```

Use TTLs. Repository locks should have a clear TTL and owner token.

---

## 13. RabbitMQ Rules

RabbitMQ is used for async jobs such as clone, fetch, parse, analyze, diff, preview, and notification dispatch.

Required:

- One queue per job type.
- Dedicated consumer per queue.
- Retry policy with backoff.
- Dead-letter queue for failed poison messages or retry exhaustion.
- Message schema versioning.
- Message validation before processing.
- Idempotency by `jobId` and input reference/hash.
- Correlation ID propagation.

Do not use RabbitMQ as the only place where job state exists. Persist job state in PostgreSQL.

---

## 14. MinIO Rules

Use MinIO only for artifact-like objects, such as:

- Diff artifacts.
- Thumbnails.
- Reports.
- Repository archives.
- Snapshots or previews.

Approved buckets:

- `diff-artifacts`
- `thumbnails`
- `reports`
- `archives`

Store metadata for objects where needed: `projectId`, `jobId`, `contentType`, `checksum`, `createdAt`, and retention policy.

Do not use MinIO as a replacement for PostgreSQL business data.

---

## 15. Git Operation Rules

Git operations are high-risk because they touch repository state and credentials.

Required:

- Validate RBAC before any Git operation.
- Acquire repository lock for operations that can conflict: clone, fetch, pull, push, commit, merge, branch deletion, and analyze working tree.
- Do not automatically resolve conflicts.
- Do not pass raw user input into shell commands.
- Prefer library APIs or sanitized argument arrays over shell command strings.
- Sanitize remote URLs and stderr/stdout before logging.
- Store Git PATs encrypted with AES-256-GCM or through an approved secret-management mechanism.
- Never store or log credentials in plain text.
- Require explicit user confirmation for destructive or remote-changing operations when specified by the SRS.

---

## 16. Security Rules

Never violate these rules:

- Do not hardcode secrets, connection strings, API keys, or credentials in source code.
- Use `appsettings.json`, environment variables, user secrets, or the approved secret provider.
- Do not log passwords, tokens, credentials, or full sensitive payloads.
- Do not return stack traces to clients in production.
- Do not trust frontend permission checks. Enforce RBAC server-side.
- Scope every project, metadata, search, activity, notification, and artifact query by the authenticated actor's permissions.
- Use parameterized SQL or EF query parameters.
- Validate paths and Git arguments to prevent path traversal or command injection.
- Apply rate limiting where the SRS requires it.
- JWT access token lifetime is 15 minutes unless the SRS changes it.
- Refresh token lifetime is 7 days unless the SRS changes it.
- Use refresh token rotation when implementing token refresh.
- Password hashing must use bcrypt with cost factor at least 12.

---

## 17. Coding Conventions

### Framework

- .NET 9.
- C# 13.
- EF Core 9.
- Nullable reference types enabled.
- Warnings as errors enabled.

### Style

- Use file-scoped namespaces.
- Use 4 spaces for indentation.
- Use UTF-8 and LF line endings.
- Sort `System.*` usings first.
- Do not use `#nullable disable`.
- Do not suppress warnings unless there is a documented, narrow reason.

### Naming

- `PascalCase` for public types, methods, and properties.
- `_camelCase` for private fields.
- `camelCase` for local variables and parameters.
- `SCREAMING_SNAKE_CASE` for constants and error codes.
- Interfaces start with `I`.

### `var` usage

Use `var` only when the type is obvious from the right-hand side. Prefer explicit types when readability improves.

Good:

```csharp
var project = Project.Create(name, description, actorId);
var cancellationTokenSource = new CancellationTokenSource(timeout);
```

Avoid:

```csharp
var result = await service.ExecuteAsync(request, cancellationToken);
```

Use the explicit type when the result type matters for understanding the code.

---

## 18. Testing Rules

Use xUnit and Moq for unit tests.

Test naming convention:

```text
MethodName_StateUnderTest_ExpectedBehavior
```

Required tests:

- Domain state transitions and invariants.
- Application command/query handlers.
- Validators for meaningful validation rules.
- Error mapping for important failures.
- RBAC/project-scope denial cases.
- Worker idempotency, retry, cancellation, timeout, and DLQ behavior.
- Regression tests for every bug fix.

Forbidden:

- Tests that require external services, databases, brokers, object storage, or the file system unless explicitly written as integration tests in the correct test project.
- Tests for trivial getters/setters or auto-generated behavior.
- Changing tests only to make broken code pass.

When fixing a bug, write or update the failing regression test first when feasible, then fix the implementation.

---

## 19. Documentation Rules

Update documentation when changing:

- Feature behavior.
- API contracts.
- Database schema.
- RBAC policy.
- Worker lifecycle.
- Queue/message contracts.
- Deployment configuration.
- Operational behavior.

Preserve existing comments and docstrings unless they are wrong or stale. If a comment is stale, update it in the same change that updates the behavior.

---

## 20. Git and Commit Rules

Use Conventional Commits.

Format:

```text
type(scope): subject
```

Allowed types:

```text
feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert
```

Allowed scopes:

```text
auth, user, project, repository, git, scene, asset, dependency, health, diff, dashboard, notification, activity, job, worker, queue, cache, storage, database, api, config, security, infra, test
```

Rules:

- Subject starts lowercase.
- Subject does not end with a period.
- Header is at most 100 characters.

Example:

```text
feat(auth): add jwt refresh token rotation
```

Branching:

- `main` is stable and production-ready.
- `develop` is the integration branch.
- `feature/*` is for new features.
- `fix/*` is for bug fixes.
- `release/*` is for release preparation.

---

## 21. Absolute Prohibitions

Do not:

1. Create God classes.
2. Put business logic in controllers, workers, infrastructure adapters, or `Program.cs`.
3. Add project references that violate Clean Architecture.
4. Import infrastructure packages into Domain or Application.
5. Access the database directly from controllers.
6. Return entities directly from API responses.
7. Run long clone, parse, analyze, diff, preview, or large Git operations inside HTTP requests.
8. Use empty catch blocks or swallow exceptions.
9. Add broad `try-catch` blocks just to hide failures.
10. Use `Thread.Sleep` or synchronous blocking in async code.
11. Commit code with compiler warnings.
12. Disable nullable reference types.
13. Suppress warnings without a narrow documented reason.
14. Modify `.editorconfig`, `Directory.Build.props`, `global.json`, or `commitlint.config.cjs` unless explicitly asked.
15. Add NuGet packages without explaining why they are necessary.
16. Store or log secrets in plain text.
17. Trust frontend authorization as the only permission check.
18. Auto-resolve Git conflicts without an explicit approved policy.
19. Use Redis or RabbitMQ as the primary source of truth for durable business data.
20. Publish job completion before durable outputs are committed.

---

## 22. Pre-Completion Checklist

Before saying a task is complete, verify:

- Relevant SRS sections were checked.
- Architecture boundaries are respected.
- Domain/Application do not depend on Infrastructure.
- Controllers contain no business logic.
- Long-running work is handled by jobs.
- RBAC and project-scope checks are enforced server-side.
- Correlation ID flows through request/job/log/error/activity paths.
- Error codes follow the project convention.
- Secrets and credentials are never logged or returned.
- Database names and indexes match PostgreSQL conventions.
- Job state, retry, timeout, heartbeat, and DLQ rules are respected when workers are involved.
- Unit or regression tests cover the behavior changed.
- `dotnet build` passes with zero warnings.
- `dotnet test` passes.
- Documentation is updated when behavior, API, DB, worker, or security contracts changed.