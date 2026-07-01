# Project / Members / Settings

## Mục tiêu

Module Project quản lý vòng đời dự án Godot trong GodForge, thành viên, vai trò theo project và cấu hình dự án.

## Tác nhân

- System Admin
- Organization Owner
- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Phạm vi

- Tạo, xem, cập nhật, xóa mềm và khôi phục project.
- Tự động gán người tạo làm `project_owner`.
- Quản lý thành viên và project-level role.
- Quản lý project settings và user settings liên quan.
- Ghi Activity Log cho mọi thao tác write.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-03 | CRUD dự án | Tạo, xem, cập nhật, xóa mềm và khôi phục dự án Godot. | Must | System Admin, Project Admin, User |
| FR-03.1 | Quản lý thành viên | Mời, đổi role và xóa member trong project. | Must | Project Admin |
| FR-17 | Cài đặt | Cấu hình project settings và user settings. | Should | Project Admin, User |
| BR-10 | Unique project name | Tên project unique toàn hệ thống, case-insensitive. | Must | System |
| BR-11 | Soft delete | Project bị xóa mềm giữ dữ liệu 30 ngày trước khi eligible hard-delete. | Must | System |
| BR-13 | Creator owner | Người tạo project được gán `project_owner`. | Must | System |

## Luồng xử lý chính

### Tạo project

1. User nhập tên, mô tả, Godot version và visibility.
2. Backend validate tên 3-50 ký tự, unique case-insensitive và visibility hợp lệ.
3. Hệ thống tạo project, slug và project settings mặc định.
4. Người tạo được gán `project_owner`.
5. Hệ thống ghi activity `project.created`.

### Cập nhật project

1. Project Admin/Owner cập nhật tên, mô tả, Godot version hoặc visibility.
2. Backend kiểm tra project chưa bị deleted/archived và actor có quyền.
3. Hệ thống validate input, lưu thay đổi và ghi `project.updated`.

### Quản lý members

1. Project Admin nhập email và role cần mời.
2. Backend kiểm tra user tồn tại hoặc tạo invite theo chính sách.
3. Hệ thống tạo/cập nhật `project_members`.
4. User nhận notification `member.invited` hoặc `member.role_changed`.
5. Hệ thống ghi activity tương ứng.

### Xóa mềm và khôi phục

1. Project Owner xác nhận thao tác xóa project.
2. Backend set `deleted_at`, ẩn project khỏi danh sách thường và ghi `project.deleted`.
3. System Admin có thể khôi phục project đã xóa mềm bằng cách xóa `deleted_at`.

## Ngoại lệ / lỗi

| Tình huống | HTTP Status | Error Code | Hành vi |
| --- | --- | --- | --- |
| Tên project trùng | 409 | `PROJECT_NAME_EXISTS` | Không tạo/cập nhật project. |
| Project không tồn tại hoặc không có quyền | 404/403 | `PROJECT_NOT_FOUND` / `FORBIDDEN` | Không lộ thông tin ngoài quyền. |
| Xóa owner cuối cùng | 400 | `LAST_OWNER_CANNOT_BE_REMOVED` | Không cập nhật member. |
| Role không hợp lệ | 400 | `VALIDATION_ERROR` | Trả field error. |
| Developer cập nhật settings project | 403 | `FORBIDDEN` | Từ chối thao tác. |
| Project đã deleted | 409 | `PROJECT_DELETED` | Yêu cầu restore trước nếu được phép. |

## Acceptance Criteria

- AC-13: Tạo project với tên hợp lệ làm project xuất hiện trong danh sách của creator.
- AC-14: Tạo project với tên trùng trả 409.
- AC-15: Xóa mềm project làm project không xuất hiện trong list thường nhưng dữ liệu còn trong DB.
- AC-16: System Admin khôi phục project đã xóa mềm thành công.
- AC-17: User không có quyền sửa project nhận 403.
- AC-27: Project Admin thay đổi setting và setting được lưu/applied.
- AC-28: Developer cố thay đổi project setting nhận 403.
- AC-29: User thay đổi personal settings và UI dùng setting mới.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects` | Authenticated | pagination/filter | Project list theo quyền | `FORBIDDEN` |
| POST | `/api/v1/projects` | Authenticated | name, description, godotVersion, visibility | Project created | `PROJECT_NAME_EXISTS`, `VALIDATION_ERROR` |
| GET | `/api/v1/projects/{id}` | Project member | project id | Project detail | `PROJECT_NOT_FOUND` |
| PUT | `/api/v1/projects/{id}` | `project_admin+` | project fields | Project updated | `FORBIDDEN`, `VALIDATION_ERROR` |
| DELETE | `/api/v1/projects/{id}` | `project_owner` | confirmation | Project soft-deleted | `FORBIDDEN` |
| POST | `/api/v1/projects/{id}/restore` | `system_admin` | none | Project restored | `PROJECT_NOT_FOUND` |
| GET | `/api/v1/projects/{id}/members` | Project member | pagination | Member list | `FORBIDDEN` |
| POST | `/api/v1/projects/{id}/members` | `project_admin+` | email, role | Member added/invited | `ALREADY_MEMBER`, `USER_NOT_FOUND` |
| PUT | `/api/v1/projects/{id}/members/{userId}` | `project_admin+` | role | Role updated | `LAST_OWNER_CANNOT_BE_REMOVED` |
| DELETE | `/api/v1/projects/{id}/members/{userId}` | `project_admin+` | none | Member removed | `LAST_OWNER_CANNOT_BE_REMOVED` |
| GET | `/api/v1/projects/{id}/settings` | `project_admin+` | none | Project settings | `FORBIDDEN` |
| PUT | `/api/v1/projects/{id}/settings` | `project_admin+` | settings payload | Settings updated | `VALIDATION_ERROR` |
| PUT | `/api/v1/users/me/settings` | Authenticated | user settings | Settings updated | `VALIDATION_ERROR` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `projects` | Lưu project core data, slug, visibility, health score cache, creator và `deleted_at`. |
| `project_members` | Gán user vào project kèm role và invited_by. |
| `project_settings` | Lưu auto parse/analyze, health schedule và notification policy của project. |
| `user_settings` | Lưu theme và notification preference của user. |
| `activities` | Ghi project/member/settings write operations. |
| `notifications` | Gửi thông báo invite, role change hoặc project event quan trọng. |

## Ghi chú bảo mật / phân quyền

- Project list và project detail phải filter theo membership hoặc quyền System Admin.
- Không trả dữ liệu project bị deleted cho user thường.
- Không cho phép self-promote hoặc giảm quyền owner cuối cùng.
- Mọi write operation phải ghi activity log và correlation id.
- Settings liên quan credential/repository phải ẩn secret và kiểm tra quyền `project_admin+`.
