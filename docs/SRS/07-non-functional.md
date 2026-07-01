# 7. Non-functional Requirements

## 7.1 Performance

| ID | Yêu cầu | Metric mục tiêu | Điều kiện đo |
| --- | --- | --- | --- |
| NFR-01 | API CRUD phản hồi nhanh | P95 < 500ms | Không bao gồm job async và external Git network. |
| NFR-02 | Dashboard cache hit | P95 < 500ms | Redis cache sẵn sàng. |
| NFR-03 | Dashboard cache miss | P95 < 2s | Project vừa, metadata đã có trong DB. |
| NFR-04 | Search | P95 < 1.5s | Query <= 200 ký tự, pageSize <= 20, project vừa. |
| NFR-05 | Scene/Asset listing | P95 < 1s | Metadata đã parse, query phân trang. |
| NFR-06 | Git status/history | P95 < 1s | Repository local workspace ready, repo vừa. |
| NFR-07 | Clone repo nhỏ/vừa | Repo < 500MB hoàn thành < 5 phút | Phụ thuộc network Git provider. |
| NFR-08 | Parse job | 100 scenes hoàn thành < 2 phút | Incremental parse nhanh hơn full parse. |
| NFR-09 | Analyze job | Project nhỏ < 1 phút, project vừa < 5 phút | Sau parse thành công. |
| NFR-10 | Scene diff | P95 < 30s nếu cần compute; cache hit < 500ms | Scene vừa, hai revision đọc được. |
| NFR-11 | Queue lag | P95 thời gian chờ queue < 60s | Tải bình thường, worker pool đủ cấu hình. |

## 7.2 Capacity / Scalability

| ID | Metric | Target MVP |
| --- | --- | --- |
| NFR-12 | Concurrent users | 50 |
| NFR-13 | Total projects | 100 |
| NFR-14 | Max repository size | 2 GB |
| NFR-15 | Max single file | 100 MB |
| NFR-16 | Max scenes per project | 1000 |
| NFR-17 | Max nodes per scene | 5000 |
| NFR-18 | Max assets per project | 10000 |

Scaling strategy:

- API stateless để có thể scale ngang.
- Worker scale theo queue depth và job type.
- PostgreSQL ưu tiên index/query tuning trước read replicas.
- Redis dùng cache/lock/rate limit; có thể cluster khi cần.
- MinIO lưu artifact lớn để tránh phình database.

## 7.3 Reliability

| ID | Yêu cầu | Metric/Quy định |
| --- | --- | --- |
| NFR-19 | API availability non-production/demo | >= 95% |
| NFR-20 | RTO | < 4 giờ cho database/core service trong môi trường đồ án/staging. |
| NFR-21 | RPO | < 1 giờ cho PostgreSQL core data nếu backup/WAL hỗ trợ. |
| NFR-22 | Worker retry | Retry lỗi tạm thời qua status `retrying`; không retry vô hạn lỗi validation/credential. |
| NFR-23 | DLQ | Poison message hoặc timeout lặp lại phải chuyển `dead_lettered`/DLQ sau khi hết retry theo policy. |
| NFR-24 | Idempotency | Retry job không tạo duplicate metadata/notification hoặc ghi đè metadata mới hơn. |
| NFR-25 | Repository lock | Mutating Git operations phải có lock TTL và release an toàn. |

## 7.4 Observability

### Logging

- Log dạng JSON structured.
- Fields bắt buộc: `timestamp`, `level`, `message`, `correlationId`, `sourceContext`.
- Nếu có: `userId`, `projectId`, `jobId`, `repositoryId`.
- Không log password, token, credential, Authorization header hoặc plaintext PAT.

### Metrics

| Metric | Mô tả |
| --- | --- |
| `http_request_duration_seconds` | API latency histogram. |
| `http_requests_total` | Request count theo route/status. |
| `active_connections` | SignalR active connections. |
| `job_duration_seconds` | Worker job duration. |
| `job_queue_depth` | Số job chờ theo queue. |
| `job_retry_total` | Số retry theo job type. |
| `job_dead_letter_total` | Số message vào DLQ. |
| `db_connection_pool_size` | DB pool usage. |
| `cache_hit_ratio` | Redis cache hit/miss. |
| `repository_lock_contention_total` | Số lần lock fail/contended. |

