# 1. Giới thiệu (Introduction)

## 1.1 Mục đích tài liệu
Tài liệu này mô tả đầy đủ các yêu cầu phần mềm cho hệ thống **Atlas (Godot Project Intelligence Platform)**. Mục tiêu là cung cấp cơ sở thống nhất để đội sản phẩm, kỹ thuật, kiểm thử, vận hành và các bên liên quan hiểu rõ phạm vi, chức năng, ràng buộc và tiêu chí nghiệm thu của hệ thống.

## 1.2 Bối cảnh
Godot là game engine mã nguồn mở. Khi quy mô dự án Godot tăng lên, nhóm phát triển cần một công cụ quản lý cấp dự án để theo dõi cấu trúc, thay đổi Git, dependency, chất lượng và lịch sử hoạt động. Atlas là một ứng dụng web tập trung cung cấp giải pháp cho vấn đề này.

## 1.3 Mục tiêu sản phẩm
- Cổng web tập trung quản lý nhiều dự án Godot và repository.
- Tích hợp giao diện Git UI cơ bản và lịch sử commit.
- Phân tích metadata (.tscn, .tres, .gd), xây dựng Scene Explorer, Asset Explorer, Dependency Graph.
- Cung cấp Scene-aware diff hỗ trợ review thay đổi theo cấu trúc thay vì text.
- Đánh giá sức khỏe dự án (Project Health) phát hiện lỗi tham chiếu, vòng phụ thuộc.

## 1.4 Phạm vi hệ thống
**Bao gồm:**
- Xác thực và phân quyền (RBAC).
- Quản lý dự án, thành viên.
- Thao tác Git (clone, fetch, commit, push, pull, branch, merge).
- Scene & Asset Explorer, Dependency Graph, Project Health.
- Scene Diff Viewer, Dashboard, Notification, Activity Log.

**Loại trừ:**
- KHÔNG cung cấp code editor hay scene editor.
- KHÔNG chạy game hoặc export build.
- KHÔNG tự động sửa code/refactor.
- KHÔNG thay thế IDE hoặc Godot Editor.

## 1.5 Đối tượng người dùng chính
- **System Admin**: Quản trị toàn hệ thống.
- **Organization Owner / Project Admin**: Quản lý dự án, thành viên, cài đặt.
- **Developer**: Dev Godot thao tác Git, xem scene/asset, diff, dependency.
- **Reviewer / QA**: Review diff, health report.
- **Viewer**: Xem readonly.
