# Scene Explorer

## Mục tiêu

Scene Explorer hiển thị cấu trúc scene Godot dưới dạng cây node tương tác, giúp user hiểu scene mà không cần mở Godot Editor.

## Tác nhân

- Developer
- Reviewer/QA
- Viewer

## Phạm vi

- Danh sách scene đã parse.
- Tree view của node trong scene.
- Node detail panel gồm properties, script, signals, groups.
- Search/filter node theo tên và type.
- Breadcrumb từ root đến node đang chọn.
- Không chỉnh sửa scene.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-09 | Scene Explorer | Hiển thị scene tree và chi tiết node từ metadata đã parse. | Must | Viewer+ |
| BR-45 | Không parse realtime | Scene Explorer đọc Metadata Schema, không parse file theo request UI. | Must | System |
| BR-46 | Scene chưa parse | Scene chưa có metadata hiển thị trạng thái rỗng và nút trigger parse nếu user có quyền. | Must | Viewer+, Developer |

## Luồng xử lý chính

1. User mở danh sách scene của project.
2. Frontend gọi API scenes với pagination/filter.
3. User chọn scene.
4. API trả scene detail và node tree từ `scenes`/`scene_nodes`.
5. User click node để xem properties, script, signals, groups và children count.
6. User search/filter node; UI highlight kết quả và giữ context tree.

## Ngoại lệ / lỗi

| Tình huống | HTTP Status | Error Code | Hành vi |
| --- | --- | --- | --- |
| Project chưa parse | 409/200 empty | `METADATA_NOT_READY` | Hiển thị empty state và action parse nếu có quyền. |
| Scene không tồn tại | 404 | `SCENE_NOT_FOUND` | Không trả path nội bộ. |
| User không có quyền | 403 | `FORBIDDEN` | Không trả metadata. |
| Node quá lớn | 200 | `PARTIAL_NODE_TREE` | Có thể phân trang/lazy load node con nếu cần. |

## Acceptance Criteria

- AC-57: Mở scene viewer hiển thị node tree đúng cấu trúc.
- AC-58: Click node hiển thị properties, script và signals.
- AC-59: Tìm kiếm `Button` highlight node type/name phù hợp.
- AC-60: Scene chưa parse hiển thị thông báo và nút trigger parse cho Developer+.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{id}/scenes` | `viewer+` | pagination, search | Scene list | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{id}/scenes/{sceneId}` | `viewer+` | scene id | Scene detail + summary | `SCENE_NOT_FOUND` |
| GET | `/api/v1/projects/{id}/scenes/{sceneId}/nodes` | `viewer+` | tree/flat, search, type | Node tree/list | `SCENE_NOT_FOUND` |
| POST | `/api/v1/projects/{id}/parse` | `developer+` | options | Parse job | `REPO_NOT_READY` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `scenes` | Scene metadata, path, name, node count, file hash. |
| `scene_nodes` | Node tree, node type, parent path, properties, script path, groups. |
| `scripts` | Script detail khi node gắn `.gd`. |
| `dependencies` | Resource/script reference liên quan scene. |
| `jobs` | Parse job state cho empty/loading state. |

## Ghi chú bảo mật / phân quyền

- Scene path trả về là repository-relative path.
- User không có project membership không được biết scene tồn tại.
- Properties có thể chứa path/resource name nhạy cảm theo project; phải filter theo RBAC.
- Scene Explorer không cung cấp edit operation.
