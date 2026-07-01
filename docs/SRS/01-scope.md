# 1. Phạm vi hệ thống

## Phạm vi bao gồm

GodForge bao gồm các năng lực sau trong phạm vi SRS hiện tại:

| Nhóm phạm vi | Nội dung |
| --- | --- |
| Authentication/RBAC | Đăng nhập, đăng xuất, refresh token, invite user, quản lý role hệ thống và role trong project. |
| Project Management | Tạo, xem, cập nhật, xóa mềm, khôi phục project, quản lý thành viên và project settings. |
| Repository/Git | Kết nối repository HTTPS, clone/fetch, status, stage/unstage, commit, push, pull, branch, merge và commit history. |
| Parser/Metadata | Parse `.tscn`, `.tres`, `.gd`, asset files; lưu scene, node, asset, script, resource và dependency metadata. |
| Analyzer/Health | Xây dependency graph, chạy health rules, tính health score và lưu health report/history. |
| Explorer/Visualization | Scene Explorer, Asset Explorer, Dependency Graph, Scene Diff Viewer và Dashboard. |
| Collaboration/Audit | Notification, Activity Log, job status realtime qua SignalR và correlation id cho truy vết. |
| Operations | Logging, monitoring, backup/restore, deployment, worker processing, retry, DLQ và data retention. |

## Phạm vi loại trừ

GodForge **không** làm các việc sau trong phạm vi hiện tại:

- Không thay thế **Godot Editor**.
- Không cung cấp scene editor hoặc visual editor để sửa trực tiếp scene Godot.
- Không cung cấp code editor, IDE hoặc refactoring tool.
- Không chạy game, debug game hoặc profiler runtime.
- Không export build, ký build hoặc phát hành game.
- Không tự động sửa code, resource hoặc scene khi phát hiện lỗi health.
- Không hỗ trợ Git SSH trong MVP; repository connection dùng HTTPS và token.
- Không triển khai plugin marketplace, AI assistant, desktop client hoặc Godot plugin trong phạm vi bắt buộc.

## Ranh giới hệ thống

| Ranh giới | Quy định |
| --- | --- |
| Repository workspace | Repository được clone vào server-side workspace do backend/worker quản lý. User không truy cập path nội bộ. |
| Metadata | Metadata được sinh từ repository và có thể tái tạo. Core data như user, project, membership, credential reference và activity là nguồn sự thật nghiệp vụ. |
| Git operations | Thao tác ghi Git phải đi qua Git Service/Worker, có distributed lock và audit log. |
| Credentials | Git token được mã hóa, không trả về API response và không xuất hiện trong log. |
| Async processing | Clone, parse, analyze, diff và preview chạy qua RabbitMQ workers khi có khả năng tốn thời gian. |
| External systems | SMTP/email, remote Git provider và object storage là dependency bên ngoài hoặc hạ tầng, không làm thay đổi phạm vi sản phẩm. |

## Vai trò và quyền tổng quan

| Vai trò | Phạm vi quyền |
| --- | --- |
| System Admin | Quản lý user toàn hệ thống, xem/khôi phục project, cấu hình hệ thống và bypass project-level RBAC khi cần vận hành. |
| Organization Owner | Vai trò nghiệp vụ cho chủ nhóm/tổ chức; trong MVP ánh xạ về `project_owner` hoặc `project_admin` theo từng project. |
| Project Admin | Quản lý project, members, repository settings, project settings và các hành động quản trị project. |
| Developer | Thực hiện Git operations, trigger parse/analyze, xem metadata, dependency, health và diff. |
| Reviewer/QA | Xem commit history, scene diff, health report, dependency và activity phục vụ review/QA. |
| Viewer | Xem readonly dashboard, scene, asset, graph, health, notification cá nhân và activity được phép. |

## Giới hạn phiên bản 1.0

- Ưu tiên Godot 4.x.
- Ưu tiên repository HTTPS.
- Quy mô MVP hướng đến nhóm nhỏ hoặc môi trường đồ án: khoảng 50 concurrent users, 100 projects, repository tối đa 2 GB.
- Metadata cũ có thể được xóa theo retention để tiết kiệm lưu trữ.
- Một project có một repository chính trong phạm vi MVP.

## Quyết định MVP

- Chưa triển khai organization entity/API riêng trong MVP.
- Project soft-delete giữ 30 ngày trước khi eligible hard-delete; giá trị này cấu hình được theo môi trường nhưng default là 30 ngày.
