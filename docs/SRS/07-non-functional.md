# 7. Non-functional Requirements

## 7.1 Performance

| ID | Requirement | Target Metric | Measurement Condition |
| --- | --- | --- | --- |
| NFR-01 | Fast CRUD API response | P95 < 500ms | Excludes async jobs and external Git network. |
| NFR-02 | Dashboard cache hit | P95 < 500ms | Redis cache ready. |
| NFR-03 | Dashboard cache miss | P95 < 2s | Medium project, metadata already in DB. |
| NFR-04 | Search | P95 < 1.5s | Query <= 200 chars, pageSize <= 20, medium project. |
| NFR-05 | Scene/Asset listing | P95 < 1s | Metadata parsed, paginated query. |
| NFR-06 | Git status/history | P95 < 1s | Local repository workspace ready, medium repo. |
| NFR-07 | Clone small/medium repo | Repo < 500MB completes < 5 mins | Depends on Git provider network. |
| NFR-08 | Parse job | 100 scenes complete < 2 mins | Incremental parse is faster than full parse. |
| NFR-09 | Analyze job | Small project < 1 min, medium project < 5 mins | After successful parse. |
| NFR-10 | Scene diff | P95 < 30s if computation needed; cache hit < 500ms | Medium scene, both revisions readable. |
| NFR-11 | Queue lag | P95 queue wait time < 60s | Normal load, adequately configured worker pool. |

## 7.2 Capacity / Scalability

| ID | Metric | MVP Target |
| --- | --- | --- |
| NFR-12 | Concurrent users | 50 |
| NFR-13 | Total projects | 100 |
| NFR-14 | Max repository size | 2 GB |
| NFR-15 | Max single file | 100 MB |
| NFR-16 | Max scenes per project | 1000 |
| NFR-17 | Max nodes per scene | 5000 |
| NFR-18 | Max assets per project | 10000 |

Scaling strategy:

- API is stateless to allow horizontal scaling.
- Workers scale according to queue depth and job type.
- PostgreSQL prioritizes index/query tuning before read replicas.
- Redis is used for cache/locks/rate limiting; can be clustered if needed.
- MinIO stores large artifacts to prevent database bloat.

## 7.3 Reliability

| ID | Requirement | Metric/Policy |
| --- | --- | --- |
| NFR-19 | API availability non-production/demo | >= 95% |
| NFR-20 | RTO | < 4 hours for database/core services in project/staging environment. |
| NFR-21 | RPO | < 1 hour for PostgreSQL core data if supported by backup/WAL. |
| NFR-22 | Worker retry | Retry transient errors via `retrying` status; do not infinitely retry validation/credential errors. |
| NFR-23 | DLQ | Poison messages or recurring timeouts must move to `dead_lettered`/DLQ after retry policy exhaustion. |
| NFR-24 | Idempotency | Job retries must not create duplicate metadata/notifications or overwrite newer metadata. |
| NFR-25 | Repository lock | Mutating Git operations must have a lock TTL and safe release mechanisms. |

## 7.4 Observability

### Logging

- Logs in structured JSON format.
- Mandatory fields: `timestamp`, `level`, `message`, `correlationId`, `sourceContext`.
- If applicable: `userId`, `projectId`, `jobId`, `repositoryId`.
- Do not log passwords, tokens, credentials, Authorization headers, or plaintext PATs.

### Metrics

| Metric | Description |
| --- | --- |
| `http_request_duration_seconds` | API latency histogram. |
| `http_requests_total` | Request count by route/status. |
| `active_connections` | SignalR active connections. |
| `job_duration_seconds` | Worker job duration. |
| `job_queue_depth` | Number of pending jobs per queue. |
| `job_retry_total` | Number of retries per job type. |
| `job_dead_letter_total` | Number of messages sent to DLQ. |
| `db_connection_pool_size` | DB connection pool usage. |
| `cache_hit_ratio` | Redis cache hits/misses. |
| `repository_lock_contention_total` | Number of lock failures/contentions. |

