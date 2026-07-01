# 6. Bảo mật (Security)

## 6.1 Authentication (Xác thực)
- Sử dụng JWT access token để định danh các phiên đăng nhập.
- Yêu cầu refresh token để duy trì phiên.
- Phải thu hồi refresh token khi đăng xuất.
- Request thiếu token, token hết hạn, sai chữ ký sẽ bị từ chối 401 Unauthorized.

## 6.2 Authorization (Phân quyền - RBAC)
Áp dụng phân quyền cấp Hệ thống và cấp Dự án. Mọi API phải kiểm tra quyền trước khi truy xuất dữ liệu:
- **System Admin**: Toàn quyền.
- **Organization Owner**: Quản lý workspace/tổ chức.
- **Project Admin**: Cấu hình project, repo, phân quyền member.
- **Developer**: Dev, thao tác Git, view diff/health.
- **Reviewer/QA**: View diff, ghi chú.
- **Viewer**: Read-only.

## 6.3 Quản lý Secret
- **KHÔNG** lưu thông tin Git token, password dưới dạng plain text.
- Phải mã hóa hoặc dùng `credential_ref` tham chiếu đến Secret Manager.
- Secret không được phép in ra trong bất kỳ Activity Log hay Technical Log nào.

## 6.4 An toàn dữ liệu Git
- **Repository Lock**: Bắt buộc khóa khi thực hiện Git Pull, Merge, Commit bằng API.
- Cảnh báo người dùng khi thao tác Checkout nhánh nếu có working tree bẩn.
- Lỗi Git phải được chuẩn hóa, không hiển thị trực tiếp các thông tin nhạy cảm của server nội bộ.

## 6.5 Network Security
- API công khai qua HTTPS.
- Database, Redis, RabbitMQ, Worker chạy trong Private Container Network, không expose ra public.
