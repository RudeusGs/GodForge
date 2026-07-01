# 6. Bảo mật (Security)

## 6.1 Authentication (Xác thực)

### JWT Configuration
| Parameter | Giá trị | Mô tả |
|-----------|---------|-------|
| Algorithm | `HS256` (MVP), `RS256` (production) | Thuật toán ký |
| Access Token Lifetime | 15 phút | Ngắn để giảm rủi ro |
| Refresh Token Lifetime | 7 ngày | Dài hơn, lưu DB |
| Issuer | `atlas-api` | JWT `iss` claim |
| Audience | `atlas-client` | JWT `aud` claim |
| Clock Skew | 30 giây | Chênh lệch đồng hồ cho phép |

### JWT Payload Claims
```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "name": "Display Name",
  "role": "system_admin",
  "iat": 1719792000,
  "exp": 1719792900,
  "iss": "atlas-api",
  "aud": "atlas-client",
  "jti": "unique-token-id"
}
```

### Refresh Token
- **Format:** Opaque random string (Base64, 64 bytes).
- **Storage:** Lưu SHA-256 hash trong DB (`refresh_tokens` table).
- **Rotation:** Bắt buộc — mỗi lần refresh phải phát token mới, thu hồi token cũ.
- Refresh token gửi qua request body (không dùng cookie cho MVP).

### Quy tắc xác thực
- Request thiếu token → 401 Unauthorized.
- Token hết hạn → 401 với error code `TOKEN_EXPIRED`.
- Token sai chữ ký → 401 với error code `INVALID_TOKEN`.
- Token bị revoke → 401 với error code `TOKEN_REVOKED`.

---

## 6.2 Authorization (Phân quyền - RBAC)

### Permission Matrix

#### System-level
| Action | system_admin | user |
|--------|:---:|:---:|
| Manage all users | ✅ | ❌ |
| View all projects | ✅ | ❌ |
| Restore deleted projects | ✅ | ❌ |
| System configuration | ✅ | ❌ |
| Create project | ✅ | ✅ |

#### Project-level
| Action | project_owner | project_admin | developer | reviewer | viewer |
|--------|:---:|:---:|:---:|:---:|:---:|
| Delete project | ✅ | ❌ | ❌ | ❌ | ❌ |
| Manage members | ✅ | ✅ | ❌ | ❌ | ❌ |
| Configure repo | ✅ | ✅ | ❌ | ❌ | ❌ |
| Update settings | ✅ | ✅ | ❌ | ❌ | ❌ |
| Git operations | ✅ | ✅ | ✅ | ❌ | ❌ |
| Trigger parse/analyze | ✅ | ✅ | ✅ | ❌ | ❌ |
| View scenes/assets | ✅ | ✅ | ✅ | ✅ | ✅ |
| View diff | ✅ | ✅ | ✅ | ✅ | ✅ |
| View health | ✅ | ✅ | ✅ | ✅ | ✅ |
| View commit history | ✅ | ✅ | ✅ | ✅ | ✅ |

### RBAC Implementation
- Kiểm tra quyền ở tầng Application (Use Case / Command Handler), KHÔNG ở Controller.
- System Admin bypass tất cả project-level RBAC check.
- Nếu user không có quyền → 403 Forbidden với error code `FORBIDDEN`.

---

## 6.3 Password Security

### Password Policy
| Rule | Giá trị |
|------|---------|
| Minimum length | 8 ký tự |
| Require uppercase | ✅ (ít nhất 1) |
| Require lowercase | ✅ (ít nhất 1) |
| Require digit | ✅ (ít nhất 1) |
| Require special character | ❌ (không bắt buộc cho MVP) |
| Max length | 128 ký tự |

### Password Hashing
- **Algorithm:** bcrypt.
- **Cost factor:** 12 (≈ 250ms trên server hiện đại).
- **KHÔNG** sử dụng MD5, SHA-1, SHA-256 cho password hashing.
- **KHÔNG** sử dụng custom hash functions.

### Brute-force Protection
- Sau 5 lần login sai liên tiếp → lock account 15 phút.
- `failed_login_count` reset về 0 sau login thành công.
- Rate limit endpoint `/api/v1/auth/login`: 10 requests/phút/IP.

---

## 6.4 Quản lý Secret

### Credential Encryption
- Git PAT mã hóa bằng **AES-256-GCM**.
- Encryption key lưu trong environment variable (`ATLAS_ENCRYPTION_KEY`), KHÔNG hardcode trong source.
- Decrypt chỉ xảy ra tại Worker khi cần thực hiện Git operation.

### Secret Handling Rules
1. **KHÔNG** lưu plaintext token, password, key trong DB.
2. **KHÔNG** log credential, token, password trong bất kỳ log nào (activity log, technical log, console).
3. **KHÔNG** trả credential trong API response (masked hoặc omit).
4. Credential API response hiển thị: `"credential": "***masked***"`.

---

## 6.5 An toàn dữ liệu Git

### Repository Lock
- **Bắt buộc** acquire distributed lock (Redis) trước khi thực hiện Git pull, merge, commit, push.
- Lock key: `lock:repo:{repository_id}`.
- Lock TTL: 5 phút (auto-release nếu crash).
- Nếu không acquire được lock → trả 423 Locked.

### Working Tree Safety
- Cảnh báo user khi checkout branch nếu có uncommitted changes.
- Conflict không tự động resolve — hiển thị cho user quyết định.

### Error Sanitization
- Lỗi Git phải được chuẩn hóa trước khi trả cho client.
- **KHÔNG** hiển thị server path, internal URL, credential trong error message.
- Map Git error → custom error code (e.g., `GIT_AUTH_FAILED`, `GIT_CONFLICT`, `GIT_REPO_LOCKED`).

---

## 6.6 Network Security

### Transport
- API public qua **HTTPS** (TLS 1.2+).
- HTTP request tự động redirect sang HTTPS.

### Internal Network
- Database (PostgreSQL), Redis, RabbitMQ, MinIO, Worker chạy trong **Private Container Network** (Docker bridge network `atlas_network`).
- Không expose port internal services ra public.
- Chỉ API container và MinIO console (dev only) expose port.

### CORS Policy
```
Allowed Origins: [configured per environment]
Allowed Methods: GET, POST, PUT, DELETE, OPTIONS
Allowed Headers: Authorization, Content-Type, X-Correlation-Id
Expose Headers: X-Correlation-Id
Allow Credentials: true
Max Age: 3600
```

### Rate Limiting
| Endpoint | Limit | Window |
|----------|-------|--------|
| `/api/v1/auth/login` | 10 requests | 1 phút / IP |
| `/api/v1/auth/refresh` | 20 requests | 1 phút / IP |
| General API | 100 requests | 1 phút / user |
| Search | 30 requests | 1 phút / user |
| Git operations | 10 requests | 1 phút / user |

### Security Headers
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 0
Strict-Transport-Security: max-age=31536000; includeSubDomains
Content-Security-Policy: default-src 'self'
Referrer-Policy: strict-origin-when-cross-origin
```
