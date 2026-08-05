# ADR 0002: PostgreSQL as durable business-state source of truth

## Status
Accepted

## Context
RabbitMQ, Redis, MinIO and Forgejo all hold operational data, but GodForge needs one durable state model for authorization, jobs, analysis and audit.

## Decision
PostgreSQL is authoritative for users, organizations, projects, permissions, jobs, revision metadata, findings, AI runs, asset metadata and audit state. RabbitMQ is transport, Redis is cache/lock, MinIO stores objects, and Forgejo stores Git objects/refs.

## Consequences
### Positive
- Consistent recovery and queryable state.
- Jobs remain visible if messages are delayed or duplicated.

### Negative
- Cross-system consistency requires outbox/inbox and reconciliation.
- PostgreSQL schema governance becomes critical.

## Constraints enforced on implementation and AI agents
- Never use Redis or RabbitMQ as the only record of a business event.
- Store object references and checksums, not large binaries, in PostgreSQL.
