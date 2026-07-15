# GodForge

GodForge là nền tảng quản lý, phân tích và trực quan hóa dự án Godot dựa trên Git. Người dùng có thể liên kết repository hiện có hoặc, ở giai đoạn sau, tạo repository được host thông qua Forgejo. Mỗi revision được định danh bằng commit SHA và được xử lý bởi worker để API luôn phản hồi nhanh.

## Giá trị cốt lõi

- Quản lý project, member và quyền truy cập theo project.
- Liên kết repository Git ngoài; hỗ trợ hosted repository qua Forgejo khi bật profile `hosted-git`.
- Đồng bộ branch, commit và file tree theo revision.
- Parse dữ liệu Godot theo cách xác định: `project.godot`, `.tscn`, `.tres`, `.gd` và dependency.
- Tạo health report bằng rule engine trước khi dùng AI.
- Tạo context giới hạn, loại binary và che secrets trước khi gọi Gemini.
- Dùng RabbitMQ worker cho clone/fetch, parse, health, context ingest và AI analysis.

## Nguyên tắc bắt buộc

1. Git là nguồn source code; PostgreSQL là nguồn metadata và trạng thái job.
2. Parser/rule engine là nguồn sự thật; Gemini chỉ đưa ra tư vấn có nhãn AI.
3. API không clone repository, parse file hoặc gọi Gemini trong HTTP request.
4. Không tự viết Git protocol server. Hosted Git sử dụng Forgejo qua adapter.
5. Một commit SHA chỉ tạo một pipeline phân tích tương ứng với cùng cấu hình/input hash.

## Phạm vi MVP

MVP tập trung vào project/member, linked repository, branch/commit/file browser, worker pipeline, Godot parser, health report, dependency graph và Gemini advisory thủ công. Không xây GitHub clone đầy đủ, web IDE, CI platform, merge-conflict editor hoặc pull-request engine hoàn chỉnh.

## Khởi chạy local

```bash
cp .env.example .env
docker compose up -d

cd GodForge-BE
dotnet restore
dotnet tool restore
dotnet ef database update --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
dotnet run --project src/GodForge.Api
```

Mở terminal khác:

```bash
cd GodForge-BE
dotnet run --project src/GodForge.Worker
```

Frontend:

```bash
cd GodForge-FE
npm install
npm run dev
```

Bật Forgejo khi cần hosted repository:

```bash
docker compose --profile hosted-git up -d
```

## Tài liệu chính

- `docs/GODFORGE_FEASIBLE_SYSTEM_BLUEPRINT.md`: kiến trúc và roadmap đầy đủ.
- `docs/BLUEPRINT_MIGRATION_GUIDE.md`: trình tự áp dụng thay đổi.
- `docs/SRS/`: yêu cầu chức năng và phi chức năng.
- `docs/ADR/0005-external-git-engine.md`: quyết định dùng Forgejo.
- `docs/ADR/0006-ai-advisory-boundary.md`: ranh giới parser/rule engine/Gemini.
