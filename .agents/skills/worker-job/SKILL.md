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
            // Mark failed
            _logger.LogError(ex, "Job {JobId} failed", message.JobId);
            job.MarkAsFailed(ex.Message, "JOB_PROCESSING_ERROR");
            await _jobRepository.SaveAsync(cancellationToken);
            await _notificationService.NotifyJobFailed(job, cancellationToken);
        }
    }
}
```

### 4. Job Lifecycle (State Machine)

```
queued → running → completed
                 → failed
                 → cancelled
```

- `queued`: Job đã tạo, đang chờ Worker pick up.
- `running`: Worker đang xử lý. `progress` cập nhật 0-100.
- `completed`: Hoàn thành thành công.
- `failed`: Thất bại, `error_message` và `error_code` được ghi.
- `cancelled`: User hủy job.

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
- **Retry:** Retry tối đa 3 lần với exponential backoff. Sau 3 lần → dead-letter queue.
- **Cancellation:** Hỗ trợ `CancellationToken` để user có thể cancel job đang chạy.
- **Timeout:** Job phải có timeout hợp lý theo `docs/SRS/12-worker-processing.md` (clone theo repo size, fetch ngắn hơn clone, parse/analyze theo số file/graph size, diff thường 30 giây đến 2 phút, preview 30-60 giây).

## Checklist
- [ ] Job message chứa đủ thông tin cần thiết (`schemaVersion`, `messageId`, `jobId`, `projectId`, `correlationId`, `createdAt`, `attemptCount`, `inputHash`).
- [ ] Handler cập nhật job status đúng lifecycle.
- [ ] Progress được report qua SignalR.
- [ ] Error isolation: 1 file lỗi không crash job.
- [ ] Retry policy cấu hình.
- [ ] CancellationToken được truyền và check.
- [ ] Activity Log ghi `job.started`, `job.completed`, `job.failed`.
- [ ] Notification gửi khi job completed/failed.
