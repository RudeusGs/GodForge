---
name: worker-job
description: Skill for implementing async background jobs (Workers) for the GodForge system. Covers RabbitMQ consumer patterns, job lifecycle, error handling, progress reporting via SignalR, and idempotency requirements.
---

# Worker Job

Skill hướng dẫn implement background job (Worker) cho các tác vụ nặng: clone, fetch, parse, analyze, diff, preview.

## Kiến trúc Worker

```
API Controller
    ↓ (tạo job record trong DB, publish message)
RabbitMQ Queue
    ↓ (Worker consume)
GodForge.Worker / Background Service
    ↓ (xử lý)
    ├── Cập nhật DB (job status, metadata)
    ├── Gửi progress qua SignalR
    └── Gửi notification khi hoàn thành
```

## Quy trình implement

### 1. Định nghĩa Job Type
Job types theo SRS: `clone`, `fetch`, `parse`, `analyze`, `diff`, `preview`.

### 2. Tạo Job Message

```csharp
namespace GodForge.Application.Features.Jobs.Messages;

public record ParseJobMessage(
    string SchemaVersion,
    Guid MessageId,
    Guid JobId,
    Guid ProjectId,
    Guid RepositoryId,
    Guid? ActorId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    int AttemptCount,
    string InputHash);
```

### 3. Tạo Job Handler

```csharp
namespace GodForge.Worker.Handlers;

public class ParseJobHandler : IJobHandler<ParseJobMessage>
{
    private readonly IJobRepository _jobRepository;
    private readonly ISceneParserService _parser;
    private readonly IProgressReporter _progressReporter;
    private readonly ILogger<ParseJobHandler> _logger;

    public async Task HandleAsync(
        ParseJobMessage message,
        CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(message.JobId, cancellationToken);
        if (job is null) return; // Job deleted, skip

        try
        {
            // 1. Update status → running
            job.MarkAsRunning();
            await _jobRepository.SaveAsync(cancellationToken);

            // 2. Execute work with progress reporting
            var files = await GetFilesToParse(message.RepositoryId, cancellationToken);
            var total = files.Count;

            for (var i = 0; i < total; i++)
            {
                try
                {
                    await _parser.ParseFileAsync(files[i], cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log and continue — 1 file failure does NOT crash the job
                    _logger.LogWarning(ex, "Failed to parse {FilePath}", files[i].Path);
                }

                // Report progress
                var progress = (int)((i + 1.0) / total * 100);
                job.UpdateProgress(progress);
                await _progressReporter.ReportAsync(job.Id, progress, cancellationToken);
            }

            // 3. Mark complete
            job.MarkAsCompleted();
            await _jobRepository.SaveAsync(cancellationToken);

            // 4. Send notification
            await _notificationService.NotifyJobCompleted(job, cancellationToken);
        }
        catch (Exception ex)
        {
            // Mark retrying/failed/dead_lettered according to retry policy
            _logger.LogError(ex, "Job {JobId} failed", message.JobId);
            job.MarkAsFailed(ex.Message, "JOB_PROCESSING_ERROR");
            await _jobRepository.SaveAsync(cancellationToken);
            await _notificationService.NotifyJobFailed(job, cancellationToken);
        }
    }
}
```

### 4. Job Lifecycle (State Machine)

Canonical lifecycle theo `docs/SRS/12-worker-processing.md`:

| State | Ý nghĩa | Transition hợp lệ |
| --- | --- | --- |
| `queued` | Job đã tạo và publish queue. | `running`, `cancelled`, `failed` |
| `running` | Worker đang xử lý. `progress` cập nhật 0-100. | `completed`, `failed`, `retrying`, `cancelled`, `timeout` |
| `retrying` | Job lỗi tạm thời và chờ retry. | `queued`, `failed`, `dead_lettered` |
| `completed` | Hoàn thành thành công. | Terminal |
| `failed` | Thất bại không retry hoặc hết retry theo policy. | Terminal hoặc manual retry nếu policy cho phép |
| `cancelled` | User hủy job trước hoặc trong xử lý an toàn. | Terminal |
| `timeout` | Job vượt timeout cấu hình và worker dừng/rollback an toàn. | Terminal hoặc manual retry nếu policy cho phép |
| `dead_lettered` | Message chuyển DLQ sau poison message, schema lỗi hoặc retry exhaustion cần inspect. | Manual inspect/requeue |

### 5. Progress Reporting via SignalR

```csharp
public class ProgressReporter : IProgressReporter
{
    private readonly IHubContext<GodForgeHub> _hubContext;

    public async Task ReportAsync(Guid jobId, int progress, CancellationToken ct)
    {
        await _hubContext.Clients
            .Group($"project:{projectId}")
            .SendAsync("JobProgressUpdate", new { jobId, progress }, ct);
    }
}
```

## Business Rules quan trọng
- **Idempotency:** Job phải idempotent. Nếu retry, kết quả phải giống lần đầu (upsert, không duplicate).
- **Error isolation:** Lỗi 1 item (1 file, 1 scene) KHÔNG được crash toàn bộ job.
- **Distributed Lock:** Job Git (clone, push, pull, merge) phải acquire Redis lock trước khi chạy.
- **Retry:** Retry tối đa 3 lần với exponential backoff cho lỗi tạm thời. Khi retry, status chuyển `retrying`, sau đó quay lại `queued`.
- **DLQ:** Poison message, schema lỗi hoặc retry exhaustion chuyển message vào DLQ và job sang `dead_lettered` nếu không còn retry tự động.
- **Cancellation:** Hỗ trợ `CancellationToken` để user có thể cancel job đang chạy.
- **Timeout:** Job phải có timeout hợp lý theo `docs/SRS/12-worker-processing.md`; timeout phải ghi status `timeout` hoặc đi qua retry/DLQ nếu queue policy cho phép retry timeout.

## Checklist
- [ ] Job message chứa đủ thông tin cần thiết (`schemaVersion`, `messageId`, `jobId`, `projectId`, `correlationId`, `createdAt`, `attemptCount`, `inputHash`).
- [ ] Handler cập nhật job status đúng lifecycle: `queued`, `running`, `retrying`, `completed`, `failed`, `cancelled`, `timeout`, `dead_lettered`.
- [ ] Progress được report qua SignalR.
- [ ] Error isolation: 1 file lỗi không crash job.
- [ ] Retry policy cấu hình, chuyển `retrying` trước khi requeue và `dead_lettered` khi vào DLQ.
- [ ] CancellationToken được truyền và check.
- [ ] Timeout policy ghi `timeout` và release repository lock an toàn.
- [ ] Activity Log ghi `job.started`, `job.retrying`, `job.completed`, `job.failed`, `job.cancelled`, `job.timeout`, `job.dead_lettered` khi áp dụng.
- [ ] Notification gửi khi job completed/failed/timeout/dead_lettered.
