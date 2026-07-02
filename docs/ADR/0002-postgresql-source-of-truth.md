# ADR 0002: PostgreSQL as Source of Truth

## Status
Accepted

## Context
GodForge manages critical state transitions across multiple services, including async jobs, RBAC, collaboration, and repository management. We need a primary store that supports robust transactions, relational integrity, and standard querying.

## Decision
**PostgreSQL** will act as the absolute and sole source of truth for all durable business data and job states.

- **RabbitMQ** will be used exclusively as an asynchronous message transport and never as a durable store of job status.
- **Redis** will be used for ephemeral caching, rate-limiting, and distributed locks (e.g., repository workspace locks). It must not store primary business records.
- **MinIO/S3** will be used strictly for artifact/blob storage (thumbnails, diffs, archives) with metadata pointers stored in PostgreSQL.

## Consequences
### Positive
- Strict ACID compliance for business operations.
- Easier backups, Point-in-Time Recovery (PITR), and auditing.
- No split-brain state issues between message queues and databases.

### Negative
- Requires polling or transactional outbox patterns to ensure atomicity between DB writes and RabbitMQ publishing.
- High volume tables (e.g. metadata) may require partitioning in PostgreSQL.

## Constraints enforced on AI agents
- Do not build behavior that assumes a RabbitMQ message failure means the job state is lost; job state must already exist in PostgreSQL before dispatch.
- Redis must not be used to store persistent configurations or business models.
- All migrations must be explicitly defined using EF Core code-first in the infrastructure layer.
