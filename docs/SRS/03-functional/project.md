# Functional Requirements: Project Management

## FR-03: CRUD Dự án
**Mức ưu tiên:** Must  
**Mô tả:** Tạo, xem, cập nhật, xóa mềm và khôi phục dự án Godot trong hệ thống.

### Luồng chính — Tạo dự án
1. User nhập tên dự án, mô tả (optional), Godot version, visibility (private/internal).
2. Hệ thống validate: tên unique trong hệ thống, tên 3-50 ký tự, chỉ chứa `[a-zA-Z0-9_-]`.
3. Tạo project, gán người tạo làm `project_owner`.
4. Ghi Activity Log: `project.created`.

### Luồng chính — Cập nhật dự án
1. Project Admin/Owner cập nhật tên, mô tả, Godot version, settings.
2. Validate input, lưu thay đổi.
3. Ghi Activity Log: `project.updated`.

### Luồng chính — Xóa mềm
1. Project Owner chọn xóa dự án.
2. Hệ thống xác nhận (prompt confirmation).
3. Đánh dấu `deleted_at` timestamp (soft-delete).
4. Dự án không còn xuất hiện trong danh sách, nhưng dữ liệu vẫn tồn tại.
5. Ghi Activity Log: `project.deleted`.

### Luồng chính — Khôi phục
1. System Admin xem danh sách dự án đã xóa.
2. Chọn khôi phục → xóa `deleted_at`, dự án trở lại bình thường.

### Business Rules
- **BR-10:** Tên project unique toàn hệ thống (case-insensitive).
- **BR-11:** Soft-delete giữ lại data 30 ngày, sau đó eligible cho hard-delete bởi background job.
- **BR-12:** Visibility mặc định là `private`.
- **BR-13:** Tạo project tự động gán creator làm `project_owner`.

### Acceptance Criteria
- [x] AC-13: Tạo project với tên hợp lệ → project xuất hiện trong danh sách.
- [x] AC-14: Tạo project với tên trùng → 409 Conflict.
- [x] AC-15: Xóa mềm project → không xuất hiện trong list, nhưng data còn trong DB.
- [x] AC-16: Khôi phục project đã xóa → xuất hiện lại.
- [x] AC-17: User không có quyền sửa project → 403.

---

## FR-14: Dashboard
**Mức ưu tiên:** Must  
**Mô tả:** Trang tổng quan hiển thị thống kê, health score, trạng thái repository, job gần đây và hoạt động mới.

### Dữ liệu hiển thị
| Mục | Mô tả | Nguồn |
|-----|-------|-------|
| Project Summary | Tổng số scene, asset, script, resource | Redis Cache |
| Health Score | Điểm sức khỏe dự án (0-100) | Redis Cache (từ health_reports) |
| Repository Status | Branch hiện tại, commit mới nhất, sync status | DB + Git |
| Recent Jobs | 10 job gần nhất (status, type, duration) | DB (jobs table) |
| Activity Feed | 20 hoạt động gần nhất | DB (activities table) |
| Health Issues | Top 5 issues nghiêm trọng nhất | Redis Cache |

### Business Rules
- **BR-14:** Dashboard data ưu tiên lấy từ Redis Cache. Cache TTL: 5 phút.
- **BR-15:** Nếu cache miss → query DB và rebuild cache.
- **BR-16:** Dashboard chỉ hiển thị data của project mà user có quyền truy cập.

### Acceptance Criteria
- [x] AC-18: Mở dashboard → hiển thị đầy đủ 6 mục trên trong P95 < 2 giây.
- [x] AC-19: Cache hit → response < 500ms.
- [x] AC-20: User chỉ thấy data của project được phân quyền.

---

## FR-15: Tìm kiếm (Search)
**Mức ưu tiên:** Should  
**Mô tả:** Tìm kiếm dự án, scene, node, asset, script, commit.

### Chi tiết
- **Searchable fields:** project name, scene name, node name/type, asset path, script path, commit message, author.
- **Search type:** Full-text search (PostgreSQL `tsvector`/`tsquery`).
- **Pagination:** Offset-based, default page size = 20, max = 100.
- **Filtering:** Theo project, loại resource (scene/asset/script), thời gian.

### Business Rules
- **BR-17:** Kết quả tìm kiếm phải tuân thủ RBAC — chỉ trả về data thuộc project user có quyền.
- **BR-18:** Query tối đa 200 ký tự, sanitize input chống SQL injection.

### Acceptance Criteria
- [x] AC-21: Tìm kiếm "Player" → trả về scenes, nodes, scripts có chứa "Player".
- [x] AC-22: User không có quyền project A → kết quả không chứa data từ project A.
- [x] AC-23: Kết quả được phân trang đúng.

---

## FR-16: Thông báo (Notification)
**Mức ưu tiên:** Should  
**Mô tả:** Gửi và quản lý thông báo cho người dùng.

