# ADR 0011: Organization and project multi-tenant security boundary

## Status
Accepted

## Context
GodForge may serve companies. A project identifier supplied by a client is not proof of access, and cross-tenant leakage is a critical failure.

## Decision
Organization is the tenant boundary; project is the primary authorization scope. Every query and mutation resolves actor membership and permission server-side. Storage objects, cache keys, locks, jobs and audit events carry organization/project identifiers. System-admin actions are separately audited.

## Consequences
### Positive
- Clear enterprise isolation and policy ownership.

### Negative
- More authorization joins, tests and operational metadata.

## Constraints enforced on implementation and AI agents
- Never fetch by resource ID alone when tenant scope is available.
- Mask existence with 404 where policy requires.
- Background jobs revalidate current project state before publishing results.
