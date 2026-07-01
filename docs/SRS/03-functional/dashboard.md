# Dashboard

## Mục tiêu

Dashboard cung cấp tổng quan có thể hành động về project: health score, repository status, metadata summary, job gần đây, issue nghiêm trọng và activity mới.

## Tác nhân

- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Phạm vi

- Project summary.
- Health score và top health issues.
- Repository status.
- Recent jobs.
- Activity feed.
- Notification/job status integration.
- Không thực hiện chỉnh sửa dữ liệu trực tiếp ngoài navigation/action link.

## Yêu cầu chức năng

| ID | Tên yêu cầu | Mô tả | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-14 | Dashboard | Hiển thị thống kê, health score, repository status, job gần đây và activity mới. | Must | Viewer+ |
| BR-14 | Cache-first | Dashboard ưu tiên Redis cache với TTL 5 phút. | Should | System |
| BR-15 | Cache miss rebuild | Cache miss thì query DB và rebuild cache. | Should | System |
| BR-16 | Permission scoped | Dashboard chỉ hiển thị dữ liệu user có quyền xem. | Must | System |

## Luồng xử lý chính

1. User mở project dashboard.
2. API kiểm tra project membership.
3. API đọc dashboard cache từ Redis.
4. Nếu cache miss, API query PostgreSQL và rebuild cache.
5. API trả project summary, health, repo status, jobs, activity và top issues.
6. Frontend subscribe SignalR để nhận job progress và notification mới.

## Dữ liệu hiển thị

| Mục | Mô tả | Nguồn |
| --- | --- | --- |
| Project Summary | Tổng scene, asset, script, resource. | Redis/PostgreSQL metadata |
| Health Score | Điểm 0-100 và trend gần nhất. | `health_reports`, Redis |
| Repository Status | Branch hiện tại, commit mới nhất, sync/clone status. | `repositories`, Git workspace |
| Recent Jobs | 10 job gần nhất, status, type, duration. | `jobs` |
| Activity Feed | 20 hoạt động gần nhất. | `activities` |
| Health Issues | Top critical/warning issues. | `health_issues`, Redis |

## Ngoại lệ / lỗi

| Tình huống | HTTP Status | Error Code | Hành vi |
| --- | --- | --- | --- |
| Project chưa có repository | 200 | `REPO_NOT_CONNECTED` | Hiển thị empty state connect repository. |
| Chưa có metadata | 200 | `METADATA_NOT_READY` | Hiển thị action parse/analyze nếu có quyền. |
| Cache lỗi | 200/ degraded | `CACHE_UNAVAILABLE` | Fallback query DB và ghi log warning. |
| User không có quyền | 403 | `FORBIDDEN` | Không trả dashboard data. |

## Acceptance Criteria

- AC-18: Mở dashboard hiển thị đủ summary, health, repo status, recent jobs, activity và top issues trong P95 dưới 2 giây.
- AC-19: Cache hit trả response dưới 500ms.
- AC-20: User chỉ thấy dữ liệu project được phân quyền.

## API liên quan

| Method | Path | Permission | Request chính | Response chính | Error chính |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{id}/dashboard` | `viewer+` | time range optional | Dashboard summary | `FORBIDDEN` |
| GET | `/api/v1/projects/{id}/jobs` | Project member | pagination | Recent jobs | `FORBIDDEN` |
| GET | `/api/v1/projects/{id}/activities` | Project member | pagination | Activity feed | `FORBIDDEN` |
| GET | `/api/v1/projects/{id}/health` | `viewer+` | none | Latest health report | `HEALTH_NOT_READY` |

## Database liên quan

| Bảng | Vai trò |
| --- | --- |
| `projects` | Project summary và health score cache. |
| `repositories` | Repository status. |
| `jobs` | Recent jobs và trạng thái job. |
| `activities` | Activity feed. |
| `notifications` | Unread indicator nếu hiển thị. |
| `health_reports`, `health_issues` | Health score và top issues. |
| `scenes`, `assets`, `scripts`, `resources` | Metadata counts. |
| Redis | Dashboard cache. |

## Ghi chú bảo mật / phân quyền

- Dashboard là aggregation nhiều nguồn nên tất cả query phải có project scope.
- Không cache response chung giữa user có quyền khác nhau nếu payload có dữ liệu role-dependent.
- Không hiển thị internal workspace path, credential hoặc raw exception.
