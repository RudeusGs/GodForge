# Functional Requirements: Authentication & Authorization

## FR-01: Xác thực người dùng (Authentication)
**Mức ưu tiên:** Must  
**Mô tả:** Cho phép người dùng đăng nhập, đăng xuất, refresh phiên và bảo vệ API bằng JWT.

### Luồng chính — Đăng nhập
1. Người dùng nhập email và mật khẩu.
2. Hệ thống xác thực thông tin:
   - So sánh password hash (bcrypt, cost factor ≥ 12).
   - Kiểm tra trạng thái account (active/locked/disabled).
3. Nếu thành công:
   - Phát hành **access token** (JWT, lifetime: 15 phút).
   - Phát hành **refresh token** (opaque token, lifetime: 7 ngày, lưu DB có hash).
   - Ghi Activity Log: `user.login.success`.
4. Trả về `{ accessToken, refreshToken, expiresIn, user }`.

### Luồng phụ — Refresh Token
1. Client gửi refresh token khi access token hết hạn.
2. Hệ thống kiểm tra refresh token hợp lệ, chưa bị thu hồi, chưa hết hạn.
3. Phát hành access token mới + refresh token mới (rotation).
4. Thu hồi refresh token cũ.

### Luồng phụ — Đăng xuất
1. Client gửi request đăng xuất kèm refresh token.
2. Hệ thống thu hồi refresh token trong DB.
3. Ghi Activity Log: `user.logout`.

### Xử lý lỗi
| Tình huống | HTTP Status | Error Code | Hành vi |
|------------|-------------|------------|---------|
| Sai email/password | 401 | `INVALID_CREDENTIALS` | Tăng `failed_login_count` |
| Account bị khóa | 403 | `ACCOUNT_LOCKED` | Thông báo liên hệ admin |
| Account bị vô hiệu | 403 | `ACCOUNT_DISABLED` | Thông báo liên hệ admin |
| Token hết hạn | 401 | `TOKEN_EXPIRED` | Client tự refresh |
| Token sai chữ ký | 401 | `INVALID_TOKEN` | Từ chối request |
| Refresh token đã bị thu hồi | 401 | `TOKEN_REVOKED` | Yêu cầu đăng nhập lại |

### Business Rules
- **BR-01:** Sau 5 lần đăng nhập sai liên tiếp → khóa account 15 phút.
- **BR-02:** Refresh token rotation bắt buộc — mỗi lần refresh phải phát token mới và thu hồi token cũ.
- **BR-03:** Một user có thể có nhiều phiên đồng thời (multi-device).
- **BR-04:** Đăng xuất chỉ thu hồi refresh token của phiên hiện tại, không ảnh hưởng phiên khác.

### Acceptance Criteria
- [x] AC-01: Đăng nhập đúng email/password → nhận access token + refresh token.
- [x] AC-02: Đăng nhập sai → trả 401, không tiết lộ email có tồn tại hay không.
- [x] AC-03: Access token hết hạn → gọi refresh endpoint để lấy token mới.
- [x] AC-04: Refresh token đã bị thu hồi → trả 401, buộc đăng nhập lại.
- [x] AC-05: Đăng xuất → refresh token bị thu hồi, access token cũ vẫn hoạt động cho đến khi hết hạn.
- [x] AC-06: Sau 5 lần sai password → account bị lock 15 phút.

---

## FR-02: Quản lý người dùng và vai trò (User & Role Management)
**Mức ưu tiên:** Must  
**Mô tả:** Quản lý hồ sơ người dùng, vai trò hệ thống và vai trò theo dự án dựa trên RBAC.

### Luồng chính — Tạo người dùng (Invite-only)
1. System Admin tạo tài khoản mới bằng cách nhập email, display name, system role.
2. Hệ thống kiểm tra email chưa tồn tại.
3. Tạo account với trạng thái `pending_activation`.
4. Gửi email mời kèm link thiết lập mật khẩu (token hết hạn sau 48h).
5. Người dùng click link, thiết lập mật khẩu → account chuyển `active`.

### Luồng chính — Phân quyền dự án
1. Project Admin mời user vào dự án bằng email.
2. Gán vai trò cho user trong dự án (Developer / Reviewer / Viewer).
3. User nhận thông báo và có thể truy cập dự án.

### Hệ thống vai trò (RBAC)

#### System-level Roles
| Role | Quyền hạn |
|------|-----------|
| `system_admin` | Toàn quyền: quản lý users, tất cả projects, cấu hình hệ thống |
| `user` | Tạo project, truy cập project được gán |

#### Project-level Roles
| Role | Quyền hạn |
|------|-----------|
| `project_owner` | Toàn quyền project: CRUD, quản lý member, repo config, xóa project |
| `project_admin` | Quản lý member, repo config, không xóa project |
| `developer` | Git operations, xem scene/asset/diff/health, comment |
| `reviewer` | Xem diff, health report, comment. Không thao tác Git |
| `viewer` | Read-only toàn bộ project |

### Xử lý lỗi
| Tình huống | HTTP Status | Error Code |
|------------|-------------|------------|
| Email đã tồn tại | 409 | `EMAIL_ALREADY_EXISTS` |
| Tự nâng quyền | 403 | `CANNOT_SELF_PROMOTE` |
| Xóa owner cuối cùng | 400 | `LAST_OWNER_CANNOT_BE_REMOVED` |
| Invite token hết hạn | 400 | `INVITE_TOKEN_EXPIRED` |

### Business Rules
- **BR-05:** Không hỗ trợ tự đăng ký (self-registration). Chỉ System Admin tạo account.
- **BR-06:** Một user có thể có vai trò khác nhau ở các project khác nhau.
- **BR-07:** Không thể xóa hoặc hạ quyền owner cuối cùng của project.
- **BR-08:** Password phải ≥ 8 ký tự, chứa chữ hoa, chữ thường, số.
- **BR-09:** System Admin không bị ảnh hưởng bởi project-level RBAC (luôn có quyền truy cập).

### Acceptance Criteria
- [x] AC-07: Admin tạo user → user nhận email mời → thiết lập password → đăng nhập thành công.
- [x] AC-08: Project Admin mời user vào project → user thấy project trong danh sách.
- [x] AC-09: Developer cố truy cập admin-only API → 403 Forbidden.
- [x] AC-10: Cố xóa owner cuối cùng → 400 Bad Request.
- [x] AC-11: User bị remove khỏi project → không còn thấy project.
- [x] AC-12: Password không đạt policy → trả lỗi validation rõ ràng.
