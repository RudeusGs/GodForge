---
name: implement-feature
description: Skill for implementing new GodForge backend features using Clean Architecture, CQRS/MediatR, ASP.NET Core .NET 9, EF Core, PostgreSQL, RabbitMQ workers, RBAC, activity logging, and SRS-driven acceptance criteria. Use when adding or changing backend functionality across Domain, Application, Infrastructure, API, Worker, persistence, and tests.
---

# Implement Feature

Use this skill to implement a new backend feature for GodForge. Always implement from the SRS outward: requirements first, domain/application rules second, infrastructure/API last.

## Core Rules

- Treat `docs/SRS/` as the source of truth before writing code.
- Keep business rules out of controllers and infrastructure adapters.
- Keep API requests short. Long-running work such as clone, parse, analyze, diff, preview, or large Git operations must create a job and publish to RabbitMQ.
- Enforce RBAC server-side in the backend for every protected operation; never rely on frontend checks.
- Preserve correlation id across API, application handlers, domain services, worker jobs, logs, activity records, notifications, and errors.
- Return DTOs or API contracts only. Never return EF entities or domain entities directly.
- Record activity logs for important write operations, including failed operations when useful for audit/debugging.
- Do not log secrets, tokens, credentials, raw Git credentials, or sensitive repository content.
- Do not automatically resolve Git conflicts or rewrite project files unless the SRS and user action explicitly allow it.

## 1. Read the SRS Before Coding

Before making code changes, identify and write down:

- Functional requirement id, for example `FR-03`, `FR-05`, `FR-12`.
- Acceptance criteria that must pass.
- API endpoint specification from `docs/SRS/05-api.md` or equivalent SRS API section.
- Database entities, columns, indexes, delete behavior, and schema ownership from the SRS database section.
- RBAC rules from the SRS security/RBAC section.
- Workflow and activity diagram constraints for repository state, job state, and error handling.
- Whether the feature is synchronous API work or asynchronous worker/job work.

Do not start implementation if the expected behavior, permission, data model, and error cases are unclear.

## 2. Choose the Correct Architectural Path

### Synchronous feature

Use this path only for lightweight work that can complete inside the API latency target.

Flow:

1. API Controller validates request shape and auth context.
2. Controller maps request to command/query.
3. Application handler enforces RBAC, business workflow, transaction boundary, and activity logging.
4. Domain entity/service enforces invariants.
5. Infrastructure persists data or calls external adapters through interfaces.
6. Controller returns `ApiResponse<T>` or standardized error response.

### Asynchronous feature

Use this path for clone, parse, analyze, diff, preview, large Git operations, large metadata processing, or anything likely to exceed API latency targets.

Flow:

1. API Controller maps request to command.
2. Application handler validates permission, repository/project state, idempotency, and job conflict rules.
3. Handler creates a job record and publishes a RabbitMQ message with `jobId`, `projectId`, `actorId`, `correlationId`, input hash, and attempt metadata.
4. API returns `202 Accepted` with job DTO.
5. Worker consumes the message, acquires required Redis/repository lock, updates heartbeat/progress, executes the engine, persists output, releases lock, updates job status, invalidates cache, and emits notification/activity events.
6. Failed worker work transitions to `Retrying`, `Failed`, or `DeadLettered` according to retry policy.

## 3. Implement in Dependency Order

### Step 1: Domain Layer

Location: `src/GodForge.Domain/`

Create only domain concepts here:

```text
GodForge.Domain/
├── Entities/
├── ValueObjects/
├── Enums/
├── Events/
├── Exceptions/
└── Services/
```

Rules:

- Use private setters and domain methods for state changes.
- Keep EF Core attributes out of domain entities unless the project has already standardized on them.
- Put invariants in entity methods or domain services, not handlers or controllers.
- Use domain events for meaningful state changes that notification, audit, dashboard invalidation, or worker orchestration may consume.
- Include soft-delete behavior when the SRS marks the entity as recoverable or retained.
- Use explicit state transitions for jobs, repository clone status, analysis status, and Git operation states.

Example:

```csharp
namespace GodForge.Domain.Entities;

public sealed class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public string Visibility { get; private set; } = default!;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private Project() { }

    public static Project Create(
        string name,
        string slug,
        string? description,
        string visibility,
        Guid createdBy,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Project name is required.");

        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug,
            Description = description,
            Visibility = visibility,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(string name, string? description, DateTimeOffset now)
    {
        if (DeletedAt is not null)
            throw new DomainException("Deleted projects cannot be updated.");

        Name = name.Trim();
        Description = description;
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (DeletedAt is not null) return;
        DeletedAt = now;
        UpdatedAt = now;
    }
}
```

### Step 2: Application Layer

Location: `src/GodForge.Application/`

```text
GodForge.Application/
├── Common/
│   ├── Interfaces/
│   ├── Models/
│   ├── Security/
│   ├── Behaviors/
│   └── Errors/
└── Features/
    └── {Module}/
        ├── Commands/
        ├── Queries/
        ├── DTOs/
        └── Mapping/
```

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

```csharp
namespace GodForge.Application.Features.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    string Visibility,
    Guid ActorId,
    string CorrelationId) : IRequest<Result<ProjectDto>>;

public sealed class CreateProjectCommandHandler
    : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectRepository _projects;
    private readonly IAuthorizationService _authorization;
    private readonly IActivityLogWriter _activityLog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateProjectCommandHandler(
        IProjectRepository projects,
        IAuthorizationService authorization,
        IActivityLogWriter activityLog,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _projects = projects;
        _authorization = authorization;
        _activityLog = activityLog;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ProjectDto>> Handle(
        CreateProjectCommand request,
        CancellationToken cancellationToken)
    {
        var allowed = await _authorization.CanCreateProjectAsync(
            request.ActorId,
            cancellationToken);

        if (!allowed)
            return Errors.Forbidden("PROJECT_CREATE_FORBIDDEN");

        if (await _projects.NameExistsAsync(request.ActorId, request.Name, cancellationToken))
            return Errors.Conflict("PROJECT_NAME_EXISTS");

        var now = _clock.UtcNow;
        var slug = SlugGenerator.From(request.Name);
        var project = Project.Create(
            request.Name,
            slug,
            request.Description,
            request.Visibility,
            request.ActorId,
            now);

        await _projects.AddAsync(project, cancellationToken);
        await _activityLog.WriteAsync(
            ActivityActions.ProjectCreated,
            request.ActorId,
            project.Id,
            status: "Succeeded",
            correlationId: request.CorrelationId,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProjectDto.From(project);
    }
}
```

Async job command rules:

- Validate permission before creating the job.
- Validate project/repository state before publishing.
- Prevent conflicting jobs when the SRS requires it.
- Create the job in the database before publishing the message.
- Include `correlationId`, `actorId`, and input hash in the message.
- Return job DTO, not the eventual result.

### Step 3: Infrastructure Layer

Location: `src/GodForge.Infrastructure/`

```text
GodForge.Infrastructure/
├── Persistence/
│   ├── GodForgeDbContext.cs
│   ├── Configurations/
│   ├── Repositories/
│   └── Migrations/
├── Messaging/
│   └── RabbitMq/
├── Caching/
│   └── Redis/
├── Storage/
│   └── MinIO/
├── Git/
├── Observability/
└── DependencyInjection.cs
```

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

```csharp
namespace GodForge.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", "core");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasColumnName("slug")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(p => new { p.CreatedBy, p.Slug })
            .IsUnique()
            .HasDatabaseName("ux_projects_created_by_slug");

        builder.HasIndex(p => p.DeletedAt)
            .HasDatabaseName("ix_projects_deleted_at");

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
```

### Step 4: Worker Layer

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

### Step 5: API Layer

Location: `src/GodForge.Api/`

```text
GodForge.Api/
├── Controllers/
├── Contracts/
├── Middleware/
├── Filters/
└── Extensions/
```

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

```csharp
namespace GodForge.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICorrelationContext _correlation;

    public ProjectsController(ISender sender, ICorrelationContext correlation)
    {
        _sender = sender;
        _correlation = correlation;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand(
            request.Name,
            request.Description,
            request.Visibility,
            User.GetUserId(),
            _correlation.CorrelationId);

        var result = await _sender.Send(command, cancellationToken);

        return result.Match(
            value => CreatedAtAction(
                nameof(GetById),
                new { id = value.Id },
                new ApiResponse<ProjectDto>(value)),
            error => error.ToActionResult(_correlation.CorrelationId));
    }
}
```

