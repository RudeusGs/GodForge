# 6. Security

## 6.1 Mục tiêu bảo mật

GodForge quản lý repository, metadata dự án, credential Git và lịch sử hoạt động nên phải coi dữ liệu dự án là nhạy cảm. Mục tiêu bảo mật chính:

- Xác thực user bằng JWT access token và refresh token rotation.
- Phân quyền bằng RBAC ở system-level và project-level.
- Không lưu credential, password hoặc token plaintext.
- Không expose internal path, credential, stack trace hoặc raw command output nhạy cảm.
- Ghi audit/activity log cho thao tác quan trọng với correlation id.
- Validate mọi input từ user, repository URL, file path và worker message.

## 6.2 Authentication

| Thành phần | Quy định |
| --- | --- |
| Access token | JWT, lifetime 15 phút. |
| Refresh token | Opaque random token, lifetime 7 ngày, lưu hash SHA-256 trong DB. |
| Rotation | Mỗi lần refresh phát refresh token mới và revoke token cũ. |
| JWT issuer | `godforge-api` hoặc cấu hình theo môi trường. |
| JWT audience | `godforge-client` hoặc cấu hình theo môi trường. |
| Clock skew | Tối đa 30 giây. |

JWT payload tối thiểu:

```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "name": "Display Name",
  "role": "system_admin",
  "iat": 1719792000,
  "exp": 1719792900,
  "iss": "godforge-api",
  "aud": "godforge-client",
  "jti": "unique-token-id"
}
```

## 6.3 Password hashing

| Quy định | Giá trị |
| --- | --- |
| Algorithm | bcrypt |
| Cost factor | Tối thiểu 12 |
| Minimum length | 8 ký tự |
| Require uppercase | Có |
| Require lowercase | Có |
| Require digit | Có |
| Require special character | Không bắt buộc trong MVP |
| Max length | 128 ký tự |

Không dùng MD5, SHA-1, SHA-256 đơn thuần hoặc custom hash cho password.

## 6.4 Authorization / RBAC

### System-level roles

| Action | system_admin | user |
| --- | :---: | :---: |
| Manage users | Có | Không |
| View all projects for operations | Có | Không |
| Restore deleted projects | Có | Không |
| System configuration | Có | Không |
| Create project | Có | Có |

### Project-level roles

| Action | project_owner | project_admin | developer | reviewer | viewer |
| --- | :---: | :---: | :---: | :---: | :---: |
| Delete project | Có | Không | Không | Không | Không |
| Manage members | Có | Có | Không | Không | Không |
| Configure repository | Có | Có | Không | Không | Không |
| Update project settings | Có | Có | Không | Không | Không |
| Git operations | Có | Có | Có | Không | Không |
| Trigger parse/analyze | Có | Có | Có | Không | Không |
| View scenes/assets | Có | Có | Có | Có | Có |
| View dependency/diff/health | Có | Có | Có | Có | Có |
| View activity | Có | Có | Có | Có | Có |

### Quy tắc phân quyền

- Kiểm tra quyền ở Application/Use Case layer, không chỉ ở Controller hoặc UI.
- Mọi query dữ liệu project phải có project scope.
- System Admin bypass project RBAC cho quản trị nhưng vẫn ghi activity/audit.
- Frontend role-based visibility chỉ là hỗ trợ UX, không phải biện pháp bảo mật.

## 6.5 Git credential encryption

| Nội dung | Quy định |
| --- | --- |
| Git PAT | Mã hóa AES-256-GCM hoặc cơ chế tương đương trước khi lưu. |
| Key | Lưu trong secret manager hoặc environment variable bảo vệ, không hardcode. |
| API response | Không trả plaintext; chỉ trả trạng thái cấu hình hoặc masked value. |
| Logs | Không log PAT, remote URL có embedded credential, token hoặc secret. |
| Decrypt | Chỉ decrypt trong worker/service khi cần Git operation. |

Environment variable đề xuất: `GODFORGE_ENCRYPTION_KEY`.

## 6.6 Secret management

- Không commit secret vào repo.
- Tách config dev/staging/prod.
- Secret production phải được cấp qua secret manager hoặc biến môi trường an toàn.
- Rotate key/credential phải có quy trình rõ ràng.
- Log scrubber phải loại bỏ token, password, PAT và authorization header.

## 6.7 Rate limiting

| Endpoint/Nhóm | Limit | Window |
| --- | --- | --- |
| `/api/v1/auth/login` | 10 requests | 1 phút / IP |
| `/api/v1/auth/refresh` | 20 requests | 1 phút / IP |
| General API | 100 requests | 1 phút / user |
| Search | 30 requests | 1 phút / user |
| Git operations | 10 requests | 1 phút / user |

