# GodForge — Agent Rules

> Tài liệu này quy định các nguyên tắc bắt buộc cho bất kỳ AI coding agent nào làm việc trên codebase GodForge.

---

## 🏛 Kiến trúc & Cấu trúc dự án

### Monorepo Layout
```
GodForge/
├── GodForge-BE/          # Backend — ASP.NET Core .NET 9
│   ├── src/
│   │   ├── GodForge.Api/           # API Controllers, Middleware, DI
│   │   ├── GodForge.Application/   # Use Cases, CQRS Handlers, DTOs, Interfaces
│   │   ├── GodForge.Domain/        # Entities, Value Objects, Domain Events, Enums
│   │   ├── GodForge.Infrastructure/# EF Core, Redis, RabbitMQ, MinIO, Git implementations
│   │   └── GodForge.Worker/        # Background Workers (parse, analyze, diff, git)
│   └── tests/
│       └── GodForge.UnitTests/     # xUnit tests
├── GodForge-FE/          # Frontend — Vue 3 (chưa khởi tạo)
└── docs/                 # Tài liệu SRS
    └── SRS/
```

### Clean Architecture — Dependency Rules (NGHIÊM CẤM VI PHẠM)
```
GodForge.Domain        ← KHÔNG phụ thuộc bất kỳ project nào
GodForge.Application   ← chỉ phụ thuộc GodForge.Domain
GodForge.Infrastructure← phụ thuộc GodForge.Application (và GodForge.Domain gián tiếp)
GodForge.Api           ← phụ thuộc GodForge.Application + GodForge.Infrastructure
GodForge.Worker        ← phụ thuộc GodForge.Application + GodForge.Infrastructure
```
- **TUYỆT ĐỐI KHÔNG** để GodForge.Domain hoặc GodForge.Application reference trực tiếp GodForge.Infrastructure.
- **TUYỆT ĐỐI KHÔNG** import EF Core, Redis, RabbitMQ, hoặc bất kỳ thư viện infrastructure nào trong GodForge.Domain hoặc GodForge.Application.
- Infrastructure concerns phải ẩn sau interface được khai báo trong GodForge.Application.

### Worker Architecture (MVP vs Scale)
- Dự án `src/GodForge.Worker` hiện tại là một **shared worker host** (host chạy chung) dành cho giai đoạn MVP để đơn giản hóa deployment. Tuy nhiên, bên trong nó **PHẢI** được cấu trúc như nhiều logical workers độc lập để có thể tách ra thành các process riêng biệt trong tương lai khi cần scale.
- Hệ thống bao gồm các logical workers (Consumers/Handlers) sau:
  - Repository Clone Worker
  - Repository Sync/Git Worker
  - Metadata Parser Worker
  - Metadata Analyzer Worker
  - Scene Diff Worker
  - Asset Preview Worker
  - Notification Dispatch Worker (Optional cho MVP)
- **BẮT BUỘC:** Mỗi hàng đợi (queue) phải có một Consumer class và Handler/Service abstraction riêng biệt.

---

## 📐 Coding Conventions

### C# / .NET
- **Framework:** .NET 9, C# 13.
- **Nullable reference types:** BẬT (đã cấu hình trong `Directory.Build.props`). Không dùng `#nullable disable`.
- **Warnings as errors:** BẬT. Mọi warning phải được xử lý, không suppress bừa.
- **Naming:**
  - `PascalCase` cho public members, types, methods, properties.
  - `_camelCase` cho private fields (có underscore prefix).
  - `camelCase` cho local variables, parameters.
  - `SCREAMING_SNAKE_CASE` cho constants.
  - Interface bắt đầu bằng `I` (vd: `IProjectRepository`).
- **Usings:** Sắp xếp `System.*` lên đầu (`dotnet_sort_system_directives_first = true`).
- **File-scoped namespaces:** Ưu tiên dùng `namespace GodForge.Domain.Entities;` thay vì block `{}`.
- **Indentation:** 4 spaces, UTF-8, LF line endings.

### CQRS Pattern
- Mỗi use case là 1 Command hoặc 1 Query.
- Command: thay đổi state, trả về `Result` hoặc `Result<T>`.
- Query: chỉ đọc, trả về DTO, KHÔNG trả entity.
- Đặt trong folder `GodForge.Application/Features/{Module}/{Commands|Queries}/`.
- Naming: `CreateProjectCommand`, `CreateProjectCommandHandler`, `GetProjectByIdQuery`, `GetProjectByIdQueryHandler`.

### Error Handling
- KHÔNG throw raw `Exception`. Dùng custom domain exceptions hoặc Result pattern.
- KHÔNG dùng `try-catch` trong Controller. Để Global Exception Middleware xử lý.
- Error codes dùng `SCREAMING_SNAKE_CASE`, prefix theo domain: `AUTH_`, `PROJECT_`, `GIT_`, `JOB_`.
- Mọi error response phải kèm `correlationId`.

### Entity Design
- Mọi entity có `Id` kiểu `Guid` (PK).
- Audit columns: `CreatedAt`, `UpdatedAt` kiểu `DateTimeOffset` (UTC).
- Soft-delete dùng `DeletedAt` (nullable `DateTimeOffset`).
- Entity KHÔNG có public setter cho properties quan trọng. Thay đổi state qua domain methods.

---

## 🗄 Database & Infrastructure

### PostgreSQL
- ORM: Entity Framework Core 9.
- Migrations: Code-first. Tạo bằng `dotnet ef migrations add <Name>`.
- Table/column naming: `snake_case` (cấu hình qua EF convention hoặc `ToSnakeCase()`).
- Mọi bảng phải có `id`, `created_at`, `updated_at`.
- Indexes phải khai báo explicit trong `OnModelCreating` hoặc EntityConfiguration.