### Alerting

- API 5xx rate > 1% within 5 minutes.
- Queue depth exceeds threshold for the worker pool.
- Job failure rate spikes abnormally.
- DB/Redis/RabbitMQ/MinIO becomes unavailable.
- New messages arrive in DLQ.
- Disk/storage artifacts exceed thresholds.

## 7.5 Backup / Restore

| ID | Data | Policy |
| --- | --- | --- |
| NFR-26 | PostgreSQL Core Schema | Backed up most frequently; prioritize restore before metadata. |
| NFR-27 | PostgreSQL Metadata Schema | Periodic backup or regenerate from repository if acceptable recovery time allows. |
| NFR-28 | MinIO artifacts | Backup buckets according to retention; diffs/previews may have a TTL. |
| NFR-29 | Redis | No backup required for cache/locks; sessions/tokens depend on configuration. |
| NFR-30 | RabbitMQ | Durable queues for critical jobs; DLQ kept long enough for investigation. |

Restore drills must test PostgreSQL and MinIO on staging or an equivalent environment at a minimum.

## 7.6 Maintainability

| ID | Requirement | Policy |
| --- | --- | --- |
| NFR-31 | Architecture | Backend maintains strict separation of domain/application/infrastructure; workers do not bypass business rules. |
| NFR-32 | API versioning | Public endpoints use `/api/v1`; breaking changes require a new version or clear migration path. |
| NFR-33 | Database migration | Schema changes via reviewed migrations; no manual modifications to production DB. |
| NFR-34 | Error contract | Stable error codes for UI/QA/test automation reliance. |
| NFR-35 | Documentation | Requirement changes must update functional docs, traceability, and testing docs. |
| NFR-36 | Code quality | No hardcoded secrets/configs; lint/test according to project conventions. |

## 7.7 Compatibility

| ID | Requirement | Policy |
| --- | --- | --- |
| NFR-37 | Godot version | Prioritize Godot 4.x. |
| NFR-38 | File format | Parser supports `.tscn`, `.tres`, `.res`, `.gd` at the metadata level required for SRS. |
| NFR-39 | Browser | Frontend supports modern browsers: supported versions of Chromium, Firefox, Edge. |
| NFR-40 | Repository | MVP supports Git HTTPS repositories; SSH is out of current scope. |

## 7.8 Security NFR

| ID | Requirement | Metric/Policy |
| --- | --- | --- |
| NFR-41 | Password hashing | bcrypt cost >= 12. |
| NFR-42 | Token lifetime | Access token 15 minutes, refresh token 7 days. |
| NFR-43 | Secret handling | No plaintext credentials in DB/logs/APIs. |
| NFR-44 | RBAC | 100% of project-scoped APIs must have permission tests. |
| NFR-45 | Input validation | Repository URLs, file paths, search queries, and worker messages are validated server-side. |
| NFR-46 | Error sanitization | Production errors contain no stack traces/internal paths. |

## 7.9 Testing Quality Targets

| ID | Requirement | Target |
| --- | --- | --- |
| NFR-47 | Unit test | Domain logic, validators, permission checkers, and critical parser rules. |
| NFR-48 | Integration test | Critical path API + DB + Redis/RabbitMQ/MinIO where applicable. |
| NFR-49 | Security test | Auth, RBAC, input validation, secret leakage, and path traversal. |
| NFR-50 | Performance test | Dashboard, search, parse/analyze, scene diff using representative datasets. |

## 7.10 MVP Decisions

- Default targets for MVP/staging: minimum 95% uptime, RTO under 4 hours, RPO under 1 hour for PostgreSQL core data if supported by backup/WAL.
- Frontend supports modern browsers maintained by their vendors: Chromium/Chrome, Edge, and Firefox. Detailed compatibility requirements can be tightened as production nears.
