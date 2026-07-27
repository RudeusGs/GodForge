---
name: implement-feature
description: Skill for implementing new GodForge backend features using Clean Architecture, CQRS/MediatR, ASP.NET Core .NET 9, EF Core, PostgreSQL, RabbitMQ workers, RBAC, activity logging, and SRS-driven acceptance criteria. Use when adding or changing backend functionality across Domain, Application, Infrastructure, API, Worker, persistence, and tests.
---


Use this skill to implement a new backend feature for GodForge. Always implement from the SRS outward: requirements first, domain/application rules second, infrastructure/API last.


- Treat `docs/SRS/` as the source of truth before writing code.
- Keep business rules out of controllers and infrastructure adapters.
- Keep API requests short. Long-running work such as clone, parse, analyze, diff, preview, or large Git operations must create a job and publish to RabbitMQ.
- Enforce RBAC server-side in the backend for every protected operation; never rely on frontend checks.
- Preserve correlation id across API, application handlers, domain services, worker jobs, logs, activity records, notifications, and errors.
- Return DTOs or API contracts only. Never return EF entities or domain entities directly.
- Record activity logs for important write operations, including failed operations when useful for audit/debugging.
- Do not log secrets, tokens, credentials, raw Git credentials, or sensitive repository content.
- Do not automatically resolve Git conflicts or rewrite project files unless the SRS and user action explicitly allow it.


Before making code changes, identify and write down:

- Functional requirement id, for example `FR-03`, `FR-05`, `FR-12`.
- Acceptance criteria that must pass.
- API endpoint specification from `docs/SRS/05-api.md` or equivalent SRS API section.
- Database entities, columns, indexes, delete behavior, and schema ownership from the SRS database section.
- RBAC rules from the SRS security/RBAC section.
- Workflow and activity diagram constraints for repository state, job state, and error handling.
- Whether the feature is synchronous API work or asynchronous worker/job work.

Do not start implementation if the expected behavior, permission, data model, and error cases are unclear.


Use this path only for lightweight work that can complete inside the API latency target.

Flow:

1. API Controller validates request shape and auth context.
2. Controller maps request to command/query.
3. Application handler enforces RBAC, business workflow, transaction boundary, and activity logging.
4. Domain entity/service enforces invariants.
5. Infrastructure persists data or calls external adapters through interfaces.
6. Controller returns `ApiResponse<T>` or standardized error response.


Use this path for clone, parse, analyze, diff, preview, large Git operations, large metadata processing, or anything likely to exceed API latency targets.

Flow:

1. API Controller maps request to command.
2. Application handler validates permission, repository/project state, idempotency, and job conflict rules.
3. Handler creates a job record and publishes a RabbitMQ message with `jobId`, `projectId`, `actorId`, `correlationId`, input hash, and attempt metadata.
4. API returns `202 Accepted` with job DTO.
5. Worker consumes the message, acquires required Redis/repository lock, updates heartbeat/progress, executes the engine, persists output, releases lock, updates job status, invalidates cache, and emits notification/activity events.
6. Failed worker work transitions to `Retrying`, `Failed`, or `DeadLettered` according to retry policy.


Location: `src/GodForge.Domain/`

Create only domain concepts here:


Rules:

- Use private setters and domain methods for state changes.
- Keep EF Core attributes out of domain entities unless the project has already standardized on them.
- Put invariants in entity methods or domain services, not handlers or controllers.
- Use domain events for meaningful state changes that notification, audit, dashboard invalidation, or worker orchestration may consume.
- Include soft-delete behavior when the SRS marks the entity as recoverable or retained.
- Use explicit state transitions for jobs, repository clone status, analysis status, and Git operation states.

Example:


Location: `src/GodForge.Application/`


Rules:

