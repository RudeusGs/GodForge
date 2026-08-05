# 7. Non-functional Requirements

## Performance

| ID | Target |
|---|---|
| NFR-01 | Normal authenticated CRUD/read endpoints: p95 <= 500 ms under baseline load, excluding external providers. |
| NFR-02 | Dashboard cached read: p95 <= 500 ms; uncached aggregate p95 <= 1.5 s. |
| NFR-03 | Scene/asset paged list: p95 <= 1 s for medium dataset. |
| NFR-04 | Every list is paginated; default 20, maximum 100 unless stricter. |
| NFR-05 | Graph response is bounded by depth/node/edge limits and supports filtered subgraphs. |
| NFR-06 | No avoidable N+1 query in high-volume endpoints; query count is measured in integration/performance tests. |
| NFR-07 | Full analysis performance is measured by repository tier; no fixed claim is accepted without benchmark evidence. |
| NFR-08 | Eligible incremental analysis should reduce processed files and median duration by at least 50% on the evaluation corpus while preserving equivalent results. |

## Capacity and resource limits

- Configurable repository byte, file count, text file, path depth and command timeout limits.
- Worker concurrency limits by job type.
- AI input/output token and request budgets.
- Asset size, organization storage and signed-URL TTL quotas.

## Reliability

| ID | Requirement |
|---|---|
| NFR-20 | Business/job state survives API, Worker and RabbitMQ restart. |
| NFR-21 | Duplicate webhook/message does not duplicate revision, report, asset version or notification. |
| NFR-22 | Transient failures use bounded exponential retry with jitter. |
| NFR-23 | Poison/retry-exhausted messages are visible in DLQ and linked to durable job. |
| NFR-24 | Cancellation and timeout release locks and temporary resources. |
| NFR-25 | AI/provider failure degrades only dependent optional stage. |
| NFR-26 | Backup and restore are tested and measured against defined RPO/RTO. |

## Availability and recovery targets

Graduation deployment target (subject to hosting constraints):

- API monthly availability target: 99.5%, excluding announced maintenance.
- RPO: <= 24 hours for thesis environment; production aspiration <= 1 hour where supported.
- RTO: <= 4 hours for thesis environment after infrastructure is available.

These are targets, not claims, until monitoring and restore drills provide evidence.

## Maintainability

- Clean Architecture dependency rules.
- Feature-slice use cases and typed contracts.
- Versioned parser, rule set, profile, prompt and message schemas.
- Documentation and traceability updated with behavior changes.
- No file/class size metric is a hard quality proxy; complexity and responsibility are reviewed.

## Compatibility

- Supported browser versions are documented at release.
- Supported Godot versions are defined by validation/parser profiles.
- API version changes preserve compatibility or introduce a new version.
- Database migrations support upgrade from the last released version.

## Accessibility and usability

- Keyboard-accessible main workflows.
- Non-color severity indicators.
- Clear degraded/partial/stale status.
- Large graph/tree views offer search, filtering and progressive loading.

## Privacy and cost

- Minimize retained source excerpts and AI context.
- Track AI token usage and asset/storage growth by organization/project.
- Organization policy can disable AI and configure retention within platform minimums.
