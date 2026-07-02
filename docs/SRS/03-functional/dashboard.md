# Dashboard

## Purpose

The Dashboard provides an actionable overview of the project: health score, repository status, metadata summary, recent jobs, critical issues, and recent activities.

## Actors

- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Scope

- Project summary.
- Health score and top health issues.
- Repository status.
- Recent jobs.
- Activity feed.
- Notification/job status integration.
- Does not perform direct data modifications other than navigation/action links.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-14 | Dashboard | Display statistics, health score, repository status, recent jobs, and new activities. | Must | Viewer+ |
| BR-14 | Cache-first | The dashboard prioritizes Redis cache with a 5-minute TTL. | Should | System |
| BR-15 | Cache Miss Rebuild | On cache miss, query the DB and rebuild the cache. | Should | System |
| BR-16 | Permission Scoped | The dashboard only displays data the user is authorized to view. | Must | System |

## Main Workflow

1. User opens the project dashboard.
2. API verifies project membership.
3. API reads the dashboard cache from Redis.
4. On a cache miss, the API queries PostgreSQL and rebuilds the cache.
5. API returns the project summary, health, repo status, jobs, activities, and top issues.
6. Frontend subscribes to SignalR to receive job progress and new notifications.

## Displayed Data

| Item | Description | Source |
| --- | --- | --- |
| Project Summary | Total scenes, assets, scripts, resources. | Redis/PostgreSQL metadata |
| Health Score | 0-100 score and latest trend. | `health_reports`, Redis |
| Repository Status | Current branch, latest commit, sync/clone status. | `repositories`, Git workspace |
| Recent Jobs | Last 10 jobs, status, type, duration. | `jobs` |
| Activity Feed | Last 20 activities. | `activities` |
| Health Issues | Top critical/warning issues. | `health_issues`, Redis |

## Exceptions / Errors

| Situation | HTTP Status | Error Code | Behavior |
| --- | --- | --- | --- |
| Project lacks repository | 200 | `REPO_NOT_CONNECTED` | Display empty state to connect repository. |
| No metadata | 200 | `METADATA_NOT_READY` | Display parse/analyze action if authorized. |
| Cache error | 200 / degraded | `CACHE_UNAVAILABLE` | Fallback to DB query and log a warning. |
| User lacks permission | 403 | `FORBIDDEN` | Do not return dashboard data. |

## Acceptance Criteria

- AC-18: Opening the dashboard displays the full summary, health, repo status, recent jobs, activities, and top issues with a P95 under 2 seconds.
- AC-19: A cache hit returns a response in under 500ms.
- AC-20: Users only see data for projects they are authorized to access.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/dashboard` | `viewer+` | optional time range | Dashboard summary | `FORBIDDEN` |
| GET | `/api/v1/projects/{projectId}/jobs` | Project member | pagination | Recent jobs | `FORBIDDEN` |
| GET | `/api/v1/projects/{projectId}/activities` | Project member | pagination | Activity feed | `FORBIDDEN` |
| GET | `/api/v1/projects/{projectId}/health` | `viewer+` | none | Latest health report | `HEALTH_NOT_READY` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `projects` | Project summary and health score cache. |
| `repositories` | Repository status. |
| `jobs` | Recent jobs and job status. |
| `activities` | Activity feed. |
| `notifications` | Unread indicator if displayed. |
| `health_reports`, `health_issues` | Health score and top issues. |
| `scenes`, `assets`, `scripts`, `resources` | Metadata counts. |
| Redis | Dashboard cache. |

## Security / Authorization Notes

- The dashboard is an aggregation of multiple sources, so all queries must be project-scoped.
- Do not share cache responses between users with different permissions if the payload contains role-dependent data.
- Do not expose internal workspace paths, credentials, or raw exceptions.