Brute-force protection: sau 5 lần login sai liên tiếp, account bị khóa 15 phút.

## 6.8 Input validation

| Input | Quy định |
| --- | --- |
| Repository URL | Chỉ HTTPS trong MVP; validate scheme, host, length và deny private/reserved network mặc định, chỉ cho phép override bằng allowlist ở môi trường dev/staging. |
| File path | Normalize path, reject absolute path, `..`, symlink escape và path ngoài workspace. |
| Git arguments | Không tạo shell command từ raw string; dùng structured args. |
| Search query | Max 200 ký tự, parameterized query. |
| Pagination | `pageSize` tối đa 100. |
| Worker message | Validate schema version, job id, project id, correlation id, input hash và attempt count. |
| Godot file | Giới hạn kích thước, timeout parse, không execute script. |

## 6.9 Audit logging

Mọi thao tác quan trọng phải ghi activity/audit log:

- Login success/failure, logout.
- User created, role changed, member added/removed.
- Project created/updated/deleted/restored.
- Repository connected/credential updated/disconnected.
- Git commit/push/pull/merge.
- Job started/completed/failed.
- Permission denied ở các hành động nhạy cảm nếu cấu hình audit bật.

Activity log phải có `correlation_id`, actor, project nếu có, action, target và metadata đã sanitize.

## 6.10 Không expose thông tin nội bộ

API response và UI không được hiển thị:

- Server filesystem path.
- Credential, token, password, invite token.
- Stack trace production.
- Raw Git stderr/stdout chứa secret hoặc path nội bộ.
- Connection string, broker URL, MinIO key.
- Worker internal exception đầy đủ.

## 6.11 Network security

- Public API phải dùng HTTPS/TLS 1.2+.
- PostgreSQL, Redis, RabbitMQ, MinIO và workers nằm trong private container network.
- Không expose DB/Redis/RabbitMQ ra public.
- CORS allowlist theo môi trường.
- Security headers tối thiểu:

```text
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Strict-Transport-Security: max-age=31536000; includeSubDomains
Content-Security-Policy: default-src 'self'
Referrer-Policy: strict-origin-when-cross-origin
```

## 6.12 Threat model tối thiểu

| Threat | Rủi ro | Biện pháp kiểm soát | Test/Verification |
| --- | --- | --- | --- |
| Git credential leak | PAT bị lộ qua DB, API response, log hoặc exception. | Mã hóa credential, mask response, log scrubber, không log Authorization/PAT, secret rotation. | Security test kiểm tra DB không có plaintext PAT và log không chứa token mẫu. |
| Git command injection | User input như branch/path/message bị chèn vào Git command. | Không ghép command string; validate branch/path; truyền argument dạng structured; deny ký tự/path nguy hiểm. | Test branch/path malicious và xác nhận không execute ngoài ý định. |
| SSRF qua repository URL | Clone/fetch tới IP nội bộ hoặc metadata service. | Chỉ HTTPS, validate host, deny private/reserved IP mặc định, allowlist provider nếu cần. | Test URL private IP, localhost, link-local bị reject. |
| Path traversal khi đọc repo | User yêu cầu path `../` hoặc symlink để đọc file ngoài workspace. | Normalize path, reject absolute/parent traversal, kiểm tra resolved path nằm trong workspace, xử lý symlink an toàn. | Test path traversal và symlink escape. |
| Malicious Godot file | File `.tscn`, `.tres`, `.gd` gây crash parser, resource exhaustion hoặc exploit parser. | Không execute script, size limit, timeout, parser isolation, partial failure, retry phân loại. | Fuzz/fixture file corrupt, file lớn và encoding lạ. |
| Permission bypass | User đọc scene/diff/notification/activity của project không thuộc quyền. | Server-side RBAC, project scope mọi query, audit denied, integration tests theo role. | Test cross-project IDOR cho API chính. |
| Token theft | Access/refresh token bị đánh cắp và reuse. | Access token ngắn hạn, refresh rotation, revoke, HTTPS, rate limit, audit login/refresh bất thường. | Test refresh token reuse bị revoke/deny. |

## 6.13 Quyết định MVP

- Repository URL tới private/reserved network bị chặn mặc định ở mọi môi trường; dev/staging chỉ được mở bằng allowlist cấu hình rõ ràng.
- Dev dùng user secrets hoặc environment variables. Production dùng secret manager của hạ tầng triển khai; nếu chưa có secret manager riêng, environment injection của nền tảng deploy là yêu cầu tối thiểu.
