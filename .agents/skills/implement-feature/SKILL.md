---
name: implement-feature
description: Skill for implementing a new backend feature following Clean Architecture, CQRS, and project conventions. Covers creating domain entities, application handlers, infrastructure, API controllers, and tests.
---

# Implement Feature

Skill hướng dẫn implement một tính năng mới cho GodForge Backend theo đúng kiến trúc và convention của dự án.

## Quy trình bắt buộc

### 1. Đọc tài liệu TRƯỚC KHI code
- Mở `docs/SRS/` và đọc file functional requirement liên quan.
- Xác định rõ: FR nào, acceptance criteria nào cần đáp ứng.
- Xem `docs/SRS/04-database.md` cho database schema.
- Xem `docs/SRS/05-api.md` cho API endpoint specs.
- Xem `docs/SRS/06-security.md` cho RBAC requirements.

### 2. Tạo file theo thứ tự dependency

#### Bước 1: Domain Layer (`src/GodForge.Domain/`)
```
GodForge.Domain/
├── Entities/
│   └── {EntityName}.cs          # Entity class với private setters, domain methods
├── ValueObjects/
│   └── {ValueObjectName}.cs     # Immutable value objects
├── Enums/
│   └── {EnumName}.cs            # Domain enums
├── Events/
│   └── {EventName}.cs           # Domain events (nếu cần)
└── Exceptions/
    └── {DomainExceptionName}.cs # Domain-specific exceptions
```

**Entity template:**
```csharp
namespace GodForge.Domain.Entities;

public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private Project() { } // EF Core constructor

    public static Project Create(string name, string? description, Guid createdBy)
    {
        // Validation + creation logic
    }

    public void Update(string name, string? description)
    {
        // Domain method for updates
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

#### Bước 2: Application Layer (`src/GodForge.Application/`)
```
GodForge.Application/
├── Common/
│   ├── Interfaces/
│   │   └── I{Repository/Service}Name.cs   # Repository & service interfaces
│   ├── Models/
│   │   └── Result.cs                      # Result pattern
│   └── Behaviors/
│       └── ValidationBehavior.cs          # MediatR pipeline
└── Features/
    └── {Module}/
        ├── Commands/
        │   └── Create{Entity}/
        │       ├── Create{Entity}Command.cs
        │       ├── Create{Entity}CommandHandler.cs
        │       └── Create{Entity}CommandValidator.cs
        ├── Queries/
        │   └── Get{Entity}ById/
        │       ├── Get{Entity}ByIdQuery.cs
        │       └── Get{Entity}ByIdQueryHandler.cs
        └── DTOs/
            └── {Entity}Dto.cs
```

**Command/Handler template:**
```csharp
namespace GodForge.Application.Features.Projects.Commands.CreateProject;

public record CreateProjectCommand(
    string Name,
    string? Description,
    string GodotVersion,
    string Visibility,
    Guid CreatedBy) : IRequest<Result<ProjectDto>>;

public class CreateProjectCommandHandler
    : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectDto>> Handle(
        CreateProjectCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate business rules
        // 2. Create domain entity
        // 3. Persist via repository
        // 4. Save changes via UnitOfWork
        // 5. Return DTO (never entity)
    }
}
```

#### Bước 3: Infrastructure Layer (`src/GodForge.Infrastructure/`)
```
GodForge.Infrastructure/
├── Persistence/
│   ├── GodForgeDbContext.cs
│   ├── Configurations/
│   │   └── {Entity}Configuration.cs   # EF Core entity config (Fluent API)
│   ├── Repositories/
│   │   └── {Entity}Repository.cs      # Implement IXxxRepository
│   └── Migrations/
├── Services/
│   ├── Redis/
│   ├── RabbitMq/
│   ├── MinIO/
│   └── Git/
└── DependencyInjection.cs              # Service registration
```

#### Bước 4: API Layer (`src/GodForge.Api/`)
```
GodForge.Api/
├── Controllers/
│   └── {Module}Controller.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── CorrelationIdMiddleware.cs
├── Filters/
│   └── RbacAuthorizeAttribute.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

**Controller template:**
```csharp
namespace GodForge.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ISender _mediator;

    public ProjectsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand(/* map from request */);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, new { data = result.Value })
            : result.ToProblemDetails();
    }
}
```

#### Bước 5: Tests (`tests/GodForge.UnitTests/`)
```
GodForge.UnitTests/
└── Features/
    └── {Module}/
        └── Commands/
            └── Create{Entity}CommandHandlerTests.cs
```

### 3. Checklist trước khi hoàn thành
- [ ] Entity có private setters, domain methods để thay đổi state.
- [ ] Command/Query handler không chứa infrastructure code.
- [ ] Controller chỉ map request → command/query → response.
- [ ] Repository interface khai báo trong Application, implement trong Infrastructure.
- [ ] EF Configuration dùng Fluent API, table/column name là snake_case.
- [ ] Activity Log được ghi cho mọi write operation.
- [ ] RBAC check được thực hiện.
- [ ] Error handling trả đúng error code theo SRS.
- [ ] Unit test cover happy path + edge cases.
- [ ] Không có compiler warning.
