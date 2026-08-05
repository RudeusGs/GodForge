# ADR 0012: Transactional outbox and consumer inbox for production

## Status
Accepted

## Context
Persisting a job and publishing a message are separate operations. Failures between them can lose work; duplicate delivery is normal.

## Decision
Production message publication uses a PostgreSQL outbox written in the same transaction as business/job state. A dispatcher publishes and marks delivery. Consumers use an inbox or equivalent deduplication record keyed by message ID and input identity.

## Consequences
### Positive
- No silent lost job after database commit; duplicate handling is explicit.

### Negative
- Dispatcher, cleanup and monitoring are required.

## Constraints enforced on implementation and AI agents
- Direct publish after commit may be used only for local prototypes and must not be labeled production-ready.
- Outbox/inbox records have retention and replay procedures.
