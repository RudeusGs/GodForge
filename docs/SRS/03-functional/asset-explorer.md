# Asset Explorer

## Mục tiêu

Asset Explorer giúp user xem asset trong project Godot, kiểm tra metadata, preview cơ bản và usage của asset trong scene/script/resource.

## Tác nhân

- Developer
- Reviewer/QA
- Viewer

## Phạm vi

- Hiển thị asset list dạng grid/list.
- Filter/sort theo type, path, size, usage status.
- Xem asset detail.
- Hiển thị usage/reverse lookup từ dependency graph.
- Hiển thị cảnh báo unused/missing asset.
- Không chỉnh sửa hoặc xóa file asset trong phạm vi hiện tại.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-10 | Asset Explorer | Quản lý và trực quan hóa asset metadata trong project Godot. | Must | Viewer+ |
| BR-47 | Metadata source | Asset data đọc từ Metadata Schema và dependencies table. | Must | System |
| BR-48 | Thumbnail | Thumbnail image được generate bởi worker và lưu MinIO nếu cần. | Should | Worker |
| BR-49 | Unused caveat | Unused detection dựa trên dependency graph và có thể false positive với dynamic load. | Should | Viewer+ |

## Luồng xử lý chính

1. User mở Asset Explorer.
2. Frontend gọi API assets với pagination/filter.
3. API trả asset metadata và usage summary theo project permission.
4. User chọn asset để xem detail: type, size, path, format, dimensions, hash và usage.
5. Nếu asset là image và thumbnail có sẵn, UI hiển thị thumbnail từ object storage.
6. User xem cảnh báo unused/missing nếu health/dependency phát hiện.

## Loại asset hỗ trợ

| Loại | Extension | Hiển thị |
| --- | --- | --- |
| Image | `.png`, `.jpg`, `.jpeg`, `.svg`, `.webp` | Thumbnail preview, dimensions nếu có. |
| Audio | `.wav`, `.ogg`, `.mp3` | File info và audio metadata nếu có. |
| Font | `.ttf`, `.otf` | Font metadata nếu parser hỗ trợ. |
| Shader | `.gdshader` | File info và path. |
| Other | `.import`, `.cfg`, loại khác | File info cơ bản. |

## Ngoại lệ / lỗi

| Tình huống | HTTP Status | Error Code | Hành vi |
| --- | --- | --- | --- |
| Metadata chưa sẵn sàng | 409/200 empty | `METADATA_NOT_READY` | Empty state và action parse/analyze nếu có quyền. |
| Asset không tồn tại | 404 | `ASSET_NOT_FOUND` | Không trả server path. |
| Thumbnail chưa có | 200 | `THUMBNAIL_NOT_READY` | UI hiển thị fallback icon. |
| User không có quyền | 403 | `FORBIDDEN` | Không trả asset metadata. |

## Acceptance Criteria

- AC-61: Mở Asset Explorer hiển thị danh sách asset với thumbnail/fallback.
- AC-62: Xem asset detail hiển thị type, size, path và usage.
- AC-63: Asset không được reference hiển thị cảnh báo `Unused`.
- AC-64: Scene reference file không tồn tại hiển thị cảnh báo `Missing`.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{id}/assets` | `viewer+` | type, search, page | Asset list | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{id}/assets/{assetId}` | `viewer+` | asset id | Asset detail | `ASSET_NOT_FOUND` |
| GET | `/api/v1/projects/{id}/dependencies` | `viewer+` | root/target filter | Usage graph | `METADATA_NOT_READY` |
| POST | `/api/v1/projects/{id}/analyze` | `developer+` | options | Analyze job | `PARSE_REQUIRED` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `assets` | Asset metadata, type, path, size, dimensions, thumbnail path. |
| `dependencies` | Reverse lookup usage của asset. |
| `health_issues` | Missing/unused asset issues. |
| `repositories` | Scope asset theo repository. |
| `jobs` | Preview/analyze job state. |

## Ghi chú bảo mật / phân quyền

- Không expose path nội bộ hoặc MinIO private key.
- Thumbnail URL phải được bảo vệ bằng signed URL ngắn hạn hoặc proxy có kiểm tra quyền nếu bucket private.
- Asset Explorer không đọc file tùy ý ngoài repository workspace.
