# ADR 0003: Durable asynchronous job processing

## Status
Accepted

## Context
Clone, fetch, validation, parsing, graph building, health evaluation, AI calls, previews and report generation can exceed HTTP budgets.

## Decision
API commands create a durable PostgreSQL job and publish through an outbox. The API returns `202 Accepted`. Worker consumers update progress, heartbeat, attempts and terminal state. Messages are versioned, idempotent and retry-classified.

## Consequences
### Positive
- Responsive API and observable long-running work.
- Retry, cancellation and DLQ support.

### Negative
- Eventual consistency and more operational complexity.

## Constraints enforced on implementation and AI agents
- Heavy work never runs inside an HTTP request.
- Duplicate delivery must not duplicate outputs.
- Completion is visible only after durable output commits.
