# Analyzer / Project Health

## Mục tiêu

Module Analyzer phân tích metadata đã parse để tạo health report, health issues, score và dữ liệu tổng hợp cho dashboard.

## Tác nhân

- Developer
- Project Admin
- Worker/System
- Reviewer/QA

## Phạm vi

- Chạy analyze sau parse thành công.
- Đọc Metadata Schema để phát hiện issue.
- Tính health score và lưu lịch sử report.
- Cập nhật cache dashboard.
- Gửi notification khi có issue critical.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-12 | Project Health Analyzer | Phân tích sức khỏe dự án Godot, tính health score và phát hiện vấn đề. | Must | Developer, Worker |
| BR-54 | Async analyze | Analyze job chạy qua RabbitMQ Worker. | Must | System |
| BR-55 | Lưu history | Health report lưu lịch sử, không ghi đè report cũ. | Must | Worker |
| BR-56 | Hiển thị score | Health score hiển thị trên dashboard và project detail. | Must | Viewer+ |
| BR-57 | Yêu cầu parse trước | Analyze yêu cầu metadata parse hoàn thành trước. | Must | Worker |

## Luồng xử lý chính

1. User hoặc hệ thống trigger analyze.
2. API kiểm tra repository ready, parse data available và actor có quyền `developer+`.
3. API tạo job `analyze` và publish message vào RabbitMQ.
4. Analyzer Worker đọc `scenes`, `scene_nodes`, `assets`, `scripts`, `resources`, `dependencies`.
5. Worker chạy health rules: missing resource, missing script, cyclic dependency, unused asset, oversized scene, deep nesting, duplicate resource, missing import.
6. Worker tính health score.
7. Worker lưu `health_reports` và `health_issues`.
8. Worker cập nhật Redis cache cho dashboard và project summary.
9. Nếu có critical issue, hệ thống gửi notification.

## Công thức health score

```text
Health Score = 100 - SUM(penalty_per_issue)

Critical: -10 điểm/issue, cap -50
Warning:  -3 điểm/issue, cap -30
Info:     -1 điểm/issue, cap -10

Minimum score: 0
Maximum score: 100
```

## Health rules tối thiểu

| Issue Type | Severity | Mô tả |
| --- | --- | --- |
| `missing_resource` | Critical | Scene hoặc resource reference file không tồn tại. |
| `missing_script` | Critical | Node gắn script nhưng script không tồn tại. |
| `cyclic_dependency` | Warning | Có vòng phụ thuộc giữa các file. |
| `unused_asset` | Info | Asset không được reference trong dependency graph. |
| `oversized_scene` | Warning | Scene có hơn 500 nodes. |
| `deep_nesting` | Warning | Node tree sâu hơn 10 level. |
| `duplicate_resource` | Info | Resource trùng nội dung ở nhiều path. |
| `missing_import` | Warning | File `.import` tồn tại nhưng source file bị xóa. |

## Ngoại lệ / lỗi

| Tình huống | HTTP Status / Job State | Error Code | Hành vi |
| --- | --- | --- | --- |
| Chưa parse metadata | 400 | `PARSE_REQUIRED` | Từ chối analyze, yêu cầu parse trước. |
| Metadata version cũ hơn job hiện hành | Job ignored | `STALE_METADATA_VERSION` | Không ghi đè report mới hơn. |
| DB tạm lỗi | Job retry | `JOB_TRANSIENT_FAILURE` | Retry theo policy. |
| Rule config invalid | Job failed | `ANALYZE_RULE_CONFIG_INVALID` | Không retry vô hạn; ghi log và DLQ nếu cần. |

## Acceptance Criteria

- AC-70: Chạy analyze sinh health report với score 0-100.
- AC-71: Project có 2 missing resource sinh 2 critical issues và score giảm 20 điểm.
- AC-72: Có issue critical thì user liên quan nhận notification.
- AC-73: Health history giữ report cũ để xem trend.
- AC-74: Analyze khi chưa parse trả lỗi rõ ràng yêu cầu parse trước.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{id}/analyze` | `developer+` | branch/commit/options | `202` analyze job | `PARSE_REQUIRED`, `REPO_NOT_READY` |
| GET | `/api/v1/projects/{id}/health` | `viewer+` | none | Latest health report | `HEALTH_NOT_READY` |
| GET | `/api/v1/projects/{id}/health/history` | `viewer+` | pagination | Health history | `FORBIDDEN` |
| GET | `/api/v1/projects/{id}/health/{reportId}/issues` | `viewer+` | severity/filter | Health issues | `REPORT_NOT_FOUND` |
| GET | `/api/v1/projects/{id}/jobs/{jobId}` | Project member | job id | Job status | `JOB_NOT_FOUND` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `jobs` | Analyze job lifecycle, progress, error code và correlation id. |
| `scenes`, `scene_nodes` | Source data cho scene size, nesting và script attachment rules. |
| `assets`, `scripts`, `resources`, `dependencies` | Source data cho missing/unused/cycle rules. |
| `health_reports` | Lưu report summary, score, severity counts và rule version. |
| `health_issues` | Lưu từng issue, severity, file path và details. |
| `projects` | Cache health score mới nhất nếu cần. |
| `notifications` | Thông báo critical health issues. |

## Ghi chú bảo mật / phân quyền

- Health issue có thể lộ path/structure repository; mọi query phải filter theo project permission.
- Analyzer không execute script hoặc resource.
- Error trả client không chứa server path, stack trace hoặc raw parser exception.
- Health rule version phải lưu để giải thích score tại thời điểm report.
