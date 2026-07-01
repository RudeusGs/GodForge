---
name: api-endpoint
description: Skill for adding new API endpoints to the GodForge backend. Covers controller creation, request/response DTOs, RBAC authorization, error handling, and API conventions from the SRS specification.
---

# API Endpoint

Skill hướng dẫn thêm API endpoint mới cho GodForge Backend.

## Quy trình

### 1. Tra cứu specification
- Mở `docs/SRS/05-api.md` và tìm endpoint cần implement.
- Xác nhận: HTTP method, path, request body, response body, auth, RBAC permission.
- Xem error codes cần handle.

### 2. Tạo Request/Response DTOs

Đặt trong `GodForge.Application/Features/{Module}/DTOs/` hoặc `GodForge.Api/Contracts/`.

```csharp
// Request DTO (API layer)
public record CreateProjectRequest(
    string Name,
    string? Description,
    string GodotVersion,
    string Visibility);

// Response DTO (Application layer)
public record ProjectDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string GodotVersion,
    string Visibility,
    int? HealthScore,
    DateTimeOffset CreatedAt);
```

### 3. Tạo Controller Action

```csharp
/// <summary>
/// Tạo dự án mới.
/// </summary>
[HttpPost]
[ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
public async Task<IActionResult> Create(
    [FromBody] CreateProjectRequest request,
    CancellationToken cancellationToken)
{
    var userId = User.GetUserId(); // Extension method
    var command = new CreateProjectCommand(
        request.Name,
        request.Description,
        request.GodotVersion,
        request.Visibility,
        userId);

    var result = await _mediator.Send(command, cancellationToken);

    return result.Match(
        success => CreatedAtAction(
            nameof(GetById),
            new { id = success.Id },
            new ApiResponse<ProjectDto>(success)),
        failure => failure.ToProblemDetails());
}
```

### 4. API Response Format

**Thành công:**
```json
{
  "data": { ... },
  "meta": { "page": 1, "pageSize": 20, "totalItems": 100, "totalPages": 5 }
}
```

**Lỗi:**
```json
{
  "error": {
    "code": "PROJECT_NAME_EXISTS",
    "message": "Tên project đã tồn tại.",
    "correlationId": "abc-123",
    "details": []
  }
}
```

### 5. RBAC Authorization

```csharp
// Custom attribute hoặc policy-based authorization
[HttpDelete("{id:guid}")]
[Authorize(Policy = "ProjectOwner")] // Chỉ project_owner mới xóa được
public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
{
    // ...
}
```

Tham khảo RBAC matrix trong `docs/SRS/06-security.md` §6.4.

### 6. Pagination Convention

```csharp
[HttpGet]
public async Task<IActionResult> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? sortBy = "createdAt",
    [FromQuery] string sortOrder = "desc",
    CancellationToken cancellationToken = default)
{
    // pageSize capped at 100
    pageSize = Math.Min(pageSize, 100);
    // ...
}
```

### 7. Async Job Endpoints
Endpoints trigger heavy tasks → trả 202 Accepted + jobId:

```csharp
[HttpPost("{id:guid}/parse")]
public async Task<IActionResult> TriggerParse(Guid id, CancellationToken cancellationToken)
{
    var command = new TriggerParseCommand(id);
    var result = await _mediator.Send(command, cancellationToken);

    return result.Match(
        job => Accepted(new ApiResponse<JobDto>(job)),
        failure => failure.ToProblemDetails());
}
```

## Checklist
- [ ] Endpoint path và method khớp với `05-api.md`.
- [ ] Request validation (FluentValidation).
- [ ] RBAC check đúng role.
- [ ] Response format đúng convention (data + meta).
- [ ] Error codes đúng với SRS.
- [ ] Activity Log được ghi cho write operations.
- [ ] Controller KHÔNG chứa business logic.
