# 5. Thiết kế API

## 5.1 Tiêu chuẩn API
Hệ thống sử dụng chuẩn RESTful API, triển khai bằng ASP.NET Core .NET 9.
- **Stateless:** API không lưu session, xác thực qua JWT.
- **Base URL:** `/api/v1`
- **Request/Response Format:** `application/json`
- **Authentication:** Bearer Token trong header `Authorization: Bearer {accessToken}`

## 5.2 Convention

### Pagination
```
GET /api/v1/projects?page=1&pageSize=20&sortBy=createdAt&sortOrder=desc
```
Response:
```json
{
  "data": [...],
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 150,
    "totalPages": 8
  }
}
```
- Default `pageSize`: 20. Max: 100.
- Default `sortOrder`: `desc`.

### Filtering
```
GET /api/v1/assets?type=image&search=player
```

### Standard Response — Thành công
**200 OK / 201 Created:**
```json
{
  "data": { ... },
  "meta": { "pagination": ... }
}
```

### Standard Response — Lỗi
**4xx / 5xx:**
```json
{
  "error": {
    "code": "PROJECT_NOT_FOUND",
    "message": "Project không tồn tại hoặc bạn không có quyền truy cập.",
    "correlationId": "abc-123",
    "details": [
      { "field": "name", "message": "Tên project đã tồn tại." }
    ]
  }
}
```

### Async Job Response
**202 Accepted:**
```json
{
  "data": {
    "jobId": "uuid",
    "type": "parse",
    "status": "queued"
  }
}
```

---

## 5.3 API Endpoints

### Auth Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `POST` | `/api/v1/auth/login` | Đăng nhập | ❌ | — |
| `POST` | `/api/v1/auth/refresh` | Refresh token | ❌ | — |
| `POST` | `/api/v1/auth/logout` | Đăng xuất | ✅ | Any |
| `POST` | `/api/v1/auth/setup-password` | Thiết lập password (từ invite link) | ❌ | — |

**`POST /api/v1/auth/login`**
```
Request:  { "email": "string", "password": "string" }
Response: { "data": { "accessToken": "jwt", "refreshToken": "opaque", "expiresIn": 900, "user": {...} } }
Errors:   401 INVALID_CREDENTIALS | 403 ACCOUNT_LOCKED | 403 ACCOUNT_DISABLED
```

**`POST /api/v1/auth/refresh`**
```
Request:  { "refreshToken": "string" }
Response: { "data": { "accessToken": "jwt", "refreshToken": "opaque", "expiresIn": 900 } }
Errors:   401 TOKEN_EXPIRED | 401 TOKEN_REVOKED
```

---

### User Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `GET` | `/api/v1/users` | Danh sách users | ✅ | system_admin |
| `POST` | `/api/v1/users` | Tạo user (invite) | ✅ | system_admin |
| `GET` | `/api/v1/users/{id}` | Chi tiết user | ✅ | system_admin hoặc self |
| `PUT` | `/api/v1/users/{id}` | Cập nhật user | ✅ | system_admin hoặc self |
| `PUT` | `/api/v1/users/{id}/status` | Thay đổi status (lock/disable) | ✅ | system_admin |
| `GET` | `/api/v1/users/me` | Thông tin user hiện tại | ✅ | Any |
| `PUT` | `/api/v1/users/me/settings` | Cập nhật user settings | ✅ | Any |
| `PUT` | `/api/v1/users/me/password` | Đổi password | ✅ | Any |

**`POST /api/v1/users`**
```
Request:  { "email": "string", "displayName": "string", "systemRole": "user|system_admin" }
Response: 201 { "data": { "id": "uuid", "email": "...", "status": "pending_activation" } }
Errors:   409 EMAIL_ALREADY_EXISTS | 400 VALIDATION_ERROR
```

---

### Project Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `GET` | `/api/v1/projects` | Danh sách projects (của user) | ✅ | Any |
| `POST` | `/api/v1/projects` | Tạo project | ✅ | Any |
| `GET` | `/api/v1/projects/{id}` | Chi tiết project | ✅ | project member |
| `PUT` | `/api/v1/projects/{id}` | Cập nhật project | ✅ | project_admin+ |
| `DELETE` | `/api/v1/projects/{id}` | Xóa mềm project | ✅ | project_owner |
| `POST` | `/api/v1/projects/{id}/restore` | Khôi phục project | ✅ | system_admin |
| `GET` | `/api/v1/projects/{id}/dashboard` | Dashboard data | ✅ | project member |
| `GET` | `/api/v1/projects/{id}/settings` | Xem settings | ✅ | project_admin+ |
| `PUT` | `/api/v1/projects/{id}/settings` | Cập nhật settings | ✅ | project_admin+ |