Async endpoint template:

```csharp
[HttpPost("{id:guid}/analyze")]
[ProducesResponseType(typeof(ApiResponse<JobDto>), StatusCodes.Status202Accepted)]
public async Task<IActionResult> Analyze(
    Guid id,
    CancellationToken cancellationToken)
{
    var command = new RequestAnalyzeCommand(
        id,
        User.GetUserId(),
        _correlation.CorrelationId);

    var result = await _sender.Send(command, cancellationToken);

    return result.Match(
        job => Accepted(new ApiResponse<JobDto>(job)),
        error => error.ToActionResult(_correlation.CorrelationId));
}
```

### Step 6: Tests

Create tests in the appropriate test project:

```text
tests/
├── GodForge.Domain.Tests/
├── GodForge.Application.Tests/
├── GodForge.Infrastructure.Tests/
├── GodForge.Api.Tests/
└── GodForge.Worker.Tests/
```

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

## 4. Error Handling Standard

- Use stable SRS/project error codes such as `PROJECT_NAME_EXISTS`, `FORBIDDEN`, `REPOSITORY_LOCKED`, `JOB_ALREADY_RUNNING`, or module-specific equivalents.
- Map validation errors to 400.
- Map unauthorized/forbidden to 401/403.
- Map missing resources to 404 without leaking whether a resource exists outside the actor's scope.
- Map conflicts and invalid state transitions to 409.
- For worker errors, update job state and notification instead of trying to return worker exceptions through the original request.
- Include safe `details` only when useful and non-sensitive.

## 5. Security and RBAC Checklist

- [ ] Backend enforces permission for every read/write endpoint.
- [ ] Queries include project scope or owner scope where required.
- [ ] Permission denied is audited when SRS/security requires it.
- [ ] Sensitive data is neither returned nor logged.
- [ ] Git paths, search filters, and shell/Git arguments are validated and not passed as unsafe raw input.
- [ ] Credentials are stored as encrypted references or secret-manager references only.
- [ ] Frontend checks are treated as UX only, never as security.

## 6. Observability Checklist

- [ ] Correlation id exists at request entry.
- [ ] Correlation id is included in commands, worker messages, logs, job records, activity log, and error responses.
- [ ] Structured logs include service, environment, action, status, errorCode, projectId when available, userId when available, and duration.
- [ ] Metrics are emitted for API latency/errors, queue depth/retry/DLQ, worker duration/failures, Git command duration/failures, and lock wait time where applicable.
- [ ] Logs do not include secrets, tokens, raw credentials, or unsafe repository content.

## 7. Completion Checklist

- [ ] SRS FR, AC, API, database, security, and workflow requirements were checked.
- [ ] Correct path chosen: synchronous API or asynchronous job.
- [ ] Domain changes contain invariants and state transitions.
- [ ] Application handler owns orchestration and uses interfaces.
- [ ] Infrastructure implements adapters without business rule leakage.
- [ ] API controller contains no business logic.
- [ ] EF configuration uses `snake_case`, correct schema, correct indexes, and correct delete behavior.
- [ ] RBAC is enforced server-side.
- [ ] Activity log is recorded for important write operations.
- [ ] Correlation id is propagated and returned in errors.
- [ ] Long-running work returns `202 Accepted` and job id.
- [ ] Worker jobs are idempotent, observable, cancellable, retryable, and DLQ-aware.
- [ ] Unit tests and relevant integration/API/worker tests are added or updated.
- [ ] `dotnet build` passes without warnings introduced by the feature.
- [ ] `dotnet test` passes.
- [ ] Manual API verification is done when the feature changes externally visible behavior.

## 8. Commit Convention

Use concise conventional commits:

```text
feat(projects): add project creation workflow
feat(git): add repository clone job orchestration
feat(health): publish health report after analysis
```

For fixes discovered during implementation, use a separate `fix(scope): ...` commit when practical.