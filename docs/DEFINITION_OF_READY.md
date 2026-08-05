# Definition of Ready

A feature is ready for implementation only when all applicable items are complete.

## Product and requirements

- A stable requirement ID exists in `docs/SRS/03-functional/`.
- Purpose, actors, main flow, alternate flows and error cases are documented.
- Scope is classified as Core, Advanced, Extension or Deferred.
- Acceptance criteria are objective and testable.
- Current-versus-target status is explicit.

## Architecture and data

- Architectural impact is consistent with `SRS/02-architecture.md`.
- A new architectural decision has an accepted ADR when required.
- Database tables, ownership, indexes, uniqueness, retention and migration impact are documented.
- Storage placement is decided: PostgreSQL, MinIO, Redis cache or Forgejo.
- Sync versus async execution is decided.
- Idempotency and concurrency behavior are defined.

## API and security

- Endpoint method, route, permission, request, response and error codes are documented.
- RBAC and tenant boundaries are mapped.
- Threats, secret exposure, input validation, audit requirements and rate limits are identified.
- Sensitive data classification is known.

## Operations and testing

- Logs, metrics, traces and alert conditions are defined.
- Unit, integration, security and acceptance tests are listed.
- Rollback, retry, cancellation and cleanup behavior are defined where applicable.
- Performance budget is defined for expensive operations.

## Agent prohibition

AI coding agents must not implement a feature that fails this definition. They must update or request updates to documentation first.
