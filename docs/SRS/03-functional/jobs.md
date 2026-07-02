# Jobs (Monitoring & Cancellation)

## Purpose

The Jobs module provides user-facing APIs for monitoring asynchronous background tasks (such as repository clone, parse, analyze, diff, and asset preview) and canceling them when appropriate. This document focuses on the user-facing REST APIs and SignalR updates. For detailed backend worker internals (queues, retries, idempotency, dead-letter queues), see `12-worker-processing.md`.

## Actors

- Developer
- Project Admin
- Viewer (project member)
- Worker / System

## Scope

### In Scope
- List jobs associated with a project.
- View job detail, status, and progress.
- Cancel cancellable jobs (e.g., jobs in `queued` or `running` states, if supported by the job type).
- Receive realtime job progress updates via SignalR.
- View terminal states (`completed`, `failed`, `timeout`, `cancelled`, `dead_lettered`) and safe user-facing error codes.

### Out of Scope
- Manual DLQ (Dead-Letter Queue) inspection or requeuing from user-facing APIs (unless explicitly approved for system admins).
- Editing or mutating job payloads.
- Exposing internal worker exception details, stack traces, or internal server paths.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-19 | Job Monitoring and Cancellation | Users can list, view, and cancel asynchronous jobs for their projects. | Must | Developer, Project Admin, Viewer |
| BR-01 | PostgreSQL Source of Truth | The REST API reads job state directly from PostgreSQL, not from RabbitMQ. | Must | System |
| BR-02 | Project Scoped Visibility | Jobs are strictly scoped to a project. Users can only see jobs for projects they are members of. | Must | System |
| BR-03 | Cancellation Permission | Only Developer and Project Admin roles can cancel jobs. | Must | Developer, Project Admin |
| BR-04 | Terminal State Protection | Jobs in a terminal state (`completed`, `failed`, `cancelled`, `timeout`, `dead_lettered`) cannot be cancelled. | Must | System |
| BR-05 | Safe Error Exposure | Failed jobs expose a `correlationId` and a safe `error_code`, but never internal secrets or stack traces. | Must | System |

## Main Workflow

### List and View Jobs
1. The user requests the job list or job detail via the REST API.
2. The backend queries the `ops.jobs` table (PostgreSQL) and applies project-level RBAC.
3. The API returns the job details, including `status`, `progress`, `type`, timestamps, and any `error_code`.

### Realtime Updates
1. As the backend worker processes a job, it updates `ops.jobs` in PostgreSQL and emits a `JobProgressUpdate` event via SignalR.
2. Connected clients receive the progress update.
3. The REST API remains the authoritative source of truth if the SignalR connection drops or is missed.

### Cancel a Job
1. A Developer or Project Admin sends a request to cancel a specific job.
2. The backend verifies the job is not in a terminal state and the user has permission.
3. The backend marks the job with cancellation intent (e.g., `cancellation_requested_at`).
4. The background worker cooperatively checks for cancellation tokens. If possible, it stops processing, cleans up, and transitions the job to `cancelled`.

## Exceptions / Errors

| Situation | HTTP Status | Error Code | Behavior |
| --- | --- | --- | --- |
| Job not found in project | 404 | `JOB_NOT_FOUND` | Job does not exist or user lacks access. |
| Project not found/access denied | 403 / 404 | `PROJECT_NOT_FOUND` / `FORBIDDEN` | Deny access to the project's jobs. |
| Job already terminal | 400 | `JOB_NOT_CANCELLABLE` | Deny cancellation request. |
| User lacks cancellation permission| 403 | `FORBIDDEN` | Deny cancellation request (Viewer role). |

## Acceptance Criteria

- AC-01 (TC-JOB-002): A project member can list visible jobs for their project with correct pagination.
- AC-02 (TC-JOB-003): A user cannot view or access jobs belonging to a project they are not a member of.
- AC-03 (TC-JOB-004): A Developer or Project Admin can successfully request cancellation of a cancellable `queued` or `running` job.
- AC-04 (TC-JOB-005): Attempting to cancel a job that is already in a terminal state returns `JOB_NOT_CANCELLABLE`.
- AC-05 (TC-JOB-006): Job detail responses include status, progress, type, timestamps, safe error code (if failed), and a `correlationId`.
- AC-06 (TC-JOB-007): SignalR progress updates are supplemental; the REST job endpoint accurately reflects the durable job state in the database.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/jobs` | Project member | type, status, page | Job list | `FORBIDDEN` |
| GET | `/api/v1/projects/{projectId}/jobs/{jobId}` | Project member | job id | Job detail/progress | `JOB_NOT_FOUND` |
| POST | `/api/v1/projects/{projectId}/jobs/{jobId}/cancel` | `developer+` | none | Job cancelled | `JOB_NOT_CANCELLABLE`, `FORBIDDEN` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `ops.jobs` | The canonical source of truth for job state, progress, ownership, error codes, and lifecycle timestamps. |

## Related Documentation

- `05-api.md`: Standard REST API responses and async job payloads.
- `06-security.md`: RBAC and permission models.
- `10-traceability.md`: Traceability matrices mapping requirements to tests.
- `11-testing-acceptance.md`: Test cases.
- `12-worker-processing.md`: Worker queue topology, retries, idempotency, DLQ, timeouts, and internal worker lifecycles.