### Alerting

- API 5xx rate > 1% trong 5 phút.
- Queue depth vượt ngưỡng theo worker pool.
- Job failure rate tăng bất thường.
- DB/Redis/RabbitMQ/MinIO unavailable.
- DLQ có message mới.
- Disk/storage artifact vượt ngưỡng.

## 7.5 Backup / Restore

| ID | Dữ liệu | Chính sách |
| --- | --- | --- |
| NFR-26 | PostgreSQL Core Schema | Backup thường xuyên nhất; ưu tiên restore trước metadata. |
| NFR-27 | PostgreSQL Metadata Schema | Backup định kỳ hoặc regenerate từ repository nếu chấp nhận thời gian phục hồi. |
| NFR-28 | MinIO artifacts | Backup bucket theo retention; diff/preview có thể TTL. |
| NFR-29 | Redis | Không yêu cầu backup cho cache/lock; session/token tùy cấu hình. |
| NFR-30 | RabbitMQ | Durable queue cho job quan trọng; DLQ giữ đủ lâu để điều tra. |

Restore drill tối thiểu phải kiểm tra PostgreSQL và MinIO trên staging hoặc môi trường tương đương.

## 7.6 Maintainability

| ID | Yêu cầu | Quy định |
| --- | --- | --- |
| NFR-31 | Architecture | Backend giữ tách biệt domain/application/infrastructure; worker không bypass business rules. |
| NFR-32 | API versioning | Public endpoint dùng `/api/v1`; breaking change cần version mới hoặc migration rõ ràng. |
| NFR-33 | Database migration | Schema change qua migration có review; không sửa DB production thủ công. |
| NFR-34 | Error contract | Error code ổn định để UI/QA/test automation dựa vào. |
| NFR-35 | Documentation | Requirement change phải cập nhật functional docs, traceability và testing docs. |
| NFR-36 | Code quality | Không hardcode secret/config; lint/test theo convention dự án. |

## 7.7 Compatibility

| ID | Yêu cầu | Quy định |
| --- | --- | --- |
| NFR-37 | Godot version | Ưu tiên Godot 4.x. |
| NFR-38 | File format | Parser hỗ trợ `.tscn`, `.tres`, `.res`, `.gd` ở mức metadata cần cho SRS. |
| NFR-39 | Browser | Frontend hỗ trợ browser hiện đại: Chromium, Firefox, Edge bản còn được hỗ trợ. |
| NFR-40 | Repository | MVP hỗ trợ Git HTTPS repository; SSH ngoài phạm vi hiện tại. |

## 7.8 Security NFR

| ID | Yêu cầu | Metric/Quy định |
| --- | --- | --- |
| NFR-41 | Password hashing | bcrypt cost >= 12. |
| NFR-42 | Token lifetime | Access token 15 phút, refresh token 7 ngày. |
| NFR-43 | Secret handling | Không plaintext credential trong DB/log/API. |
| NFR-44 | RBAC | 100% API project-scoped phải có permission test. |
| NFR-45 | Input validation | Repository URL, file path, search query và worker message validate server-side. |
| NFR-46 | Error sanitization | Production error không có stack trace/internal path. |

## 7.9 Testing quality targets

| ID | Yêu cầu | Target |
| --- | --- | --- |
| NFR-47 | Unit test | Domain logic, validators, permission checker và parser rules quan trọng. |
| NFR-48 | Integration test | Critical path API + DB + Redis/RabbitMQ/MinIO khi có thể. |
| NFR-49 | Security test | Auth, RBAC, input validation, secret leakage và path traversal. |
| NFR-50 | Performance test | Dashboard, search, parse/analyze, scene diff với dataset đại diện. |

## 7.10 Quyết định MVP

- Mục tiêu mặc định cho MVP/staging: uptime tối thiểu 95%, RTO dưới 4 giờ, RPO dưới 1 giờ cho PostgreSQL core data nếu backup/WAL hỗ trợ.
- Frontend hỗ trợ các browser hiện đại còn được vendor hỗ trợ: Chromium/Chrome, Edge và Firefox. Compatibility chi tiết có thể siết lại khi bước sang production.
