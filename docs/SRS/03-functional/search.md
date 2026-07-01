# Search

## Mục tiêu

Search giúp user tìm nhanh project, scene, node, asset, script và commit trong phạm vi họ có quyền truy cập.

## Tác nhân

- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Phạm vi

- Full-text search metadata và project fields.
- Filter theo project, type và thời gian.
- Pagination.
- RBAC filtering bắt buộc.
- Không search nội dung source code đầy đủ ngoài metadata đã parse trong phạm vi hiện tại.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-15 | Tìm kiếm | Tìm project, scene, node, asset, script và commit theo quyền. | Should | Viewer+ |
| BR-17 | RBAC search | Search result chỉ chứa dữ liệu thuộc project user có quyền. | Must | System |
| BR-18 | Query sanitization | Query tối đa 200 ký tự và sanitize/parameterize để chống injection. | Must | System |

## Luồng xử lý chính

1. User nhập query và filter.
2. API validate query length, page size và filter.
3. Backend xác định project scope user được xem.
4. Search query chạy trên PostgreSQL full-text/search index hoặc metadata query tối ưu.
5. API trả kết quả phân trang và facets nếu có.

## Ngoại lệ / lỗi

| Tình huống | HTTP Status | Error Code | Hành vi |
| --- | --- | --- | --- |
| Query rỗng hoặc quá dài | 400 | `VALIDATION_ERROR` | Trả field error. |
| Project filter ngoài quyền | 403/empty | `FORBIDDEN` | Không trả dữ liệu project đó. |
| Page size vượt giới hạn | 400 | `VALIDATION_ERROR` | Max page size 100. |
| Search index chưa sẵn sàng | 200 degraded | `SEARCH_INDEX_NOT_READY` | Có thể fallback metadata query hoặc empty state. |

## Acceptance Criteria

- AC-21: Tìm kiếm `Player` trả về scene, node hoặc script chứa `Player` trong phạm vi quyền.
- AC-22: User không có quyền project A thì kết quả không chứa data từ project A.
- AC-23: Kết quả được phân trang đúng.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/search` | Authenticated | q, type, projectId, page, pageSize | Search results, pagination | `VALIDATION_ERROR` |
| GET | `/api/v1/projects/{id}/scenes` | `viewer+` | search/filter | Scene results | `FORBIDDEN` |
| GET | `/api/v1/projects/{id}/assets` | `viewer+` | search/filter | Asset results | `FORBIDDEN` |
| GET | `/api/v1/projects/{id}/git/commits` | `viewer+` | author/date/search | Commit results | `REPO_NOT_READY` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `projects` | Project name/slug search. |
| `project_members` | RBAC scope. |
| `scenes`, `scene_nodes` | Scene/node search. |
| `assets`, `scripts`, `resources` | Metadata search. |
| `repositories` | Commit history scope và repo state. |

## Ghi chú bảo mật / phân quyền

- Query phải dùng parameterized SQL hoặc ORM expression an toàn.
- Search result phải filter theo permission trước khi trả client.
- Không trả snippet chứa secret hoặc server path.
