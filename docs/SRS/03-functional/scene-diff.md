# Scene Diff

## Mục tiêu

Scene Diff so sánh thay đổi scene Godot theo cấu trúc node/property thay vì chỉ text diff, giúp reviewer hiểu thay đổi `.tscn` chính xác hơn.

## Tác nhân

- Developer
- Reviewer/QA
- Viewer

## Phạm vi

- So sánh scene giữa hai commit hoặc hai branch tip.
- Hiển thị added, removed, modified, moved và unchanged nodes.
- Hiển thị property-level diff.
- Cache diff artifact trong MinIO.
- Fallback text diff cho file không phải `.tscn`.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-13 | Scene Diff Viewer | So sánh scene theo cấu trúc giữa hai revision. | Must | Viewer+ |
| BR-58 | Diff cache | Diff được cache bằng key gồm scene path và hai revision. | Should | Worker |
| BR-59 | Cache expiry | Diff cache expire sau 7 ngày hoặc khi re-analyze nếu không còn hợp lệ. | Should | System |
| BR-60 | Scope `.tscn` | Scene-aware diff chỉ áp dụng cho `.tscn`; file khác dùng text diff. | Must | System |
| BR-61 | Async diff | Diff chạy async và trả 202 nếu cần compute. | Must | Worker |

## Luồng xử lý chính

1. User chọn scene path và hai revision: commit hoặc branch.
2. API kiểm tra quyền và validate scene path trong repository.
3. Nếu diff artifact đã cache và còn hợp lệ, API trả kết quả nhanh.
4. Nếu chưa cache, API tạo diff job và trả `202 Accepted`.
5. Diff Worker lấy hai version của file `.tscn`.
6. Worker parse node tree/property của hai version.
7. Worker tạo diff summary, node-level diff và property-level diff.
8. Kết quả được lưu MinIO và trả cho UI khi job hoàn thành.

## Hiển thị diff

| Loại thay đổi | Hiển thị |
| --- | --- |
| Added nodes | Highlight xanh, hiển thị node info mới. |
| Removed nodes | Highlight đỏ, hiển thị node info trước khi xóa. |
| Modified nodes | Highlight vàng, hiển thị property changes. |
| Moved nodes | Hiển thị parent path cũ và mới. |
| Unchanged nodes | Mặc định collapsed hoặc làm mờ. |

## Ngoại lệ / lỗi

| Tình huống | HTTP Status / Job State | Error Code | Hành vi |
| --- | --- | --- | --- |
| Scene path không thuộc project | 400/403 | `PATH_NOT_ALLOWED` | Từ chối request. |
| Revision không tồn tại | 404 | `REVISION_NOT_FOUND` | Không tạo diff. |
| File không phải `.tscn` | 200 | `TEXT_DIFF_FALLBACK` | Trả text diff nếu hỗ trợ. |
| Parse một revision thất bại | Job failed/degraded | `DIFF_PARSE_FAILED` | Trả lỗi rõ ràng hoặc fallback nếu có thể. |
| Cache artifact mất | 202 | `DIFF_CACHE_MISS` | Tạo lại diff job. |

## Acceptance Criteria

- AC-75: So sánh hai commit của cùng scene hiển thị node-level diff.
- AC-76: Node được thêm highlight xanh và hiển thị full info.
- AC-77: Property changed hiển thị old value và new value.
- AC-78: Diff đã cache trả response nhanh dưới 500ms.
- AC-79: File non-tscn fallback text diff.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{id}/diff/scene` | `viewer+` | scenePath, commitA/branchA, commitB/branchB | Diff job hoặc cached result | `REVISION_NOT_FOUND`, `PATH_NOT_ALLOWED` |
| GET | `/api/v1/projects/{id}/diff/{diffId}` | `viewer+` | diff id | Diff result | `DIFF_NOT_FOUND` |
| GET | `/api/v1/projects/{id}/git/commits/{hash}/diff` | `viewer+` | commit hash | File diff summary | `COMMIT_NOT_FOUND` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `jobs` | Diff job lifecycle, progress và error. |
| `repositories` | Repository workspace và default branch. |
| `scenes` | Scene metadata hiện hành, path validation. |
| `activities` | Ghi scene diff request nếu policy yêu cầu audit. |
| MinIO `diff-artifacts` | Lưu diff result/cache. |

## Ghi chú bảo mật / phân quyền

- Scene path phải normalize và nằm trong repository workspace.
- Diff result có thể chứa metadata source; phải kiểm tra quyền project khi đọc artifact.
- Không expose server path hoặc raw Git stderr.
- Diff Worker không execute nội dung scene/script.