- Define repository/service interfaces here; implement them in Infrastructure.
- Put use-case orchestration in command/query handlers.
- Enforce RBAC in handlers or authorization services before data mutation or sensitive reads.
- Use FluentValidation for input rules not owned by the domain.
- Use `Result<T>` or project-standard result type with stable error codes from the SRS.
- Pass `CancellationToken` through every async call.
- Use `IClock` or equivalent for time to make tests deterministic.
- Use `ICorrelationContext` or equivalent to propagate correlation id.
- Add activity logging inside the transaction or outbox flow where consistency matters.
- Do not call EF Core, RabbitMQ, Redis, MinIO, or Git implementations directly from handlers; depend on interfaces.

Command template:


Async job command rules:

- Validate permission before creating the job.
- Validate project/repository state before publishing.
- Prevent conflicting jobs when the SRS requires it.
- Create the job in the database before publishing the message.
- Include `correlationId`, `actorId`, and input hash in the message.
- Return job DTO, not the eventual result.


Location: `src/GodForge.Infrastructure/`


Rules:

- Use EF Core Fluent API configuration.
- Use PostgreSQL `snake_case` table and column names.
- Keep core data and metadata schema separation aligned with the SRS.
- Store only credential references or encrypted secret references; never store Git credentials in plain text.
- Configure indexes for lookup, project scope, status, time ordering, and uniqueness constraints from the SRS.
- Use repository methods that preserve project scope and RBAC assumptions.
- Use Redis locks for repository/Git operations that can conflict.
- Wrap external calls and Git engine results in structured result objects with error code, warnings, duration, and safe stdout/stderr summaries.
- Register all services in `DependencyInjection.cs` without creating circular dependencies.

EF configuration template:


Use a worker implementation when the feature consumes RabbitMQ jobs or performs heavy processing.

Rules:

- Validate message schema before processing.
- Load the job by `jobId`; ignore or safely complete duplicate messages when idempotency says work is already done.
- Acquire any repository or project lock before touching the working tree or conflicting metadata.
- Update job `Started`, `Running`, `Progress`, `Heartbeat`, `Completed`, `Retrying`, `Failed`, or `DeadLettered` states.
- Use timeout and cancellation tokens.
- Persist output with metadata version, commit hash, or input hash where relevant.
- Emit notifications and dashboard invalidation events only after successful persistence.
- Never swallow exceptions silently; convert them to structured job errors and safe logs.


Location: `src/GodForge.Api/`


Rules:

- Controllers only coordinate request → command/query → response.
- Use API contracts for request models; do not expose application/domain internals unless that is already the project convention.
- Require `[Authorize]` for protected endpoints.
- Use route/method/status codes from the SRS API spec.
- Use `201 Created` for created resources.
- Use `202 Accepted` for async job triggers.
- Use pagination metadata for list responses.
- Include `correlationId` in error responses.
- Do not return stack traces, raw exceptions, internal paths, secrets, or unsafe details.

Controller template:


Async endpoint template:


Create tests in the appropriate test project:


Minimum coverage:

- Domain invariants and state transitions.
- Command/query handler happy path.
- Validation failures.
- Permission denied behavior.
- Repository/project/job invalid state behavior.
- Error code mapping.
- Activity log creation for write operations.
- Correlation id propagation where applicable.
- Idempotency and retry behavior for worker jobs.
- Repository lock behavior for Git operations.
- API response status and shape for endpoint tests.

Do not change tests just to match broken behavior. If an existing test is wrong because the SRS changed, document the SRS reference in the test update.


- Use stable SRS/project error codes such as `PROJECT_NAME_EXISTS`, `FORBIDDEN`, `REPOSITORY_LOCKED`, `JOB_ALREADY_RUNNING`, or module-specific equivalents.
- Map validation errors to 400.
- Map unauthorized/forbidden to 401/403.
- Map missing resources to 404 without leaking whether a resource exists outside the actor's scope.
- Map conflicts and invalid state transitions to 409.
- For worker errors, update job state and notification instead of trying to return worker exceptions through the original request.
- Include safe `details` only when useful and non-sensitive.


Use concise conventional commits:


For fixes discovered during implementation, use a separate `fix(scope): ...` commit when practical.


