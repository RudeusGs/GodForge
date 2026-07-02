# Analyzer / Project Health

## Purpose

The Analyzer module evaluates parsed Godot project metadata and produces project health reports, health issues, severity summaries, scores, recommendations, and dashboard-ready metrics.

The Analyzer must never modify repository content, Godot files, scripts, scenes, resources, or assets. It is a read-only analysis module that derives results from a specific metadata version and commit hash.

## Actors

| Actor | Responsibility |
| --- | --- |
| Developer | Triggers analysis and reviews issues that affect implementation quality. |
| Project Admin | Configures project analysis options and reviews project-level health. |
| Reviewer / QA | Reviews health issues, dependency risks, and report history. |
| Worker / System | Executes analyze jobs, persists reports, invalidates cache, and publishes events. |
| Viewer | Reads health reports and dashboard summaries when authorized. |

## Scope

The Analyzer module includes:

- Running analysis after parse completion or after an authorized user request.
- Reading parsed metadata for scenes, nodes, assets, scripts, resources, dependencies, metadata versions, and parser warnings.
- Building or updating dependency summaries required by health checks and dashboards.
- Running a versioned health rule set.
- Calculating a deterministic health score from issue severity and rule weights.
- Persisting immutable health report history.
- Persisting health issues linked to the report, metadata version, project, and entity reference.
- Updating dashboard/project summary cache after durable report persistence.
- Publishing notification/activity events for critical results.
- Rejecting or ignoring stale analysis results when a newer metadata version or newer completed report already exists.

Out of scope:

- Editing or automatically fixing repository files.
- Executing scripts or scene logic.
- Running heavy graph analysis inside a long HTTP request.
- Returning server paths, raw stack traces, or unfiltered repository structure to unauthorized users.

## Functional Requirements

| ID | Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-12 | Project Health Analyzer | Analyze Godot project health, calculate score, and generate issue/recommendation data. | Must | Developer, Worker |
| BR-54 | Async analysis | Analyze work that can exceed API latency targets must run through RabbitMQ worker processing. | Must | System |
| BR-55 | Report history | Health reports are immutable history records and must not overwrite previous reports. | Must | Worker |
| BR-56 | Health visibility | Latest health score and severity summary must be visible on dashboard and project detail for authorized users. | Must | Viewer+ |
| BR-57 | Parse prerequisite | Analyze requires a completed parse/metadata version for the target commit. | Must | Worker |
| BR-58 | Commit-bound analysis | Every analysis result must be tied to a specific commit hash and metadata version. | Must | Worker |
| BR-59 | Versioned rules | Every report must store `rule_set_version` and `metadata_schema_version`. | Must | Worker |
| BR-60 | Stale result protection | Older jobs must not overwrite newer reports, dashboard cache, or project summary fields. | Must | Worker |
| BR-61 | Permission-filtered access | Health report and issue APIs must enforce project-scoped RBAC server-side. | Must | API |

## Main Workflow

1. A user, parser completion event, repository sync completion, or future schedule requests analysis.
2. The API validates JWT, project membership, project-level RBAC, repository readiness, parse availability, and requested branch/commit/options.
3. For normal analysis, the API creates a `jobs` record with status `queued`, stores the correlation id, publishes an `ANALYZE_PROJECT` message, and returns `202 Accepted` with `jobId`.
4. The Analyzer Worker consumes the message and verifies message schema version, idempotency key, job state, metadata version, commit hash, and stale-message conditions.
5. The worker marks the job `running`, records heartbeat/progress, and loads parsed metadata from PostgreSQL.
6. The Analyzer runs versioned rules such as missing resource, missing script, cyclic dependency, unused asset, oversized scene, deep nesting, duplicate resource, missing import, unresolved dependency, and parser warning escalation.
7. The Analyzer calculates score, severity counts, recommendations, and dashboard summary.
8. The worker persists `health_reports` and `health_issues` in a transaction or equivalent durable write sequence.
9. Only after durable output is committed, the worker updates latest project health summary/cache, publishes `HealthReportPublished`, writes activity, and sends notification for critical issues.
10. The frontend receives progress through SignalR or polls the job endpoint, then loads the latest health report or report issues through permission-filtered APIs.

## API Boundary

The API must only orchestrate and return lightweight responses.

