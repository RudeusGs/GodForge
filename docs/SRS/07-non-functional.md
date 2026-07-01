# 7. Yêu cầu Phi chức năng (Non-functional Requirements)

## 7.1 Hiệu năng (Performance)

### API Response Time (P95)
| Loại API | Target P95 | Mô tả |
|----------|-----------|-------|
| CRUD đơn giản (GET/POST/PUT/DELETE) | < 500ms | Users, Projects, Settings |
| Dashboard (cached) | < 500ms | Redis cache hit |
| Dashboard (cache miss) | < 2s | Query DB + rebuild cache |
| Search | < 1s | Full-text search PostgreSQL |
| Git Status | < 1s | Đọc workspace trên server |
| Commit History | < 1s | Git log command |
| Scene/Asset listing | < 1s | Paginated query |

### Async Job Throughput
| Job Type | Target Duration | Mô tả |
|----------|----------------|-------|
| Clone (repo < 500MB) | < 5 phút | Phụ thuộc network |
| Parse (100 scenes) | < 2 phút | Incremental nhanh hơn |
| Analyze | < 1 phút | Depends on project size |
| Diff (1 scene) | < 30 giây | Cached gần như instant |

### Database
- Connection pool: 20-50 connections.
- Query timeout: 30 giây.
- Slow query log: > 1 giây.

## 7.2 Khả năng mở rộng (Scalability)

### Capacity Targets (MVP)
| Metric | Target |
|--------|--------|
| Concurrent users | 50 |
| Total projects | 100 |
| Max repo size | 2 GB |
| Max single file | 100 MB |
| Max scenes per project | 1000 |
| Max nodes per scene | 5000 |
| Max assets per project | 10000 |

### Scaling Strategy
- **Horizontal:** API stateless → có thể scale bằng nhiều instance sau này.
- **Worker:** Có thể chạy nhiều Worker instance xử lý song song queue.
- **Database:** Vertical scaling trước (MVP), read replicas khi cần.
- **Cache:** Redis single-node (MVP), cluster khi cần.

## 7.3 Khả dụng (Availability)

| Metric | Target |
|--------|--------|
| Uptime (non-production) | 95% |
| Planned downtime window | Chủ Nhật 2:00-4:00 AM UTC |
| Recovery Time Objective (RTO) | < 4 giờ |
| Recovery Point Objective (RPO) | < 1 giờ (PostgreSQL backup interval) |

## 7.4 Logging & Monitoring

### Structured Logging
- Format: JSON structured log (Serilog).
- Fields bắt buộc: `timestamp`, `level`, `message`, `correlationId`, `userId`, `sourceContext`.
- Log levels: `Debug` (dev only), `Information`, `Warning`, `Error`, `Fatal`.

### Log Rules
- **KHÔNG** log sensitive data: password, token, credential, PII.
- Request/Response body log ở level `Debug` — tắt ở production.
- Mọi unhandled exception phải log `Error` kèm stack trace + correlation ID.

### Monitoring (Observability Stack)
| Tool | Vai trò |
|------|---------|
| Serilog | Structured logging |
| Loki | Log aggregation |
| Prometheus | Metrics collection |
| Grafana | Dashboard & alerting |

### Key Metrics
| Metric | Mô tả |
|--------|-------|
| `http_request_duration_seconds` | API response time histogram |
| `http_requests_total` | Request count by status code |
| `active_connections` | Số connection WebSocket/SignalR |
| `job_duration_seconds` | Worker job duration histogram |
| `job_queue_depth` | Số job đang chờ trong queue |
| `db_connection_pool_size` | Connection pool usage |
| `cache_hit_ratio` | Redis cache hit/miss ratio |

## 7.5 Xử lý lỗi (Error Handling Convention)

### Nguyên tắc
1. **Không bao giờ** trả stack trace cho client ở production.
2. Mọi lỗi phải có `correlationId` để trace.
3. Lỗi validation trả 400 với `details[]` chỉ rõ field sai.
4. Lỗi server (5xx) log `Error` và trả generic message cho client.
5. Lỗi business logic dùng custom error code (e.g., `REPO_LOCKED`, `LAST_OWNER_CANNOT_BE_REMOVED`).

### Global Exception Middleware
```
Request → Auth Middleware → RBAC Check → Controller → Use Case
                                                        ↓
                                              Exception thrown
                                                        ↓
                                         Global Exception Handler
                                                        ↓
                                    Map to Standard Error Response
                                    Log Error with CorrelationId
                                    Return 4xx/5xx to client
```

### Error Code Naming Convention
- Format: `SCREAMING_SNAKE_CASE`
- Prefix theo domain: `AUTH_`, `PROJECT_`, `GIT_`, `JOB_`, `REPO_`
- Ví dụ: `AUTH_INVALID_CREDENTIALS`, `GIT_REPO_LOCKED`, `PROJECT_NAME_EXISTS`

## 7.6 Internationalization (i18n)
- **MVP:** Chỉ hỗ trợ tiếng Anh.
- API error messages bằng tiếng Anh.
- Frontend UI bằng tiếng Anh.
- Chuẩn bị i18n keys cho tương lai nhưng không triển khai multi-language trong MVP.

## 7.7 Testing Strategy

### Test Pyramid
| Level | Framework | Coverage Target | Mô tả |
|-------|-----------|----------------|-------|
| Unit Tests | xUnit + Moq | > 80% domain logic | Test business rules, validators, mappers |
| Integration Tests | xUnit + TestContainers | Critical paths | Test DB queries, RabbitMQ, Redis |
| E2E Tests | (Post-MVP) | Happy paths | Full API flow tests |

### Test Naming Convention
```
MethodName_StateUnderTest_ExpectedBehavior
```
Ví dụ: `Login_WithInvalidPassword_Returns401`

## 7.8 Code Quality

### Conventions
- **C# Coding Standards:** Theo .NET coding conventions.
- **Naming:** PascalCase cho public, camelCase cho private, SCREAMING_SNAKE cho constants.
- **EditorConfig:** Bắt buộc format theo `.editorconfig` (đã có).
- **Git Commits:** Conventional Commits (`type(scope): subject`). Enforced bởi Husky + Commitlint.
- **Branching:** Git Flow (`main`, `develop`, `feature/*`, `fix/*`, `release/*`).

### Code Review Checklist
- [ ] Tuân thủ Clean Architecture (no domain → infrastructure dependency).
- [ ] Có unit test cho business logic mới.
- [ ] Không có TODO/HACK comment.
- [ ] Error handling đầy đủ, không swallow exception.
- [ ] Không hardcode secret/config.
- [ ] Activity Log được ghi cho mọi write operation.
