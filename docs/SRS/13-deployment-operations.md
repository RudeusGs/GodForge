# 13. Deployment & Operations

## Local dev setup

Local development cần tối thiểu:

- Frontend Vue 3.
- Backend API.
- PostgreSQL.
- Redis.
- RabbitMQ.
- MinIO.
- Worker process.

Quy trình local:

1. Cấu hình environment variables.
2. Khởi động hạ tầng bằng Docker Compose hoặc service local tương đương.
3. Chạy database migration.
4. Chạy API.
5. Chạy worker.
6. Chạy frontend.
7. Seed System Admin/dev data nếu cần.

## Docker Compose setup

Docker Compose cho dev/staging nên có các service:

| Service | Vai trò |
| --- | --- |
| `godforge-api` | REST API và SignalR. |
| `godforge-worker` | Parser/Analyzer/Diff/Preview/Git workers hoặc worker host. |
| `godforge-frontend` | Vue frontend nếu deploy containerized. |
| `postgres` | Database. |
| `redis` | Cache/rate limit/repository lock. |
| `rabbitmq` | Async queue và DLQ. |
| `minio` | Object storage. |
| `prometheus` | Metrics scrape. |
| `grafana` | Monitoring dashboard. |
| `loki` | Log aggregation nếu bật. |

Internal services không expose public port ngoài nhu cầu dev.

## Production deployment overview

- API và frontend đặt sau reverse proxy/load balancer.
- HTTPS bắt buộc.
- API stateless để scale ngang.
- Worker scale theo queue depth.
- PostgreSQL, Redis, RabbitMQ và MinIO chạy trong private network.
- Secret cấp qua secret manager hoặc environment injection an toàn.
- Migration chạy có kiểm soát trước khi deploy version mới.

## Environment variables

| Biến | Mục đích | Secret |
| --- | --- | :---: |
| `GODFORGE_ENVIRONMENT` | dev/staging/prod. | Không |
| `GODFORGE_API_BASE_URL` | Public API URL. | Không |
| `GODFORGE_DATABASE_URL` | PostgreSQL connection string. | Có |
| `GODFORGE_REDIS_URL` | Redis connection string. | Có |
| `GODFORGE_RABBITMQ_URL` | RabbitMQ connection string. | Có |
| `GODFORGE_MINIO_ENDPOINT` | MinIO endpoint. | Không |
| `GODFORGE_MINIO_ACCESS_KEY` | MinIO access key. | Có |
| `GODFORGE_MINIO_SECRET_KEY` | MinIO secret key. | Có |
| `GODFORGE_JWT_SIGNING_KEY` | JWT signing key nếu dùng symmetric signing. | Có |
| `GODFORGE_ENCRYPTION_KEY` | Key mã hóa Git credential. | Có |
| `GODFORGE_CORS_ORIGINS` | CORS allowlist. | Không |
| `GODFORGE_SMTP_*` | Email notification/invite nếu bật. | Có |

## Secret management

- Không commit `.env` production.
- Secret production phải được rotate được.
- Log không in connection string hoặc secret.
- Credential Git trong DB vẫn phải mã hóa ngay cả khi DB private.

## Database migration

- Migration phải được review cùng thay đổi code.
- Backup trước migration production quan trọng.
- Migration destructive cần kế hoạch rollback hoặc data migration an toàn.
- App version và migration version phải tương thích.

## Backup / restore

| Dữ liệu | Backup | Restore priority |
| --- | --- | --- |
| PostgreSQL Core | Full + incremental/WAL nếu có | Cao nhất |
| PostgreSQL Metadata | Định kỳ hoặc regenerate | Trung bình |
| MinIO artifacts | Bucket backup theo retention | Trung bình |
| RabbitMQ config/DLQ | Durable queues + config backup | Trung bình |
| Redis cache/lock | Không bắt buộc | Thấp |

Restore drill phải xác nhận:

- API đọc được core data.
- Membership/RBAC đúng.
- Repository config còn credential reference hợp lệ.
- Metadata hoặc regenerate pipeline hoạt động.
- Artifact MinIO đọc được nếu còn trong retention.

## Monitoring

Prometheus scrape:

- API metrics.
- Worker metrics.
- RabbitMQ queue depth.
- PostgreSQL health/pool.
- Redis availability.
- MinIO health.

Grafana dashboard tối thiểu:

- API latency/error rate.
- Worker success/fail/retry/DLQ.
- Queue depth theo queue.
- DB/Redis/RabbitMQ/MinIO health.
- Repository lock contention.

## Logging

- Structured JSON log.
- Correlation id xuyên API/worker.
- Log aggregation qua Loki hoặc stack tương đương.
- Retention log 30-90 ngày tùy môi trường.
- Không log secret, token, password, PAT hoặc server path nhạy cảm.

## Alerting

| Alert | Điều kiện gợi ý |
| --- | --- |
| API high error rate | 5xx > 1% trong 5 phút. |
| API latency high | P95 vượt NFR trong 10 phút. |
| Queue backlog | Queue depth vượt ngưỡng theo worker pool. |
| Worker failures | Failure/retry tăng bất thường. |
| DLQ not empty | Có message mới trong DLQ. |
| DB unavailable | Health check fail. |
| Redis/RabbitMQ/MinIO unavailable | Health check fail. |
| Storage pressure | Disk/object storage vượt ngưỡng. |

## Incident handling

1. Ghi nhận alert, thời điểm và correlation id nếu có.
2. Xác định blast radius: API, worker, DB, queue, storage hay Git provider.
3. Kiểm tra dashboard metrics và logs.
4. Nếu liên quan queue, kiểm tra job state và DLQ.
5. Khôi phục service theo runbook.
6. Ghi post-incident note: nguyên nhân, tác động, cách phòng ngừa.

## Data retention

| Loại dữ liệu | Retention đề xuất |
| --- | --- |
| Project soft delete | 30 ngày trước hard-delete nếu policy bật. |
| Notification | 90 ngày. |
| Activity/Audit Log | 1 năm hoặc theo policy. |
| Application logs | 30-90 ngày. |
| Diff artifacts | 7 ngày hoặc theo cache policy. |
| Reports export | 30 ngày. |
| Redis cache | TTL phút/giờ theo key. |

## Disaster recovery

| Sự cố | Tác động | Khôi phục |
| --- | --- | --- |
| API container lỗi | User không truy cập API. | Restart/scale API, kiểm tra health endpoint và dependencies. |
| Worker lỗi | Job backlog tăng. | Restart worker, kiểm tra queue/DLQ, requeue job an toàn. |
| PostgreSQL lỗi | Mất đọc/ghi core data. | Restore backup, chạy migration nếu cần, kiểm tra integrity. |
| RabbitMQ lỗi | Không tạo/xử lý job mới. | Khôi phục broker, queue, DLQ và resume worker. |
| MinIO lỗi | Không đọc/ghi artifact. | Restore bucket hoặc regenerate artifact nếu có thể. |
| Redis lỗi | Cache/lock/rate limit mất tạm thời. | Restart Redis; cache rebuild; kiểm tra lock stale. |

Core data phải được ưu tiên khôi phục trước metadata/artifact vì metadata có thể tái tạo từ repository trong nhiều trường hợp.

## Quyết định MVP

- Dev/staging dùng user secrets hoặc environment variables; production dùng secret manager hoặc environment injection an toàn của nền tảng triển khai.
- Retention mặc định dùng bảng Data retention ở trên; production có thể override bằng cấu hình nhưng không được ngắn hơn yêu cầu audit đã phê duyệt.
