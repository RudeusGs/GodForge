# 11. Testing & Acceptance

## Test Strategy

GodForge applies a risk-based testing strategy: security flows, RBAC, Git operations, parser/analyzer workers, and data integrity are prioritized over secondary UI features.

## Test Levels

| Type | Scope | Examples |
| --- | --- | --- |
| Unit test | Domain logic, validators, permission checkers, pure parser/analyzer rules. | Password policy, project name validation, health score formula. |
| Integration test | API + database, Redis locks, RabbitMQ messages, MinIO artifacts, mocked Git repositories. | Connect repository creates job, parse writes metadata, lock Git operation. |
| API test | `/api/v1` contracts, responses/errors/pagination/job formats. | Login error format, project pagination, async job responses. |
| Worker test | Parser, Analyzer, Diff, Preview worker lifecycles. | Retry transient DB error, DLQ poison messages, idempotent parsing. |
| Security test | Auth, RBAC, input validation, secret leakage, path traversal, SSRF. | Cross-project IDOR, malicious repo URLs, `../` paths. |
| Performance test | Dashboard, search, parse/analyze, scene diff, queue lag. | Dashboard P95, parsing 100 scenes, medium scene diff. |
| E2E test | Primary workflows via UI/API. | Create project -> connect repo -> analyze -> view dashboard. |
| UAT | Role-based acceptance. | Reviewer views diff; Developer commits/pushes; Viewer is readonly. |

## Standard Test Case Format

| Field | Description |
| --- | --- |
| ID | Test case code, e.g., `TC-AUTH-001`. |
| Requirement | The requirement ID being tested. |
| Preconditions | Necessary data/state before testing. |
| Steps | Execution steps. |
| Expected result | Expected outcome. |
| Priority | High/Medium/Low. |

## Minimum Test Cases

