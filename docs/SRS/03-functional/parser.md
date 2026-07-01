# Parser / Metadata

## Mục tiêu

Module Parser trích xuất metadata từ repository Godot để các module Scene Explorer, Asset Explorer, Dependency Graph, Scene Diff và Project Health sử dụng mà không phải parse realtime.

## Tác nhân

- Developer
- Project Admin
- Worker/System

## Phạm vi

- Parse `.tscn` scene file.
- Parse `.tres`/`.res` resource file ở mức metadata cần thiết.
- Parse `.gd` script file ở mức class, extends, preload/load và thống kê cơ bản.
- Nhận diện asset files phổ biến.
- Ghi metadata vào Metadata Schema.
- Hỗ trợ incremental parse dựa trên file hash.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-08 | Scene parser | Worker parse `.tscn`, `.tres`, `.gd` và asset metadata từ repository workspace. | Must | Developer, Worker |
| BR-41 | Isolate file lỗi | Lỗi parse một file không được làm fail toàn bộ job nếu partial mode có thể tiếp tục. | Must | Worker |
| BR-42 | Async parse | Parse job chạy qua RabbitMQ Worker. | Must | System |
| BR-43 | Incremental parse | File không đổi theo hash không cần parse lại. | Should | Worker |
| BR-44 | Metadata tái tạo | Parser output ghi vào Metadata Schema và có thể regenerate từ repository. | Must | Worker |

## Luồng xử lý chính

1. User hoặc hệ thống trigger parse/analyze.
2. API kiểm tra project có repository ready và actor có quyền `developer+`.
3. API tạo job `parse` và publish message vào RabbitMQ.
4. Parser Worker lấy snapshot theo branch/commit để tránh branch head thay đổi giữa job.
5. Worker quét file `.tscn`, `.tres`, `.res`, `.gd` và asset files.
6. Worker tính file hash, bỏ qua file không đổi nếu incremental parse.
7. Worker parse metadata và upsert vào các bảng `scenes`, `scene_nodes`, `assets`, `scripts`, `resources`, `dependencies`.
8. Worker cập nhật progress qua job record và SignalR.
9. Hoàn tất job, gửi notification và có thể trigger analyze nếu setting bật.

## Dữ liệu parser cần trích xuất

| Loại file | Metadata |
| --- | --- |
| `.tscn` | Scene name/path, format version, node count, node tree, script path, groups, signals, properties quan trọng. |
| `.tres`/`.res` | Resource path, resource type, properties chính, external reference. |
| `.gd` | Script path, `class_name`, `extends`, preload/load dependency, line count. |
| Asset | Path, filename, asset type, file size, MIME/dimension nếu có, hash và thumbnail path nếu sinh preview. |

## Ngoại lệ / lỗi

| Tình huống | Job/API | Error Code | Hành vi |
| --- | --- | --- | --- |
| Repository chưa kết nối | 400 | `REPO_NOT_CONNECTED` | Không tạo parse job. |
| Repository chưa ready | 400 | `REPO_NOT_READY` | Yêu cầu clone/sync trước. |
| File `.tscn` corrupt | File warning | `PARSE_INVALID_SCENE` | Skip file, lưu warning, tiếp tục file khác nếu có thể. |
| File quá lớn | File warning | `PARSE_FILE_TOO_LARGE` | Skip file theo giới hạn cấu hình. |
| Encoding không hỗ trợ | File warning | `PARSE_ENCODING_UNSUPPORTED` | Detect/fallback, nếu fail thì skip. |
| DB tạm lỗi | Job retry | `JOB_TRANSIENT_FAILURE` | Retry theo worker policy. |

## Acceptance Criteria

- AC-53: Parse project 100 scenes ghi đủ scenes và node tree vào DB.
- AC-54: Một file `.tscn` corrupt bị skip, các file hợp lệ vẫn parse thành công.
- AC-55: Re-parse project chỉ parse file thay đổi dựa trên hash.
- AC-56: Parse job hiển thị progress realtime.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{id}/parse` | `developer+` | branch/commit/options | `202` job parse | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{id}/jobs/{jobId}` | Project member | job id | Job status/progress | `JOB_NOT_FOUND` |
| GET | `/api/v1/projects/{id}/scenes` | `viewer+` | pagination/filter | Parsed scenes | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{id}/assets` | `viewer+` | pagination/filter | Parsed assets | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{id}/scripts` | `viewer+` | pagination/filter | Parsed scripts | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{id}/resources` | `viewer+` | pagination/filter | Parsed resources | `METADATA_NOT_READY` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `jobs` | Lưu trạng thái parse job, progress, error và correlation id. |
| `repositories` | Cung cấp workspace path, default branch và repository state. |
| `scenes` | Lưu metadata scene `.tscn`. |
| `scene_nodes` | Lưu node tree của scene. |
| `assets` | Lưu metadata asset và thumbnail reference. |
| `scripts` | Lưu metadata `.gd`. |
| `resources` | Lưu metadata `.tres`/`.res`. |
| `dependencies` | Lưu dependency phát hiện trong parse. |

## Ghi chú bảo mật / phân quyền

- Parser chỉ đọc trong workspace đã normalize; không theo symlink/path vượt root.
- File path trả về client là path tương đối trong repository, không phải server path.
- Malicious Godot file phải được xử lý như input không tin cậy: giới hạn kích thước, timeout, memory guard và không execute script.
- Parser không chạy GDScript.
- Job result phải filter theo project permission.
