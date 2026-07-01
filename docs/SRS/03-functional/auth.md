# Auth / RBAC / User Management

## Mục tiêu

Module Auth cung cấp xác thực người dùng, quản lý phiên, refresh token rotation, quản lý tài khoản và phân quyền RBAC cho GodForge.

## Tác nhân

- System Admin
- Organization Owner
- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Phạm vi

- Đăng nhập, đăng xuất, refresh session bằng JWT access token và refresh token.
- Quản lý user theo mô hình invite-only.
- Quản lý role hệ thống và project-level role.
- Kiểm tra quyền ở backend/application layer.
- Ghi activity log cho đăng nhập, đăng xuất, tạo user và thay đổi quyền.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-01 | Xác thực người dùng | Cho phép đăng nhập, refresh token, đăng xuất và bảo vệ API bằng JWT. | Must | Tất cả user |
| FR-02 | Quản lý người dùng và vai trò | System Admin tạo user invite-only; Project Admin quản lý member/role trong project. | Must | System Admin, Project Admin |
| BR-01 | Khóa tài khoản | Sau 5 lần đăng nhập sai liên tiếp, account bị khóa 15 phút. | Must | System |
| BR-02 | Refresh token rotation | Mỗi lần refresh phải phát refresh token mới và thu hồi token cũ. | Must | System |
| BR-05 | Invite-only | Không hỗ trợ self-registration trong phạm vi hiện tại. | Must | System Admin |
| BR-07 | Owner cuối cùng | Không thể xóa hoặc hạ quyền owner cuối cùng của project. | Must | Project Admin |

## Luồng xử lý chính

### Đăng nhập

1. User nhập email và password.
2. Backend validate input, kiểm tra rate limit và account status.
3. Hệ thống so sánh password bằng bcrypt hash.
4. Nếu hợp lệ, hệ thống phát JWT access token lifetime 15 phút và refresh token opaque lifetime 7 ngày.
5. Refresh token chỉ lưu dạng hash trong `refresh_tokens`.
6. Hệ thống ghi activity `user.login.success` và trả session payload cho client.

### Refresh session

1. Client gửi refresh token khi access token hết hạn.
2. Backend kiểm tra token hash, expiry và `revoked_at`.
3. Hệ thống phát access token mới và refresh token mới.
4. Refresh token cũ bị revoke để ngăn reuse.

### Đăng xuất

1. Client gửi request logout kèm refresh token của phiên hiện tại.
2. Backend revoke refresh token trong DB.
3. Hệ thống ghi activity `user.logout`.

### Tạo user invite-only

1. System Admin nhập email, display name và system role.
2. Backend kiểm tra email chưa tồn tại.
3. Tạo user `pending_activation`.
4. Gửi invite/setup password token hoặc lưu trạng thái invite để môi trường dev kiểm thử.
5. User thiết lập password hợp lệ; account chuyển `active`.

## Ngoại lệ / lỗi

| Tình huống | HTTP Status | Error Code | Hành vi |
| --- | --- | --- | --- |
| Sai email/password | 401 | `INVALID_CREDENTIALS` | Không tiết lộ email có tồn tại; tăng `failed_login_count`. |
| Account bị khóa | 403 | `ACCOUNT_LOCKED` | Từ chối đăng nhập đến khi hết `locked_until`. |
| Account bị vô hiệu | 403 | `ACCOUNT_DISABLED` | Từ chối đăng nhập. |
| Access token hết hạn | 401 | `TOKEN_EXPIRED` | Client gọi refresh endpoint. |
| Token sai chữ ký | 401 | `INVALID_TOKEN` | Từ chối request. |
| Refresh token bị revoke | 401 | `TOKEN_REVOKED` | Buộc đăng nhập lại. |
| Email đã tồn tại | 409 | `EMAIL_ALREADY_EXISTS` | Không tạo user mới. |
| Tự nâng quyền | 403 | `CANNOT_SELF_PROMOTE` | Từ chối thay đổi role. |
| Xóa owner cuối cùng | 400 | `LAST_OWNER_CANNOT_BE_REMOVED` | Không cập nhật membership. |

## Acceptance Criteria

- AC-01: Đăng nhập đúng email/password trả access token, refresh token, thời hạn và user profile.
- AC-02: Đăng nhập sai trả 401 và không tiết lộ user có tồn tại hay không.
- AC-03: Access token hết hạn có thể refresh bằng refresh token hợp lệ.
- AC-04: Refresh token bị revoke hoặc hết hạn trả 401 và yêu cầu đăng nhập lại.
- AC-05: Logout revoke refresh token của phiên hiện tại.
- AC-06: Sau 5 lần sai password, account bị khóa 15 phút.
- AC-07: System Admin tạo user invite-only và user thiết lập password thành công.
- AC-08: Project Admin mời user vào project và user thấy project trong danh sách.
- AC-09: User thiếu quyền truy cập API admin nhận 403.
- AC-10: Không thể xóa hoặc hạ quyền owner cuối cùng của project.
- AC-11: User bị remove khỏi project không còn thấy project đó.
- AC-12: Password không đạt policy trả validation error rõ ràng.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/auth/login` | Anonymous | `email`, `password` | Access token, refresh token, user | `INVALID_CREDENTIALS`, `ACCOUNT_LOCKED` |
| POST | `/api/v1/auth/refresh` | Refresh token | `refreshToken` | Token pair mới | `TOKEN_EXPIRED`, `TOKEN_REVOKED` |
| POST | `/api/v1/auth/logout` | Authenticated | `refreshToken` | Logout success | `INVALID_TOKEN` |
| POST | `/api/v1/auth/setup-password` | Invite token | token, password | Account activated | `INVITE_TOKEN_EXPIRED`, `VALIDATION_ERROR` |
| GET | `/api/v1/users` | `system_admin` | pagination/filter | User list | `FORBIDDEN` |
| POST | `/api/v1/users` | `system_admin` | email, displayName, systemRole | User invited | `EMAIL_ALREADY_EXISTS` |
| GET | `/api/v1/users/me` | Any authenticated | none | Current user | `INVALID_TOKEN` |
| PUT | `/api/v1/users/me/password` | Any authenticated | oldPassword, newPassword | Password changed | `INVALID_CREDENTIALS`, `VALIDATION_ERROR` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `users` | Lưu danh tính, password hash, system role, status, failed login count và lock state. |
| `refresh_tokens` | Lưu hash refresh token, expiry, revoke state, user agent và IP. |
| `project_members` | Lưu project-level role của user. |
| `activities` | Ghi login/logout/user created/role changed và permission-sensitive actions. |
| `user_settings` | Lưu tùy chọn cá nhân sau khi user hoạt động. |

## Ghi chú bảo mật / phân quyền

- Password hash dùng bcrypt cost factor tối thiểu 12.
- Refresh token lưu hash, không lưu plaintext.
- JWT issuer/audience dùng tên GodForge theo môi trường, không dùng tên sản phẩm cũ.
- RBAC phải kiểm tra ở backend/application layer, không phụ thuộc frontend.
- System Admin có thể bypass project-level RBAC cho mục đích quản trị, nhưng vẫn phải ghi activity/audit.
- Không log password, token, invite token hoặc claim nhạy cảm.