| ID | Requirement | Preconditions | Steps | Expected result | Priority |
| --- | --- | --- | --- | --- | --- |
| TC-AUTH-001 | FR-01 | Active user exists. | Proper login, refresh token, logout. | Receives tokens, refresh rotation works, logout revokes tokens. | High |
| TC-AUTH-002 | FR-02 | Project has owner/admin. | Invite member, change role, attempt to remove last owner. | Role updates correctly; last owner is not removed. | High |
| TC-PROJ-001 | FR-03 | Authenticated user. | Create/update/delete/restore project. | CRUD respects permissions, soft-delete and restore work correctly. | High |
| TC-PROJ-MEMBER-001 | FR-03.1 | Project has owner/admin and an invited user. | List/add/update/remove member, try removing last owner. | Membership/RBAC correct; last owner retained; activity logged. | High |
| TC-REPO-001 | FR-04 | Project without repo. | Connect valid repository URL/PAT. | Credential encrypted, clone job created, activity logged. | High |
| TC-GIT-001 | FR-05 | Ready repo with changed files. | Status, stage, commit, push. | Correct Git state, lock functions, activity logged. | High |
| TC-GIT-002 | FR-06 | Repo with commit history. | View list/detail/filter. | Correct commit list/detail, correct pagination. | Medium |
| TC-GIT-003 | FR-07 | Ready repo. | Create branch, checkout dirty tree, merge. | Branch created, dirty checkout blocked, merge respects permissions. | Medium |
| TC-PARSE-001 | FR-08 | Godot fixture repo. | Trigger parse. | Metadata written correctly, corrupt files do not crash job. | High |
| TC-SCENE-001 | FR-09 | Parsed scene metadata. | Open scene, click node, search. | Correct tree/detail/search. | Medium |
| TC-ASSET-001 | FR-10 | Parsed asset metadata. | Open asset, view usage. | Correct detail/usage/warnings. | Medium |
| TC-GRAPH-001 | FR-11 | Completed analysis. | Open graph, filter, focus node. | Correct nodes/edges/filters/cycles. | Medium |
| TC-HEALTH-001 | FR-12 | Metadata with missing resource fixture. | Trigger analyze. | Correct report score/issues, critical notification generated. | High |
| TC-DIFF-001 | FR-13 | Two different scene revisions. | Generate scene diff. | Correct node/property diff, fast cache hit. | High |
| TC-DASH-001 | FR-14 | Project with metadata/jobs/activity. | Open dashboard. | Correct summary, cache hits target. | Medium |
| TC-SEARCH-001 | FR-15 | User has permission for project A, not B. | Search keyword present in both. | Only returns project A results. | High |
| TC-NOTIF-001 | FR-16 | Completed job. | View notification, mark read. | Realtime/persisted notification, unread count decreases. | Medium |
| TC-SETTINGS-001 | FR-17 | Project Admin and Developer. | Admin changes setting; Developer tries. | Admin succeeds, Developer receives 403. | Medium |
| TC-SETTINGS-002 | FR-17.2 | Authenticated user. | View and change user settings. | Settings correctly saved per user, independent of others. | Medium |
| TC-ACTIVITY-001 | FR-18 | Project with activity. | View activity by project and system admin view. | Project member sees appropriate scope; system admin sees global view. | High |
| TC-JOB-001 | NFR-23 | Test queue with transient error, poison message, and timeout fixture. | Trigger job, simulate retry, timeout, retry exhaustion, and DLQ. | Status transitions correctly (`queued`/`running`/`retrying`/`timeout`/`dead_lettered`); DLQ retains sanitized payload. | High |
| TC-JOB-002 | FR-19 | Project member. | Request job list for project. | Only returns jobs for that project with correct pagination. | High |
| TC-JOB-003 | FR-19 | User requests jobs for unauthorized project. | Attempt to list or view job detail. | Returns 403 or 404, no job data exposed. | High |
| TC-JOB-004 | FR-19 | Developer/Project Admin. | Request cancellation of cancellable queued/running job. | Job marked for cancellation, returns success. | High |
| TC-JOB-005 | FR-19 | Terminal job exists. | Request cancellation of terminal job. | Returns `JOB_NOT_CANCELLABLE`. | Medium |
| TC-JOB-006 | FR-19 | Failed job exists. | View job detail. | Detail includes status, progress, timestamps, safe error code, `correlationId`, no secrets. | High |
| TC-JOB-007 | FR-19 | Active SignalR connection. | Monitor running job. | SignalR updates match REST DB state; REST remains source of truth. | Medium |
| TC-SEC-001 | Security NFR | User with multiple projects. | Attempt cross-project IDOR, path traversal, malicious repo URL. | Requests rejected/forbidden, no data leaked. | High |
| TC-PERF-001 | Performance NFR | Representative dataset. | Measure dashboard/search/parse/diff. | Meets NFR targets or clearly notes deviations. | Medium |

## UAT Checklist

- System Admin manages users and views system activity.
- Project Admin creates projects, invites members, connects repositories, and configures settings.
- Developer performs Git operations, triggers parse/analyze, and views metadata.
- Reviewer/QA views commit history, scene diffs, and health reports.
- Viewer accesses readonly dashboards, scenes, assets, graphs, and permitted activities.
- Notifications and activity logs accurately reflect primary workflows.

## Definition of Done

- `Must` requirements have implementations and corresponding tests, unless an approved exception exists.
- API responses/error formats match `05-api.md`.
- RBAC is tested with at least Viewer, Developer, Project Admin, and System Admin roles.
- No secrets exist in logs, plaintext DBs, or API responses.
- Worker jobs have retry/DLQ/idempotency handling as per `12-worker-processing.md`.
- SRS docs, traceability, and testing documents are updated when requirements change.

## Sign-off Criteria

- No unhandled critical/high bugs remain.
- Key security tests pass.
- Primary performance targets are met or deviations are formally accepted.
- UAT checklist is confirmed by appropriate stakeholders.
- Release checklist covering operations, backup/restore, and monitoring is complete.
