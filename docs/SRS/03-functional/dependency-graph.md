# Dependency Graph

## Mục tiêu

Dependency Graph trực quan hóa quan hệ phụ thuộc giữa scene, script, resource và asset để user hiểu cấu trúc dự án và phát hiện rủi ro như missing/cyclic dependency.

## Tác nhân

- Developer
- Reviewer/QA
- Viewer

## Phạm vi

- Hiển thị nodes và edges từ `dependencies`.
- Filter theo type, depth và root.
- Focus mode cho incoming/outgoing dependencies.
- Highlight cyclic dependency.
- Không chỉnh sửa dependency trực tiếp.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-11 | Dependency Graph | Xây dựng và trực quan hóa graph phụ thuộc giữa resource trong project Godot. | Must | Viewer+ |
| BR-50 | Data source | Graph data đọc từ `dependencies` table. | Must | System |
| BR-51 | Project scope | Graph chỉ hiển thị data thuộc project hiện tại. | Must | System |
| BR-52 | Analyze required | Project chưa analyze hiển thị empty state và action analyze nếu có quyền. | Must | Viewer+, Developer |
| BR-53 | Client layout | Layout graph sử dụng force-directed layout ở client. | Should | Frontend |

## Luồng xử lý chính

1. User mở Dependency Graph.
2. Frontend gọi API dependencies với filter mặc định.
3. API trả nodes/edges đã filter theo project permission.
4. UI render graph, legend và detail panel.
5. User filter theo file type, depth hoặc chọn root.
6. User click node để highlight incoming/outgoing edges.
7. Cyclic dependency được highlight và liên kết với health issue nếu có.

## Loại dependency

| From | To | Relation |
| --- | --- | --- |
| Scene | Scene | `instances` |
| Scene | Script | `attaches` |
| Scene | Resource | `uses` |
| Scene | Asset | `references` |
| Script | Script | `extends`, `preload`, `load` |
| Script | Scene | `preload`, `load` |
| Script | Asset | `preload`, `load` |
| Resource | Asset | `references` |

## Ngoại lệ / lỗi

| Tình huống | HTTP Status | Error Code | Hành vi |
| --- | --- | --- | --- |
| Project chưa analyze | 409/200 empty | `ANALYZE_REQUIRED` | Empty state, CTA analyze cho Developer+. |
| Root node không tồn tại | 404 | `DEPENDENCY_ROOT_NOT_FOUND` | Không trả graph. |
| Filter depth quá lớn | 400 | `VALIDATION_ERROR` | Giới hạn depth để tránh response quá lớn. |
| User không có quyền | 403 | `FORBIDDEN` | Không trả graph. |

## Acceptance Criteria

- AC-65: Mở Dependency Graph hiển thị graph interactive với nodes và edges.
- AC-66: Filter chỉ scene chỉ hiện scene nodes và liên kết phù hợp.
- AC-67: Có cyclic dependency A -> B -> A thì cycle được highlight.
- AC-68: Click node bật focus mode incoming/outgoing.
- AC-69: Project chưa analyze hiển thị thông báo và nút trigger analyze cho Developer+.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{id}/dependencies` | `viewer+` | root, depth, type | Graph nodes/edges | `ANALYZE_REQUIRED` |
| GET | `/api/v1/projects/{id}/health/{reportId}/issues` | `viewer+` | issue type | Cycle/missing issues | `REPORT_NOT_FOUND` |
| POST | `/api/v1/projects/{id}/analyze` | `developer+` | options | Analyze job | `PARSE_REQUIRED` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `dependencies` | Graph edges và relation type. |
| `scenes`, `scripts`, `resources`, `assets` | Node labels, type và metadata. |
| `health_issues` | Cyclic/missing issue link. |
| `jobs` | Analyze job state. |

## Ghi chú bảo mật / phân quyền

- Graph có thể tiết lộ cấu trúc source/asset nên phải enforce project RBAC.
- Query depth và page/size phải giới hạn để tránh DoS.
- API không trả server workspace path.
