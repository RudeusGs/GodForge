# 12. Worker Processing

## Mục tiêu

Worker Processing định nghĩa queue, job lifecycle, retry, DLQ, timeout, idempotency, progress, cancellation, repository lock và metrics cho các tác vụ async của GodForge.

## Queue types

| Queue | Producer | Consumer | Message chính |
| --- | --- | --- | --- |
| `repository.clone` | Repository Service | Clone/Git Worker | `RepositoryCloneRequested` |
| `repository.sync` | Repository/Git Service | Git Worker | `RepositorySyncRequested` |
| `metadata.parse` | Project Service, Git Worker | Parser Worker | `ParseRequested` |
| `metadata.analyze` | Project Service, Parser Worker | Analyze Worker | `AnalyzeRequested` |
| `diff.scene` | Diff Service | Diff Worker | `DiffRequested` |
| `preview.asset` | Metadata Service | Preview Worker | `PreviewRequested` |
| `notification.dispatch` | Domain services/workers | Notification Worker nếu tách riêng | `NotificationDispatchRequested` |

## Message contract tối thiểu

Mỗi message phải có:

- `schemaVersion`
- `messageId`
- `jobId`
- `correlationId`
- `projectId`
- `repositoryId` nếu áp dụng
- `actorId` nếu do user trigger
- `createdAt`
- `attemptCount`
- `inputHash`
- payload theo job type

## Job lifecycle

Canonical job status values: `queued`, `running`, `retrying`, `completed`, `failed`, `cancelled`, `timeout`, `dead_lettered`.

| State | Ý nghĩa | Transition hợp lệ |
| --- | --- | --- |
| `queued` | Job đã tạo và publish queue. | `running`, `cancelled`, `failed` |
| `running` | Worker đang xử lý. | `completed`, `failed`, `retrying`, `cancelled`, `timeout` |
| `retrying` | Job lỗi tạm thời và chờ retry. | `queued`, `failed`, `dead_lettered` |
| `completed` | Job thành công. | Terminal |
| `failed` | Job lỗi không retry hoặc hết retry. | Terminal hoặc manual retry nếu policy cho phép |
| `cancelled` | Job bị hủy trước hoặc trong xử lý an toàn. | Terminal |
| `timeout` | Job vượt timeout đã cấu hình và worker dừng/rollback an toàn. | Terminal hoặc manual retry nếu policy cho phép |
| `dead_lettered` | Message chuyển DLQ sau poison message, schema lỗi hoặc retry exhaustion cần inspect. | Manual inspect/requeue |

## Retry policy

| Loại lỗi | Retry | Ghi chú |
| --- | --- | --- |
| Network tạm thời khi clone/fetch | Có | Exponential backoff, giới hạn attempt. |
| DB/MinIO/RabbitMQ tạm lỗi | Có | Retry nếu operation idempotent. |
| Credential sai | Không | Yêu cầu user cập nhật credential. |
| Repository URL invalid | Không | Validation error. |
| File Godot syntax lỗi | Không retry ở job-level | Ghi warning ở file-level, tiếp tục nếu partial mode. |
| Payload message sai schema | Không | Chuyển DLQ. |
| Timeout lặp lại | Có giới hạn | Ghi `timeout`; sau giới hạn chuyển `dead_lettered` hoặc `failed` theo queue policy. |

## Dead-letter queue

- Mỗi queue chính phải có DLQ tương ứng.
- DLQ record/message phải giữ `messageId`, `jobId`, `correlationId`, error code, attempt count và payload đã sanitize.
- Khi message vào DLQ, job liên quan phải chuyển `dead_lettered` nếu không còn retry tự động.
- Không chứa plaintext credential.
- Vận hành có thể inspect và requeue thủ công sau khi sửa nguyên nhân.

## Timeout

| Job type | Timeout đề xuất |
| --- | --- |
| Clone repo < 500MB | 5 phút, cấu hình tăng theo repo size. |
| Fetch/sync | 2 phút, tùy remote. |
| Parse | Theo số file; default 10 phút cho project vừa. |
| Analyze | 5 phút cho project vừa. |
| Scene diff | 30 giây đến 2 phút tùy scene size. |
| Preview | Timeout ngắn, 30-60 giây. |

## Idempotency

| Worker | Idempotency key | Quy định |
| --- | --- | --- |
| Clone Worker | repository id + remote URL + branch + input hash | Nếu workspace đã đúng commit/state, không clone lại. |
| Parser Worker | project id + repository id + commit hash + file hash | Upsert metadata; không duplicate scene/node/asset. |
| Analyze Worker | project id + metadata version + rule version | Không ghi report nếu metadata version mới hơn đã active. |
| Diff Worker | project id + scene path + base revision + target revision | Cache hit trả result cũ nếu còn hợp lệ. |
| Preview Worker | metadata version + entity hash | Không tạo duplicate artifact. |

## Progress update

- `jobs.progress` là số 0-100.
- Worker cập nhật progress theo batch, không spam DB quá dày.
- SignalR gửi `JobProgressUpdate` cho project room.
- Job detail endpoint là nguồn sự thật nếu SignalR bị mất kết nối.

## Cancellation

- User `developer+` có thể cancel job nếu job type hỗ trợ.
- Cancel clone/Git operation chỉ được thực hiện nếu có thể dừng an toàn.
- Worker phải kiểm tra cancellation token giữa các batch.
- Job cancel không được để repository lock treo.

## Repository lock

- Lock key: `lock:repo:{repository_id}`.
- TTL mặc định: 5 phút hoặc theo job type.
- Thao tác cần lock: clone/fetch, commit, push, pull, merge, checkout, diff nếu cần checkout workspace mutable.
- Lock owner/correlation id lưu để debug.
- Nếu không acquire được lock, API trả 423 hoặc job retry tùy loại.

## Error handling

- Lỗi async phải lưu vào `jobs.error_code`, `jobs.error_message` đã sanitize.
- Worker log chi tiết kỹ thuật với correlation id, nhưng không log secret.
- User-facing error phải ngắn gọn và có hành động tiếp theo.
- Partial parse được phép nếu lỗi file-level không làm mất tính nhất quán metadata.

## Worker metrics

| Metric | Mô tả |
| --- | --- |
| `job_duration_seconds` | Thời gian xử lý job theo type/status. |
| `job_queue_depth` | Số message chờ theo queue. |
| `job_retry_total` | Số retry theo type/error. |
| `job_dead_letter_total` | Số message vào DLQ. |
| `job_progress_update_total` | Số lần cập nhật progress. |
| `repository_lock_wait_seconds` | Thời gian chờ lock. |
| `repository_lock_timeout_total` | Số lần lock timeout/fail. |
| `worker_active_jobs` | Số job đang chạy theo worker. |

## Deployment model for MVP

- Tài liệu này gọi tên các **logical workers** (Clone Worker, Parser Worker, Analyze Worker, v.v.).
- Trong giai đoạn MVP, tất cả các logical workers này có thể được triển khai (deploy) chung bên trong một shared host project duy nhất là `GodForge.Worker` để đơn giản hóa vận hành.
- Tuy nhiên, mỗi hàng đợi (queue) **vẫn phải map với một consumer/handler độc lập** bên trong codebase ngay cả khi chúng chạy chung một process.
- Kiến trúc nội bộ phải đảm bảo rằng trong tương lai, chúng ta có thể dễ dàng tách từng consumer ra thành các worker services riêng biệt mà không làm thay đổi message contracts hay phải viết lại logic nghiệp vụ.
- Về cấu hình retry: mặc định 3 lần với exponential backoff cho lỗi tạm thời; từng queue có thể override bằng cấu hình.
