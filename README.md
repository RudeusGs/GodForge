# GodForge

GodForge là nền tảng quản lý mã nguồn, phân tích sức khỏe và trực quan hóa kiến trúc dành riêng cho dự án Godot Engine. Hệ thống phục vụ cá nhân, nhóm phát triển và tổ chức cần quản lý repository Godot, theo dõi chất lượng theo từng commit, bảo vệ tài nguyên nhị phân và cộng tác xử lý các vấn đề kỹ thuật.

GodForge không tự xây lại Git protocol và không đặt mục tiêu sao chép toàn bộ GitHub. Forgejo hoặc nhà cung cấp Git bên ngoài chịu trách nhiệm lưu Git object, refs, clone, push và pull. Giá trị riêng của GodForge nằm ở Godot validation, deterministic parser, dependency graph, health engine, AI advisory, Asset Vault và quy trình vận hành production.

## Phạm vi sản phẩm đã chốt

### Core graduation release

- Identity, session, organization, project, invitation và RBAC nhiều tenant.
- Hosted repository qua Forgejo và linked external repository qua HTTPS adapter.
- Branch, commit, tree, text blob và revision browser.
- Webhook, durable job, outbox/inbox, retry, timeout, cancellation và dead-letter handling.
- Godot Validation Gateway cho `project.godot`, path, symlink, secret, kích thước và loại file.
- Deterministic parser cho `project.godot`, `.tscn`, `.tres`, `.gd` và dependency liên quan.
- Dependency graph, impact analysis, health report và versioned rule engine.
- Gemini AI advisory từ bounded, redacted, structured context; AI không thay đổi health score.
- Asset Vault với quyền public/project/organization/selected/owner độc lập với repository visibility.
- Finding collaboration, dashboard, notification, audit và report export.
- Docker deployment, observability, backup/restore, retention và security testing.

### Không thuộc Core

- Tự xây Git Smart HTTP, SSH server hoặc Git object database.
- Web IDE, CI platform, package registry hoặc merge-conflict editor hoàn chỉnh.
- GitHub-compatible pull-request engine đầy đủ.
- Chạy game, build/export project hoặc thực thi script/plugin Godot không tin cậy.
- AI tự sửa code, push commit hoặc thay đổi quyền người dùng.

## Kiến trúc mục tiêu

```text
Vue 3 + TypeScript
        |
ASP.NET Core API (.NET 10 LTS)
        |
        +-- PostgreSQL: business, metadata, analysis and job state
        +-- Redis: cache, rate limit and distributed locks
        +-- RabbitMQ: durable asynchronous transport
        +-- MinIO: artifacts, reports and protected asset bytes
        +-- Forgejo: hosted Git repositories and Git authentication
        +-- Gemini: optional advisory analysis
        |
GodForge Worker (.NET 10 LTS)
        |
        +-- isolated checkout workspace
        +-- validation, parser, graph, health and AI stages
```

## Bất biến bắt buộc

1. Forgejo hoặc Git provider là nguồn sự thật của Git objects và refs.
2. PostgreSQL là nguồn sự thật của business state, authorization, job và analysis metadata.
3. Redis không phải durable business state; RabbitMQ không phải job database.
4. Parser và rule engine là authoritative; Gemini chỉ là advisory có nhãn rõ ràng.
5. API không clone repository, parse source, tạo report lớn hoặc gọi Gemini trong HTTP request.
6. Mọi analysis gắn với immutable commit SHA và version identity.
7. Mọi project member phải là active organization member của cùng organization.
8. Effective permission là giao của platform minimum, organization policy, project role và resource policy.
9. Protected asset bytes không được giả vờ private nếu đã tồn tại trong public Git history.
10. Không thực thi repository code, Godot script, plugin, native extension hoặc build pipeline trên worker.

## Runtime

Backend hiện target **.NET 10 LTS**. SDK được ghim tại `10.0.302`; ASP.NET Core/EF Core dùng `10.0.10` và Npgsql EF provider dùng `10.0.3`. Restore/build và bộ test hiện tại đã được kiểm chứng bằng .NET 10 SDK container. Việc hoàn tất runtime migration không đồng nghĩa các exit gate M1–M4 đã đạt.

## Thứ tự triển khai

1. **M0 - Foundation stabilization:** .NET 10 migration, CI, configuration, migration policy và test baseline.
2. **M1 - Identity and tenancy:** authentication, sessions, organization, project, membership, invitation, RBAC và audit.
3. **M2 - Repository foundation:** Forgejo identity/provisioning, linked Git, permission synchronization và webhook.
4. **M3 - Durable worker:** job state, outbox/inbox, retry, locks, cancellation và cleanup.
5. **M4-M7 - Godot intelligence:** validation, parser, graph, health, incremental analysis và AI advisory.
6. **M8-M10 - Product completion:** Asset Vault, collaboration, reports, observability, backup và production hardening.

## Development workflow

Trước khi sửa code, đọc `AGENTS.md`, `.agents/AGENTS.md` và `docs/DEFINITION_OF_READY.md`. Feature chưa có requirement ID, acceptance ID, API contract, data design, RBAC, tests và observability thì chưa được phép implement.

Development commands hiện tại phụ thuộc trạng thái code trong repository. Sau khi M0 hoàn tất:

```bash
cp .env.example .env
docker compose up -d

cd GodForge-BE
dotnet restore
dotnet test

dotnet run --project src/GodForge.Api
dotnet run --project src/GodForge.Worker
```

Database migration phải chạy bằng bước release hoặc lệnh migration có kiểm soát. Production không tự động migrate từ nhiều API instance khi khởi động.

Frontend:

```bash
cd GodForge-FE
npm ci
npm run dev
```

## Tài liệu nền

- `docs/PRODUCT_VISION.md`: tầm nhìn sản phẩm.
- `docs/SRS/01-scope.md`: Core, Advanced, Extension và exclusions.
- `docs/SRS/02-architecture.md`: boundary và system topology.
- `docs/SRS/03-functional/`: yêu cầu theo module.
- `docs/SRS/04-database.md`: logical data model.
- `docs/SRS/04-database-m1-physical.md`: physical design bắt buộc cho M1.
- `docs/SRS/05-api.md`: API catalog.
- `docs/SRS/05-api-contracts/`: API contracts chi tiết cho M1.
- `docs/RBAC_MATRIX.md`: organization/project roles và effective-permission rules.
- `docs/SRS/10-traceability.md`: requirement-to-API/data/test mapping.
- `docs/MILESTONES.md`: thứ tự phát triển.
- `docs/IMPLEMENTATION_STATUS.md`: trạng thái code thực tế; không phải target design.