**`POST /api/v1/projects`**
```
Request:  { "name": "string", "description": "string?", "godotVersion": "string", "visibility": "private|internal" }
Response: 201 { "data": { "id": "uuid", "name": "...", "slug": "..." } }
Errors:   409 PROJECT_NAME_EXISTS | 400 VALIDATION_ERROR
```

---

### Project Member Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `GET` | `/api/v1/projects/{id}/members` | Danh sách members | ✅ | project member |
| `POST` | `/api/v1/projects/{id}/members` | Mời member | ✅ | project_admin+ |
| `PUT` | `/api/v1/projects/{id}/members/{userId}` | Thay đổi role | ✅ | project_admin+ |
| `DELETE` | `/api/v1/projects/{id}/members/{userId}` | Xóa member | ✅ | project_admin+ |

**`POST /api/v1/projects/{id}/members`**
```
Request:  { "email": "string", "role": "developer|reviewer|viewer" }
Response: 201 { "data": { "userId": "uuid", "role": "developer", "joinedAt": "..." } }
Errors:   404 USER_NOT_FOUND | 409 ALREADY_MEMBER | 403 FORBIDDEN
```

---

### Repository Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `POST` | `/api/v1/projects/{id}/repository` | Kết nối repository | ✅ | project_admin+ |
| `GET` | `/api/v1/projects/{id}/repository` | Thông tin repository | ✅ | project member |
| `PUT` | `/api/v1/projects/{id}/repository/credential` | Cập nhật credential | ✅ | project_admin+ |
| `DELETE` | `/api/v1/projects/{id}/repository` | Ngắt kết nối | ✅ | project_admin+ |
| `POST` | `/api/v1/projects/{id}/repository/sync` | Fetch/sync remote | ✅ | developer+ |

**`POST /api/v1/projects/{id}/repository`**
```
Request:  { "remoteUrl": "string", "personalAccessToken": "string" }
Response: 202 { "data": { "jobId": "uuid", "type": "clone", "status": "queued" } }
Errors:   409 REPO_ALREADY_CONNECTED | 400 INVALID_REPO_URL
```

---

### Git Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `GET` | `/api/v1/projects/{id}/git/status` | Git status | ✅ | developer+ |
| `POST` | `/api/v1/projects/{id}/git/stage` | Stage files | ✅ | developer+ |
| `POST` | `/api/v1/projects/{id}/git/unstage` | Unstage files | ✅ | developer+ |
| `POST` | `/api/v1/projects/{id}/git/commit` | Commit changes | ✅ | developer+ |
| `POST` | `/api/v1/projects/{id}/git/push` | Push to remote | ✅ | developer+ |
| `POST` | `/api/v1/projects/{id}/git/pull` | Pull from remote | ✅ | developer+ |
| `GET` | `/api/v1/projects/{id}/git/branches` | List branches | ✅ | viewer+ |
| `POST` | `/api/v1/projects/{id}/git/branches` | Create branch | ✅ | developer+ |
| `POST` | `/api/v1/projects/{id}/git/branches/checkout` | Checkout branch | ✅ | developer+ |
| `DELETE` | `/api/v1/projects/{id}/git/branches/{name}` | Delete branch | ✅ | project_admin+ |
| `POST` | `/api/v1/projects/{id}/git/merge` | Merge branches | ✅ | developer+ |
| `GET` | `/api/v1/projects/{id}/git/commits` | Commit history | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/git/commits/{hash}` | Commit detail | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/git/commits/{hash}/diff` | Commit diff | ✅ | viewer+ |

**`POST /api/v1/projects/{id}/git/commit`**
```
Request:  { "message": "string" }
Response: 200 { "data": { "hash": "abc123", "message": "...", "author": "..." } }
Errors:   400 NO_STAGED_FILES | 400 COMMIT_MESSAGE_TOO_SHORT | 423 REPO_LOCKED
```

**`POST /api/v1/projects/{id}/git/push`**
```
Response: 200 { "data": { "pushed": true, "commits": 3 } }
Errors:   423 REPO_LOCKED | 409 NON_FAST_FORWARD (need pull first)
```

---

