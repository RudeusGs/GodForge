# 15. Observability

## Structured logs

Required safe fields where applicable:

- service, environment and version.
- correlation ID, trace ID, job ID, message ID.
- organization/project/repository IDs.
- actor ID when authorized/safe.
- job type/stage, attempt and duration.
- stable error code.

Never log secrets, tokens, credentials, full private source, raw AI prompts, signed URLs or internal paths returned to clients.

## Metrics

### API

- request count, duration and error by route/status.
- authentication/rate-limit outcomes.
- database query duration and pool health.

### Worker

- started/completed/failed/retried jobs by type.
- duration by stage.
- queue lag, active jobs, stale heartbeat and DLQ count.
- workspace bytes and cleanup failure.

### Providers

- Forgejo/Git latency and errors.
- MinIO operations and capacity.
- Gemini latency, errors, input/output tokens and quota.
- SMTP delivery outcomes.

### Product

- analysis completion/degraded rate.
- full versus incremental duration and fallback reason.
- health finding counts by severity/category.
- asset storage/download and report exports.

## Tracing

OpenTelemetry spans cover API command/query, database, outbox publish, queue consume, Git operation, parser stages, storage and provider calls. Sensitive payloads are not span attributes.

## Alerts

- API/Worker readiness failure.
- sustained 5xx or latency threshold.
- oldest outbox/queue message above threshold.
- DLQ growth.
- stale running jobs/locks.
- database or object storage capacity threshold.
- backup/restore failure.
- webhook signature failures spike.
- AI cost/error threshold.

## Dashboards

At minimum: service health, API, worker/queue, database, storage, Forgejo, AI and product analysis dashboard.
