# 0. Tổng quan GodForge

## Mục đích tài liệu

Tài liệu SRS này mô tả yêu cầu phần mềm cho **GodForge**, nền tảng web quản lý và phân tích dự án Godot. Bộ tài liệu trong `docs/SRS` là nguồn tài liệu chính của dự án, thay cho luồng duy trì SRS dạng Word.

Mục tiêu của tài liệu là giúp đội sản phẩm, kỹ thuật, kiểm thử, vận hành và các bên liên quan thống nhất về phạm vi, chức năng, dữ liệu, API, bảo mật, vận hành và tiêu chí nghiệm thu.

## Tuyên bố sản phẩm

GodForge là một nền tảng web giúp nhóm phát triển Godot quản lý nhiều dự án, kết nối repository Git, phân tích metadata của dự án và trực quan hóa các thông tin quan trọng như scene tree, asset usage, dependency graph, scene-aware diff và project health.

GodForge tập trung vào quản lý, phân tích, review và audit dự án. Hệ thống không thay thế Godot Editor, IDE hoặc công cụ build game.

## Giá trị mang lại

| Giá trị | Mô tả |
| --- | --- |
| Quản lý tập trung | Theo dõi nhiều dự án Godot, repository, thành viên, vai trò và trạng thái phân tích trong một giao diện web. |
| Hiểu cấu trúc dự án | Parse `.tscn`, `.tres`, `.gd` và asset để dựng Scene Explorer, Asset Explorer và Dependency Graph. |
| Hỗ trợ review thay đổi | Cung cấp Git UI, commit history và Scene Diff Viewer giúp reviewer hiểu thay đổi theo cấu trúc scene thay vì chỉ đọc text diff. |
| Theo dõi sức khỏe dự án | Phát hiện missing resource, missing script, cyclic dependency, unused asset, oversized scene và các vấn đề metadata khác. |
| Audit và vận hành | Ghi Activity Log, Notification, job status, correlation id và metric phục vụ truy vết, debug và vận hành. |

## Mục tiêu sản phẩm

- Cung cấp web portal để quản lý dự án Godot và repository Git.
- Hỗ trợ xác thực, phân quyền theo vai trò hệ thống và vai trò trong dự án.
- Cung cấp Git UI cho các thao tác phổ biến: status, stage, unstage, commit, push, pull, branch, merge và xem lịch sử commit.
- Parse metadata Godot từ `.tscn`, `.tres`, `.gd` và asset files.
- Xây dựng Scene Explorer, Asset Explorer, Dependency Graph và Project Health.
- Cung cấp Scene Diff Viewer để so sánh thay đổi scene theo node/property.
- Cung cấp Dashboard, Notification và Activity Log để theo dõi trạng thái dự án.
- Xử lý tác vụ nặng bằng async workers, queue và job lifecycle rõ ràng.

## Thành phần người dùng

| Vai trò | Mục tiêu chính |
| --- | --- |
| System Admin | Quản trị người dùng, cấu hình hệ thống, xem toàn cục và khôi phục dự án khi cần. |
| Organization Owner | Vai trò nghiệp vụ đại diện chủ nhóm/tổ chức; trong MVP ánh xạ về `project_owner`/`project_admin` cho từng project, chưa tạo organization entity riêng. |
| Project Admin | Quản lý dự án, thành viên, repository, settings và các thao tác quản trị trong project. |
| Developer | Làm việc với Git UI, xem metadata, chạy parse/analyze, kiểm tra dependency và health. |
| Reviewer/QA | Review commit, scene diff, dependency, health report và activity liên quan. |
| Viewer | Xem dashboard, scene, asset, dependency, health và activity ở chế độ readonly. |

## Tài liệu liên quan

- `01-scope.md`: phạm vi hệ thống, ranh giới và giới hạn phiên bản hiện tại.
- `02-architecture.md`: kiến trúc nhiều lớp, async workers và nguyên tắc thiết kế.
- `03-functional/`: yêu cầu chức năng theo module.
- `04-database.md`: schema PostgreSQL, Redis và MinIO.
- `05-api.md`: REST API `/api/v1`, response format, error format và endpoint theo module.
- `06-security.md`: authentication, authorization, secret management và threat model.
- `07-non-functional.md`: hiệu năng, reliability, scalability, observability và maintainability.
- `08-workflows.md` đến `13-deployment-operations.md`: workflow, UI/UX, traceability, testing, worker và vận hành.

## Quyết định MVP

- Không tạo mô hình Organization riêng trong database/API ở MVP. `Organization Owner` được ghi nhận ở tài liệu vai trò và ánh xạ bằng project-level role.
- Invite/notification trong MVP ưu tiên in-app và setup token; email/SMTP là optional nếu cấu hình hạ tầng sẵn sàng.