### Redis
- Dùng cho: Dashboard cache (TTL 5 phút), Distributed lock (TTL 5 phút), Rate limiting.
- Key pattern: `{purpose}:{entity_id}` (vd: `dashboard:project-uuid`, `lock:repo:repo-uuid`).
- KHÔNG dùng Redis làm primary data store.

### RabbitMQ
- Dùng cho: Async jobs (clone, parse, analyze, diff).
- Mỗi job type có queue riêng.
- Job phải idempotent — chạy lại không gây side-effect lặp.
- Worker phải handle failure gracefully: retry 3 lần, dead-letter queue.

### MinIO
- Dùng cho: Diff artifacts, thumbnails, reports, archives.
- Bucket naming: `diff-artifacts`, `thumbnails`, `reports`, `archives`.

---

## 🔐 Security Rules (KHÔNG ĐƯỢC VI PHẠM)

- **KHÔNG** hardcode secrets, connection strings, API keys trong source code. Dùng `appsettings.json` + environment variables + user secrets.
- **KHÔNG** log password, token, credential trong bất kỳ log nào.
- **KHÔNG** trả stack trace cho client ở production.
- **KHÔNG** truy cập DB trực tiếp từ Controller. Phải đi qua Application layer.
- Git PAT mã hóa bằng AES-256-GCM trước khi lưu DB.
- JWT access token lifetime: 15 phút. Refresh token: 7 ngày.
- Password hash: bcrypt, cost factor ≥ 12.
- Mọi API endpoint phải kiểm tra RBAC trước khi xử lý.

---

## ✅ Testing

- Framework: xUnit + Moq.
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior` (vd: `Login_WithInvalidPassword_Returns401`).
- Unit test cho domain logic + application handlers.
- KHÔNG viết test cho trivial code (getters/setters, auto-generated).
- Test phải isolated — không phụ thuộc external services, database, file system.

---

## 📝 Git & Commit

### Conventional Commits (BẮT BUỘC)
Format: `type(scope): subject`

**Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`

**Scopes:** `auth`, `user`, `project`, `repository`, `git`, `scene`, `asset`, `dependency`, `health`, `diff`, `dashboard`, `notification`, `activity`, `job`, `worker`, `queue`, `cache`, `storage`, `database`, `api`, `config`, `security`, `infra`, `test`

**Rules:**
- Subject không viết hoa chữ đầu.
- Subject không kết thúc bằng dấu chấm.
- Header ≤ 100 ký tự.
- Ví dụ: `feat(auth): add jwt refresh token rotation`

### Branching Strategy
- `main` — stable, production-ready.
- `develop` — integration branch.
- `feature/*` — tính năng mới (vd: `feature/auth-login`).
- `fix/*` — sửa lỗi.
- `release/*` — chuẩn bị release.

---

## 📖 Documentation

- **BẮT BUỘC** đọc tài liệu SRS trong `docs/SRS/` trước khi implement bất kỳ feature nào.
- Các file SRS quan trọng:
  - `00-overview.md / 01-scope.md` — Phạm vi, mục tiêu dự án.
  - `02-architecture.md` — Kiến trúc tổng quan.
  - `03-functional/` — Functional requirements chi tiết theo module: auth, project, git, parser, analyzer, scene/asset explorer, dependency graph, scene diff, dashboard, search, notification/activity.
  - `04-database.md` — Database schema, ERD, column specs.
  - `05-api.md` — API endpoints, request/response format.
  - `06-security.md` — JWT config, RBAC matrix, password policy.
  - `07-non-functional.md` — Performance targets, logging, testing strategy.
  - `08-workflows.md` — Workflow nghiệp vụ end-to-end.
  - `09-ui-ux.md` — Screens, states, navigation, role-based visibility.
  - `10-traceability.md` — Mapping requirement ↔ API ↔ database ↔ UI ↔ test.
  - `11-testing-acceptance.md` — Test strategy, DoD, sign-off criteria.
  - `12-worker-processing.md` — Queue, job lifecycle, retry, DLQ, idempotency.
  - `13-deployment-operations.md` — Local/dev/prod deployment và vận hành.
- Khi thêm feature mới hoặc thay đổi kiến trúc → cập nhật docs tương ứng.
- Giữ nguyên comments và docstrings có sẵn trừ khi chúng sai hoặc đã lỗi thời.

---

## 🚫 Những điều KHÔNG ĐƯỢC LÀM

1. **KHÔNG** tạo God class. Mỗi class có 1 trách nhiệm rõ ràng.
2. **KHÔNG** để business logic trong Controller, Worker, hoặc Infrastructure layer.
3. **KHÔNG** dùng `var` khi type không rõ ràng từ context.
4. **KHÔNG** swallow exception (`catch { }` trống). Phải log hoặc re-throw.
5. **KHÔNG** dùng `Thread.Sleep` hoặc synchronous blocking trong async code.
6. **KHÔNG** commit code có compiler warning.
7. **KHÔNG** sửa file `.editorconfig`, `Directory.Build.props`, `global.json`, `commitlint.config.cjs` khi không được yêu cầu.
8. **KHÔNG** thêm NuGet package mới mà không giải thích lý do.
9. **KHÔNG** trả entity trực tiếp qua API response. Phải map sang DTO.
10. **KHÔNG** tạo circular dependency giữa các project.
11. **KHÔNG** viết logic xử lý nghiệp vụ của worker trực tiếp trong `Program.cs` hay class host chung. Mọi logic phải đặt trong các thư mục `Consumers` và `Handlers` riêng biệt cho từng queue.