- `POST /api/v1/projects/{projectId}/analyze` must not run heavy analysis inline.
- It must return `202 Accepted` with `data.jobId`, `data.status`, and `correlationId` when a job is queued.
- It may return an existing queued/running/completed job when the same idempotency key is requested again.
- It must reject requests without completed parse metadata for the target commit.
- It must never expose internal repository workspace paths or raw analyzer exceptions.

## Worker Boundary

The Analyzer Worker is responsible for long-running computation and persistence of analysis output.

- PostgreSQL is the source of truth for job state and health report history.
- Redis may be used for cache, locks, and short-lived summaries, but not as primary report storage.
- The worker must be idempotent by `job_id`, `project_id`, `commit_hash`, `metadata_version_id`, `rule_set_version`, and analysis options hash.
- The worker must not process stale jobs that target older metadata when a newer completed report already exists for the same project and newer commit/metadata lineage.
- The worker must heartbeat while running and must transition to `timeout`, `retrying`, `failed`, or `dead_lettered` according to worker policy.

## Health Score Formula

Default score calculation:

```text
Health Score = 100 - SUM(penalty_per_issue)

Critical: -10 points per issue, cap -50
Warning:  -3 points per issue, cap -30
Info:     -1 point per issue, cap -10

Minimum score: 0
Maximum score: 100
```

Rules:

- The final score must be clamped to 0-100.
- Penalties and caps belong to the health rule set and must be versioned.
- Reports must store enough summary data to explain the score later, even after rule weights change.
- Score calculation must be deterministic for the same metadata version and rule set version.
- Critical issues must be highlighted separately from the numeric score because a project can still have a non-zero score while containing merge-blocking risks.

## Minimum Health Rules

| Rule Code | Issue Type | Severity | Description | Source Data |
| --- | --- | --- | --- | --- |
| `HEALTH_MISSING_RESOURCE` | `missing_resource` | Critical | Scene or resource references a file that does not exist in the target metadata version. | `dependencies`, `resources`, `assets` |
| `HEALTH_MISSING_SCRIPT` | `missing_script` | Critical | A node has a script reference but the script file does not exist or failed to parse as required. | `scene_nodes`, `scripts`, `dependencies` |
| `HEALTH_CYCLIC_DEPENDENCY` | `cyclic_dependency` | Warning | A dependency cycle exists between scene/resource/script/asset entities. | `dependencies` |
| `HEALTH_UNUSED_ASSET` | `unused_asset` | Info | An asset is present but not referenced by dependency graph or project metadata. | `assets`, `dependencies` |
| `HEALTH_OVERSIZED_SCENE` | `oversized_scene` | Warning | A scene exceeds the configured node count threshold. Default threshold: 500 nodes. | `scenes`, `scene_nodes` |
| `HEALTH_DEEP_NESTING` | `deep_nesting` | Warning | A scene node tree exceeds the configured depth threshold. Default threshold: 10 levels. | `scene_nodes` |
| `HEALTH_DUPLICATE_RESOURCE` | `duplicate_resource` | Info | Multiple resources/assets share the same content hash or equivalent metadata signature. | `resources`, `assets` |
| `HEALTH_MISSING_IMPORT` | `missing_import` | Warning | Godot import metadata points to a missing source file or inconsistent import state. | `assets`, import metadata |
| `HEALTH_UNRESOLVED_DEPENDENCY` | `unresolved_dependency` | Warning | Dependency target cannot be resolved but does not meet critical missing-resource criteria. | `dependencies` |
| `HEALTH_PARSER_WARNING` | `parser_warning` | Info | Parser produced non-fatal warnings that may affect explorer, graph, or health accuracy. | parser warnings / metadata status |

## Rule Execution Requirements

- Health rules must be pure analysis rules over metadata and configuration.
- Rules must not perform file mutation, Git operations, network calls, or script execution.
- One rule failure must not corrupt a completed report. A rule implementation failure should be recorded as an analyzer error and handled according to severity/configuration.
- Rule configuration errors are not transient and must not retry indefinitely.
- Every issue must include rule code, severity, entity reference, path or logical location when permitted, message, suggestion, and technical details safe for authorized users.

## Job Message Contract

`ANALYZE_PROJECT` messages should contain at least:

| Field | Requirement |
| --- | --- |
| `eventSchemaVersion` | Version of message payload contract. |
| `messageId` | Unique message id for deduplication. |
| `jobId` | Job record id in PostgreSQL. |
| `projectId` | Project scope. |
| `repositoryId` | Repository scope when applicable. |
| `metadataVersionId` | Parsed metadata version to analyze. |
| `commitHash` | Commit hash that produced the metadata. |
| `actorId` | User who requested the job, nullable for system-triggered jobs. |
| `correlationId` | Trace id propagated from API/event/worker. |
| `ruleSetVersion` | Health rule set version to run. |
| `analysisOptionsHash` | Hash of options used for idempotency. |
| `attemptCount` | Delivery/retry attempt count. |
| `createdAt` | Message creation time. |

## Error Handling

| Situation | HTTP Status / Job State | Error Code | Behavior |
| --- | --- | --- | --- |
| Metadata has not been parsed | `400 Bad Request` | `PARSE_REQUIRED` | Reject analyze request and tell user to run parse first. |
| Repository not ready | `409 Conflict` | `REPO_NOT_READY` | Reject request until repository is connected/cloned/synced. |
| User lacks permission | `403 Forbidden` | `FORBIDDEN` | Do not reveal project metadata or issue paths. |
| Latest health report not available | `404 Not Found` or readiness response | `HEALTH_NOT_READY` | Return actionable state, not an empty fake report. |
| Metadata version is stale | Job ignored or failed non-retryable | `STALE_METADATA_VERSION` | Do not overwrite newer report/cache. Write activity/log. |
| Rule config invalid | Job failed / DLQ if poison config | `ANALYZE_RULE_CONFIG_INVALID` | Do not retry indefinitely. Log sanitized details. |
| Graph too large for configured limits | Job failed or partial result if supported | `ANALYZE_GRAPH_TOO_LARGE` | Return clear message and keep previous report if any. |
| PostgreSQL/Redis/MinIO transient failure | Job retrying | `JOB_TRANSIENT_FAILURE` | Retry with backoff according to worker policy. |
| Worker timeout | Job timeout or retrying | `JOB_TIMEOUT` | Stop safely, persist timeout state, and avoid partial report publication. |
| Unexpected analyzer bug | Job failed | `ANALYZE_INTERNAL_ERROR` | Log with correlation id; return sanitized message to client. |

## API Endpoints

| Method | Path | Permission | Request | Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| `POST` | `/api/v1/projects/{projectId}/analyze` | `developer+` | `branch`, `commitHash`, `ruleSetVersion`, `options` | `202` with analyze job | `PARSE_REQUIRED`, `REPO_NOT_READY`, `FORBIDDEN` |
| `GET` | `/api/v1/projects/{projectId}/health` | `viewer+` | none | Latest health report summary | `HEALTH_NOT_READY`, `FORBIDDEN` |
| `GET` | `/api/v1/projects/{projectId}/health/history` | `viewer+` | pagination, branch/commit filters | Health report history | `FORBIDDEN` |
| `GET` | `/api/v1/projects/{projectId}/health/{reportId}` | `viewer+` | report id | Full report summary | `REPORT_NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/api/v1/projects/{projectId}/health/{reportId}/issues` | `viewer+` | severity, rule code, entity type, pagination | Health issues | `REPORT_NOT_FOUND`, `FORBIDDEN` |
| `PATCH` | `/api/v1/projects/{projectId}/health/issues/{issueId}` | `developer+` or configured role | status/comment | Issue acknowledgement/ignore state | `ISSUE_NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/api/v1/projects/{projectId}/jobs/{jobId}` | project member | job id | Job status and progress | `JOB_NOT_FOUND`, `FORBIDDEN` |

## Response Shape

Success responses must follow the common API response format:

```json
{
  "data": {
    "id": "report-guid",
    "projectId": "project-guid",
    "metadataVersionId": "metadata-version-guid",
    "commitHash": "abc123",
    "score": 82,
    "severitySummary": {
      "critical": 1,
      "warning": 5,
      "info": 8
    },
    "ruleSetVersion": "health-rules-1.0.0",
    "generatedAt": "2026-07-01T00:00:00Z"
  },
  "correlationId": "request-correlation-id"
}
```

Paginated issue/history endpoints must include `meta` with page, pageSize, totalItems, and totalPages.

## Database Tables