### Loại thông báo
| Type | Trigger | Mức độ |
|------|---------|--------|
| `job.completed` | Worker hoàn thành job (parse/analyze/diff) | Info |
| `job.failed` | Worker thất bại | Warning |
| `health.critical` | Health issue mức Critical mới phát hiện | Warning |
| `member.invited` | Được mời vào project | Info |
| `member.role_changed` | Vai trò bị thay đổi | Info |

### Kênh gửi
- **In-app:** Lưu DB, hiển thị bell icon + dropdown. Realtime qua SignalR.
- **Email:** Cho các event quan trọng (invite, critical health). Có thể tắt trong settings.

### Business Rules
- **BR-19:** Notification được gửi realtime qua SignalR khi user online.
- **BR-20:** User có thể đánh dấu đã đọc (mark as read) hoặc đánh dấu tất cả.
- **BR-21:** Notification retention: 90 ngày.

### Acceptance Criteria
- [x] AC-24: Job hoàn thành → user nhận notification realtime (nếu online) hoặc thấy khi mở app.
- [x] AC-25: Đánh dấu đã đọc → notification không còn badge unread.
- [x] AC-26: Notification cũ hơn 90 ngày bị xóa bởi background job.

---

## FR-17: Cài đặt (Settings)
**Mức ưu tiên:** Should  
**Mô tả:** Cho phép cấu hình dự án và tùy chọn cá nhân.

### Project Settings (Project Admin+)
| Setting | Mô tả | Default |
|---------|-------|---------|
| `godot_version` | Phiên bản Godot Engine | `4.x` |
| `auto_parse_on_push` | Tự động parse khi có push mới | `true` |
| `auto_analyze_on_parse` | Tự động analyze sau khi parse xong | `true` |
| `health_check_schedule` | Lịch chạy health check | `daily` |
| `notification_email_enabled` | Gửi email notification | `true` |

### User Settings (Cá nhân)
| Setting | Mô tả | Default |
|---------|-------|---------|
| `theme` | Giao diện sáng/tối | `dark` |
| `notification_in_app` | Nhận thông báo in-app | `true` |
| `notification_email` | Nhận email thông báo | `true` |
| `language` | Ngôn ngữ giao diện (MVP: chỉ tiếng Anh) | `en` |

### Acceptance Criteria
- [x] AC-27: Project Admin thay đổi setting → setting được lưu và áp dụng.
- [x] AC-28: Developer cố thay đổi project setting → 403.
- [x] AC-29: User thay đổi theme → giao diện cập nhật ngay.

---

## FR-18: Activity Log
**Mức ưu tiên:** Must  
**Mô tả:** Ghi nhận hoạt động quan trọng của người dùng và hệ thống để phục vụ audit.

### Cấu trúc Activity Entry
```json
{
  "id": "uuid",
  "project_id": "uuid | null",
  "user_id": "uuid | null",
  "action": "project.created",
  "target_type": "project",
  "target_id": "uuid",
  "metadata": { "project_name": "MyGame" },
  "ip_address": "1.2.3.4",
  "correlation_id": "abc-123",
  "created_at": "2026-07-01T10:00:00Z"
}
```

### Danh sách Action Types
| Action | Mô tả |
|--------|-------|
| `user.login.success` | Đăng nhập thành công |
| `user.login.failed` | Đăng nhập thất bại |
| `user.logout` | Đăng xuất |
| `user.created` | Tạo account mới |
| `user.role_changed` | Thay đổi vai trò |
| `project.created` | Tạo project |
| `project.updated` | Cập nhật project |
| `project.deleted` | Xóa mềm project |
| `project.restored` | Khôi phục project |
| `project.member_added` | Thêm member |
| `project.member_removed` | Xóa member |
| `repo.connected` | Kết nối repository |
| `repo.disconnected` | Ngắt kết nối |
| `git.push` | Push thành công |
| `git.pull` | Pull thành công |
| `git.merge` | Merge thành công |
| `job.started` | Job bắt đầu |
| `job.completed` | Job hoàn thành |
| `job.failed` | Job thất bại |

### Business Rules
- **BR-22:** Mọi thao tác thay đổi dữ liệu (write) phải ghi Activity Log.
- **BR-23:** Log thất bại cũng phải được ghi, kèm correlation_id.
- **BR-24:** KHÔNG lưu thông tin nhạy cảm (password, token) trong log.
- **BR-25:** Activity Log retention: 1 năm. Sau đó eligible cho archival.
- **BR-26:** Activity Log là append-only, không được sửa hay xóa.

### Acceptance Criteria
- [x] AC-30: Tạo project → activity log được ghi với action `project.created`.
- [x] AC-31: Đăng nhập thất bại → activity log ghi `user.login.failed` kèm IP.
- [x] AC-32: Activity log không chứa password hay token.
- [x] AC-33: Xem activity log theo project → chỉ thấy log thuộc project đó, phân trang.
