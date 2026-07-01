# 10. Traceability

## Purpose

Traceability mapping ensures that each requirement has corresponding API, database, UI, and test case representations. When requirements are added or modified, update this table and `11-testing-acceptance.md`.

| Requirement ID | Module | API | Database | UI Screen | Test Case |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Auth | `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout` | `users`, `refresh_tokens`, `activities` | Login | TC-AUTH-001 |
| FR-02 | User/RBAC | `/api/v1/users`, `/api/v1/projects/{id}/members` | `users`, `project_members`, `activities` | Admin/User Management, Project Members | TC-AUTH-002 |
| FR-03 | Project | `/api/v1/projects` | `projects`, `project_members`, `project_settings`, `activities` | Project List, Project Detail | TC-PROJ-001 |
| FR-03.1 | Project Membership | `GET/POST/PUT/DELETE /api/v1/projects/{id}/members` | `project_members`, `users`, `activities` | Project Members | TC-PROJ-MEMBER-001 |
| FR-04 | Repository | `POST /api/v1/projects/{id}/repository` | `repositories`, `jobs`, `activities` | Repository Settings | TC-REPO-001 |
| FR-05 | Git UI | `/api/v1/projects/{id}/git/status`, stage, unstage, commit, push, pull | `repositories`, `jobs`, `activities` | Git UI | TC-GIT-001 |
| FR-06 | Commit History | `/api/v1/projects/{id}/git/commits` | `repositories` | Commit History | TC-GIT-002 |
| FR-07 | Branch Management | `/api/v1/projects/{id}/git/branches`, checkout, merge | `repositories`, `activities` | Git UI, Commit History | TC-GIT-003 |
| FR-08 | Parser | `POST /api/v1/projects/{id}/parse` | `jobs`, `scenes`, `scene_nodes`, `assets`, `scripts`, `resources`, `dependencies` | Jobs, Scene Explorer, Asset Explorer | TC-PARSE-001 |
| FR-09 | Scene Explorer | `/api/v1/projects/{id}/scenes` | `scenes`, `scene_nodes`, `scripts`, `dependencies` | Scene Explorer | TC-SCENE-001 |
| FR-10 | Asset Explorer | `/api/v1/projects/{id}/assets` | `assets`, `dependencies`, `health_issues` | Asset Explorer | TC-ASSET-001 |
| FR-11 | Dependency Graph | `GET /api/v1/projects/{id}/dependencies` | `dependencies`, metadata tables, `health_issues` | Dependency Graph | TC-GRAPH-001 |
| FR-12 | Project Health | `POST /api/v1/projects/{id}/analyze`, health endpoints | `health_reports`, `health_issues`, `jobs` | Health Report, Dashboard | TC-HEALTH-001 |
| FR-13 | Scene Diff | `POST /api/v1/projects/{id}/diff/scene`, `GET /diff/{diffId}` | `jobs`, `scenes`, MinIO `diff-artifacts` | Scene Diff Viewer | TC-DIFF-001 |
| FR-14 | Dashboard | `GET /api/v1/projects/{id}/dashboard` | `projects`, `repositories`, `jobs`, `activities`, health/metadata tables, Redis | Dashboard | TC-DASH-001 |
| FR-15 | Search | `GET /api/v1/search` | `projects`, `project_members`, metadata tables | Global Search | TC-SEARCH-001 |
| FR-16 | Notification | `/api/v1/notifications` | `notifications`, `jobs`, `project_members`, settings | Notification Center | TC-NOTIF-001 |
| FR-17 | Settings | project/user settings endpoints | `project_settings`, `user_settings`, `activities` | Project Settings, User Menu | TC-SETTINGS-001 |
| FR-17.1 | Project Settings | `GET/PUT /api/v1/projects/{id}/settings` | `project_settings`, `activities` | Project Settings | TC-SETTINGS-001 |
| FR-17.2 | User Settings | `GET/PUT /api/v1/users/me/settings` | `user_settings` | User Menu | TC-SETTINGS-002 |
| FR-18 | Activity Log | `/api/v1/projects/{id}/activities`, `/api/v1/activities` | `activities` | Activity Log, Admin Activity | TC-ACTIVITY-001 |
| NFR-01..NFR-11 | Performance | API/worker metrics | Redis, PostgreSQL, RabbitMQ metrics | Dashboard, Jobs | TC-PERF-001 |
| NFR-19..NFR-25 | Reliability | Job APIs, worker queues | `jobs`, RabbitMQ DLQ | Jobs, Operations | TC-RELIABILITY-001 |
| NFR-23 | Worker Retry/DLQ | Job status APIs, worker queue consumers | `jobs.status`, RabbitMQ DLQ | Jobs, Operations | TC-JOB-001 |
| NFR-41..NFR-46 | Security | Auth, project-scoped APIs | `users`, `refresh_tokens`, `activities` | All protected screens | TC-SEC-001 |

## Maintenance Rules

- New requirements must have at least one API or workflow if they are user-facing.
- Data-related requirements must update tables/schemas in `04-database.md`.
- UI-related requirements must have screens or states in `09-ui-ux.md`.
- `Must` requirements must have high-priority test cases in `11-testing-acceptance.md`.