| Table | Role |
| --- | --- |
| `jobs` | Analyze job lifecycle, progress, heartbeat, error code, retry count, and correlation id. |
| `metadata_versions` | Commit-bound metadata lineage and schema version. |
| `scenes`, `scene_nodes` | Source data for scene size, nesting, script attachment, and scene-level rules. |
| `assets`, `scripts`, `resources`, `dependencies` | Source data for missing, unused, duplicate, and cycle rules. |
| `statistics` | Optional aggregate metrics generated during analysis for dashboard/search. |
| `health_reports` | Immutable report summary, score, severity counts, metadata version, commit hash, and rule set version. |
| `health_issues` | Individual issue records with rule code, severity, entity reference, path, message, suggestion, status, and details. |
| `projects` | Optional denormalized latest health score/report id for fast project summaries. |
| `notifications` | Notifications generated from critical or configured health events. |
| `activities` / `audit_logs` | Business timeline and audit trail for analyze requested/completed/failed and issue status updates. |

## Data Integrity Rules

- `health_reports` must be append-only for report history.
- Each report must reference exactly one project, metadata version, commit hash, and rule set version.
- `health_issues` must reference a health report and should reference an entity through a safe entity reference model.
- Dashboard cache and project latest health fields may only be updated after the report and issues are committed.
- Stale jobs must not update latest health summary fields.
- Indexes should support project-scoped queries by `project_id`, `metadata_version_id`, `commit_hash`, `generated_at`, `severity`, `rule_code`, and issue status.

## Events and Notifications

| Event | Producer | Consumers | Notes |
| --- | --- | --- | --- |
| `AnalyzeRequested` | Project/API service | RabbitMQ / Analyze Worker / Activity | Created after API validates permission and job record is queued. |
| `AnalyzeStarted` | Analyze Worker | Activity / Monitoring / SignalR | Emitted after job transitions to running. |
| `HealthReportPublished` | Analyze Worker | Dashboard / Notification / Activity | Emitted only after report output is durable. |
| `AnalyzeFailed` | Analyze Worker | Notification / Activity / Monitoring | Include sanitized error code and correlation id. |
| `CriticalHealthIssueDetected` | Analyze Worker or Notification service | Notification / Dashboard | Deduplicate notifications for repeated equivalent issues. |

## Security and Authorization

- Health data can reveal repository structure, file names, paths, missing assets, scripts, and technical debt. Every query must enforce project-scoped RBAC server-side.
- Viewer access is read-only and must be filtered to projects the actor can access.
- Analyzer must not execute repository scripts or resource code.
- API and worker logs must not contain secrets, Git credentials, raw repository tokens, stack traces returned to clients, or internal workspace paths.
- Error responses must include `correlationId` and must not include production stack traces.
- Issue details should be safe for authorized project members but still avoid exposing absolute server paths.

## Observability

The Analyzer must emit structured logs and metrics with `correlationId`, `jobId`, `projectId`, `metadataVersionId`, `commitHash`, `ruleSetVersion`, and sanitized error code when applicable.

Minimum metrics:

- Analyze job duration.
- Analyze success/failure/timeout count.
- Rule execution duration by rule code.
- Issue count by severity and rule code.
- Queue depth and retry count.
- Stale job ignored count.
- Dashboard cache invalidation count.

Minimum alerts:

- Analyze failure rate above threshold.
- Analyze job timeout spike.
- Queue backlog high.
- DLQ count above zero for analyze queue.
- Stale job count spike after deployments or repository sync events.

## Acceptance Criteria

- AC-70: Running analyze for parsed metadata creates a health report with score between 0 and 100.
- AC-71: A project with two missing resources creates two critical issues and reduces the score by 20 points under the default rule set.
- AC-72: Critical health issues generate notifications for relevant authorized users after the report is committed.
- AC-73: Health history keeps previous reports and supports trend review by report time, metadata version, commit hash, and rule set version.
- AC-74: Analyze without completed parse metadata returns a clear `PARSE_REQUIRED` error.
- AC-75: A stale analyze job cannot overwrite a newer health report, project summary, or dashboard cache.
- AC-76: Health issue APIs do not return data for projects the actor cannot access.
- AC-77: Analyzer logs and API errors include correlation id and do not expose secrets, raw stack traces, or internal workspace paths.
- AC-78: Rule set version is stored on every health report and can explain historical score calculation.
- AC-79: Analyze worker retry applies only to transient failures and does not retry non-transient rule configuration errors indefinitely.