# Notification / Activity Log

## Mục tiêu

Module Notification và Activity Log giúp user nhận thông báo về sự kiện quan trọng, đồng thời ghi lại hoạt động nghiệp vụ để audit và truy vết.

## Tác nhân

- System Admin
- Project Admin
- Developer
- Reviewer/QA
- Viewer
- Worker/System

## Phạm vi

- In-app notification, unread count, mark read và mark all read.
- Notification realtime qua SignalR khi user online.
- Email notification cho invite và critical health nếu cấu hình bật.
- Activity Log append-only cho login, project, repo, Git, job và member events.
- Project activity feed và system activity cho System Admin.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-16 | Thông báo | Gửi và quản lý notification cho user. | Should | User, Worker |
| FR-18 | Activity Log | Ghi nhận hoạt động quan trọng của user và hệ thống. | Must | System |
| BR-19 | Realtime notification | Notification gửi realtime qua SignalR khi user online. | Should | System |
| BR-20 | Read state | User có thể mark read hoặc mark all read. | Should | User |
| BR-21 | Notification retention | Notification retention mặc định 90 ngày. | Should | System |
| BR-22 | Write activity | Mọi thao tác thay đổi dữ liệu phải ghi Activity Log. | Must | System |
| BR-24 | No sensitive log | Activity không chứa password, token hoặc credential. | Must | System |
| BR-26 | Append-only activity | Activity Log append-only, không sửa/xóa bởi user thường. | Must | System |

## Luồng xử lý chính

### Notification

1. Domain event hoặc worker result phát sinh notification candidate.
2. Notification Service xác định recipient theo project membership và user settings.
3. Hệ thống tạo record `notifications`.
4. Nếu user online, SignalR gửi `NotificationReceived`.
5. User mở Notification Center, xem danh sách và mark read/mark all.

### Activity Log

1. Write operation hoặc security-relevant event xảy ra.
2. Application layer tạo activity với actor, project, action, target, metadata an toàn, IP và correlation id.
3. Activity được lưu append-only.
4. Project member xem activity theo project; System Admin xem activity toàn hệ thống.

## Event/action tối thiểu

| Loại | Action/Type | Mô tả |
| --- | --- | --- |
| Auth | `user.login.success`, `user.login.failed`, `user.logout` | Sự kiện đăng nhập/đăng xuất. |
| User | `user.created`, `user.role_changed` | Tạo user và đổi role. |
| Project | `project.created`, `project.updated`, `project.deleted`, `project.restored` | Vòng đời project. |
| Member | `project.member_added`, `project.member_removed`, `member.role_changed` | Quản lý thành viên. |
| Repository | `repo.connected`, `repo.credential_updated`, `repo.disconnected` | Cấu hình repository. |
| Git | `git.commit`, `git.push`, `git.pull`, `git.merge` | Thao tác Git. |
| Job | `job.started`, `job.retrying`, `job.completed`, `job.failed`, `job.cancelled`, `job.timeout`, `job.dead_lettered` | Worker/job lifecycle. |
| Health | `health.critical` | Critical health issue mới. |

## Ngoại lệ / lỗi

| Tình huống | HTTP Status | Error Code | Hành vi |
| --- | --- | --- | --- |
| Notification không thuộc user | 404/403 | `NOTIFICATION_NOT_FOUND` | Không lộ notification. |
| Activity ngoài project quyền | 403 | `FORBIDDEN` | Không trả activity. |
| SignalR offline | 200 | `REALTIME_UNAVAILABLE` | User vẫn thấy notification khi mở app. |
| Metadata chứa secret | N/A | `ACTIVITY_SANITIZATION_REQUIRED` | Sanitization trước khi lưu; không ghi secret. |

## Acceptance Criteria

- AC-24: Job hoàn thành tạo notification realtime nếu user online hoặc hiển thị khi mở app.
- AC-25: Mark read làm notification không còn trong unread badge.
- AC-26: Notification cũ hơn 90 ngày bị xóa bởi background job.
- AC-30: Tạo project ghi activity `project.created`.
- AC-31: Đăng nhập thất bại ghi `user.login.failed` kèm IP/correlation id.
- AC-32: Activity log không chứa password, token hoặc credential.
- AC-33: Xem activity log theo project chỉ thấy log thuộc project đó và có pagination.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/notifications` | Authenticated | unread/page | Notification list | `FORBIDDEN` |
| GET | `/api/v1/notifications/unread-count` | Authenticated | none | Unread count | `FORBIDDEN` |
| PUT | `/api/v1/notifications/{id}/read` | Owner | notification id | Read state updated | `NOTIFICATION_NOT_FOUND` |
| PUT | `/api/v1/notifications/read-all` | Authenticated | none | All read | `FORBIDDEN` |
| GET | `/api/v1/projects/{id}/activities` | Project member | pagination/filter | Project activity | `FORBIDDEN` |
| GET | `/api/v1/activities` | `system_admin` | pagination/filter | System activity | `FORBIDDEN` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `notifications` | Recipient, project, type, title, message, read state và job link. |
| `activities` | Actor, action, target, metadata, IP, correlation id và timestamp. |
| `jobs` | Job events liên kết notification/activity. |
| `project_members` | Xác định recipient và permission khi đọc activity. |
| `user_settings`, `project_settings` | Notification preference. |

## Ghi chú bảo mật / phân quyền

- Activity metadata phải được sanitize trước khi lưu.
- Notification chỉ đọc được bởi recipient.
- Activity project chỉ đọc được bởi project member; system-wide activity chỉ dành cho System Admin.
- Không ghi plaintext credential, token, password, invite token hoặc server path nội bộ.
