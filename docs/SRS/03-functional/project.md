# Functional Requirements: Project Management

## FR-03: CRUD Dự án
**Mức ưu tiên:** Must
**Mô tả:** Tạo, xem, cập nhật, xóa mềm và khôi phục dự án Godot trong hệ thống.
- Người dùng tạo dự án với tên, mô tả, visibility, settings.
- Xóa dự án là xóa mềm (soft-delete).

## FR-14: Dashboard
**Mức ưu tiên:** Must
**Mô tả:** Trang tổng quan hiển thị thống kê, health score, trạng thái repository, job gần đây và hoạt động mới. Dữ liệu này ưu tiên lấy từ Redis Cache để đạt P95 < 2s.

## FR-15: Tìm kiếm (Search)
**Mức ưu tiên:** Should
**Mô tả:** Tìm kiếm dự án, scene, node, asset, script, commit. Áp dụng phân quyền chặt chẽ trên kết quả tìm kiếm.

## FR-16: Thông báo (Notification)
**Mức ưu tiên:** Should
**Mô tả:** Gửi và quản lý thông báo về job hoàn tất/lỗi, repository thay đổi, review cần chú ý và health issue quan trọng.

## FR-17: Cài đặt (Settings)
**Mức ưu tiên:** Should
**Mô tả:** Cho phép cấu hình dự án, repository, phân tích, thông báo, retention và tùy chọn giao diện.

## FR-18: Activity log
**Mức ưu tiên:** Must
**Mô tả:** Ghi nhận hoạt động quan trọng của người dùng và hệ thống để phục vụ audit. Không lưu thông tin nhạy cảm. Thất bại cũng phải ghi nhận kèm correlation id.
