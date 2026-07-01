# Functional Requirements: Authentication & Authorization

## FR-01: Xác thực người dùng
**Mức ưu tiên:** Must
**Mô tả:** Cho phép người dùng đăng nhập, đăng xuất, refresh phiên và bảo vệ API bằng JWT.

**Luồng chính:**
1. Người dùng nhập email/username và mật khẩu.
2. Hệ thống xác thực thông tin, phát hành access token và refresh token.
3. Người dùng gửi kèm token trong request API tiếp theo.
4. Đăng xuất sẽ thu hồi refresh token.

**Xử lý lỗi:** Sai tài khoản/mật khẩu, token hết hạn, account bị khóa.

## FR-02: Quản lý người dùng và vai trò
**Mức ưu tiên:** Must
**Mô tả:** Quản lý hồ sơ người dùng, vai trò hệ thống và vai trò theo dự án dựa trên RBAC.

**Luồng chính:**
1. Admin xem danh sách người dùng.
2. Project Admin mời thành viên vào dự án với vai trò phù hợp.
3. Hệ thống kiểm tra quyền trước mỗi thao tác. Vai trò có thể được thay đổi hoặc thu hồi.

**Xử lý lỗi:** Không tự nâng quyền, không được xóa owner cuối cùng của dự án.