### Scene & Metadata Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `GET` | `/api/v1/projects/{id}/scenes` | Danh sách scenes | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/scenes/{sceneId}` | Scene detail + node tree | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/scenes/{sceneId}/nodes` | Node list (flat/tree) | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/assets` | Danh sách assets | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/assets/{assetId}` | Asset detail | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/scripts` | Danh sách scripts | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/resources` | Danh sách resources | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/dependencies` | Dependency graph data | ✅ | viewer+ |
| `POST` | `/api/v1/projects/{id}/parse` | Trigger parse job | ✅ | developer+ |
| `POST` | `/api/v1/projects/{id}/analyze` | Trigger analyze job | ✅ | developer+ |

**`POST /api/v1/projects/{id}/parse`**
```
Response: 202 { "data": { "jobId": "uuid", "type": "parse", "status": "queued" } }
Errors:   400 REPO_NOT_CONNECTED | 400 REPO_NOT_READY
```

---

### Diff Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `POST` | `/api/v1/projects/{id}/diff/scene` | Scene-aware diff | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/diff/{diffId}` | Lấy diff result | ✅ | viewer+ |

**`POST /api/v1/projects/{id}/diff/scene`**
```
Request:  { "scenePath": "string", "commitA": "string", "commitB": "string" }
Response: 202 { "data": { "jobId": "uuid", "type": "diff", "status": "queued" } }
          hoặc 200 nếu đã cache: { "data": { "diffId": "uuid", "nodes": {...} } }
```

---

### Health Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `GET` | `/api/v1/projects/{id}/health` | Health report mới nhất | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/health/history` | Lịch sử health reports | ✅ | viewer+ |
| `GET` | `/api/v1/projects/{id}/health/{reportId}/issues` | Issues chi tiết | ✅ | viewer+ |

---

### Job Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `GET` | `/api/v1/projects/{id}/jobs` | Danh sách jobs | ✅ | project member |
| `GET` | `/api/v1/projects/{id}/jobs/{jobId}` | Job detail + status | ✅ | project member |
| `POST` | `/api/v1/projects/{id}/jobs/{jobId}/cancel` | Cancel job | ✅ | developer+ |

---

### Activity Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `GET` | `/api/v1/projects/{id}/activities` | Activity log của project | ✅ | project member |
| `GET` | `/api/v1/activities` | Activity log toàn hệ thống | ✅ | system_admin |

---

### Notification Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `GET` | `/api/v1/notifications` | Danh sách notifications | ✅ | Any |
| `GET` | `/api/v1/notifications/unread-count` | Số notification chưa đọc | ✅ | Any |
| `PUT` | `/api/v1/notifications/{id}/read` | Đánh dấu đã đọc | ✅ | Any |
| `PUT` | `/api/v1/notifications/read-all` | Đánh dấu tất cả đã đọc | ✅ | Any |

---

### Search Module
| Method | Path | Mô tả | Auth | RBAC |
|--------|------|--------|------|------|
| `GET` | `/api/v1/search` | Tìm kiếm tổng hợp | ✅ | Any (RBAC filtered) |

**`GET /api/v1/search?q=Player&type=scene,script&projectId=uuid`**
```
Response: {
  "data": {
    "results": [
      { "type": "scene", "name": "Player.tscn", "path": "scenes/Player.tscn", "projectId": "uuid", "projectName": "MyGame" },
      { "type": "script", "name": "player.gd", "path": "scripts/player.gd", "projectId": "uuid", "projectName": "MyGame" }
    ]
  },
  "meta": { "totalItems": 42, ... }
}
```

---

## 5.4 WebSocket / SignalR Events

Kết nối realtime qua SignalR hub: `/hubs/atlas`

### Client → Server
| Event | Payload | Mô tả |
|-------|---------|-------|
| `JoinProject` | `{ projectId }` | Subscribe updates cho project |
| `LeaveProject` | `{ projectId }` | Unsubscribe |

### Server → Client
| Event | Payload | Mô tả |
|-------|---------|-------|
| `JobProgressUpdate` | `{ jobId, progress, status }` | Cập nhật tiến độ job |
| `JobCompleted` | `{ jobId, type, result }` | Job hoàn thành |
| `JobFailed` | `{ jobId, type, errorCode, message }` | Job thất bại |
| `NotificationReceived` | `{ notification }` | Thông báo mới |
| `RepoStatusChanged` | `{ repositoryId, status }` | Trạng thái repo thay đổi |
