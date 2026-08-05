# Dashboard

## Purpose

Provide project and organization summaries without replacing detailed source views.

## Actors

Viewer and higher roles; organization summary limited by organization permission.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-14.1 | Project health, repository, revision and job summary | Must |
| FR-14.2 | Trend of score/findings across revisions | Must |
| FR-14.3 | Asset/storage and team activity summary | Should |
| FR-14.4 | Organization portfolio summary | Should |

## Main flow

Dashboard aggregates latest revision, health categories, open critical/high findings, recent commits/jobs/activity, analysis freshness and degraded provider state.

## Error and edge cases

- No completed analysis.
- Stale cache or currently running job.
- AI unavailable.
- Partial access to organization projects.

## Authorization and security

- Aggregates are computed only from authorized projects.
- Cache keys include tenant/project scope and permission-safe dimensions.
- Do not expose private asset names/counts to unauthorized viewers.

## Async processing and idempotency

- No module-specific durable job is created by read operations.
- Writes, where present, use normal transaction/concurrency rules and do not perform hidden heavy work.

## Acceptance criteria

- `AC-FR-14-01`: Empty/new project has clear setup state.
- `AC-FR-14-02`: Cache invalidates after relevant terminal events.
- `AC-FR-14-03`: Dashboard remains usable when AI is degraded.

## Related API

- Project and organization dashboard endpoints

## Related data

- Read models over project/repository/analysis/job/activity/storage tables; Redis cache

## Tests and observability

- Test suite: `TC-DASH-*`, including empty, stale, degraded and cross-tenant states.
- Metrics: dashboard latency, cache hit ratio, stale age and aggregation failures.
- Query-count and cache-key tests prevent N+1 and tenant leakage.
