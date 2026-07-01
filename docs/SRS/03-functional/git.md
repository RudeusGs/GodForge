# Functional Requirements: Git Integration

## FR-04: Kết nối repository
**Mức ưu tiên:** Must
**Mô tả:** Kết nối dự án với repository Git qua URL, credential/token.
- Hệ thống clone/fetch metadata ban đầu, tạo job phân tích.
- **Ràng buộc:** Credential không được lưu plaintext.

## FR-05: Giao diện Git UI
**Mức ưu tiên:** Must
**Mô tả:** Cung cấp giao diện cho các thao tác Git cơ bản: status, stage/unstage, commit, push, pull.
- Xử lý xung đột (conflict) hiển thị trạng thái và không tự động ghi đè.

## FR-06: Lịch sử commit
**Mức ưu tiên:** Must
**Mô tả:** Hiển thị commit history theo branch, author, thời gian, message và file thay đổi. Liên kết với scene-aware diff nếu có tệp Godot thay đổi.

## FR-07: Quản lý branch
**Mức ưu tiên:** Should
**Mô tả:** Tạo, đổi branch, xóa branch cục bộ/remote, và merge branch khi có quyền hợp lệ. Cảnh báo khi làm việc trên branch có working tree "bẩn".
