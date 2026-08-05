# GodForge Agent Rules

These rules are mandatory for any AI coding agent working in this repository. The folder name is `.agents`.

## 1. Operating principle

GodForge is a production-oriented graduation project. Agents must optimize for correctness, security, traceability and complete end-to-end behavior, not for the fastest code generation or the largest number of superficial features.

Do not claim a target feature is implemented because entities, interfaces or documentation exist. Verify executable behavior and tests, then update `docs/IMPLEMENTATION_STATUS.md`.

## 2. Required reading order

Before changing code:

1. `docs/PRODUCT_VISION.md`.
2. `docs/SRS/01-scope.md`.
3. `docs/SRS/02-architecture.md`.
4. Relevant `docs/SRS/03-functional/*.md`.
5. `docs/SRS/04-database.md` for data changes.
6. `docs/SRS/05-api.md` and `docs/ERROR_CODES.md` for APIs.
7. `docs/SRS/06-security.md`, `docs/THREAT_MODEL.md` and `docs/RBAC_MATRIX.md`.
8. `docs/SRS/07-non-functional.md`.
9. `docs/SRS/08-workflows.md`.
10. `docs/SRS/11-testing-acceptance.md`.
11. `docs/SRS/12-worker-processing.md` for asynchronous work.
12. `docs/SRS/13-deployment-operations.md` and `docs/SRS/15-observability.md` for runtime changes.
13. Relevant ADRs.

If requirements are missing or contradictory, update/request documentation before implementation. Do not silently invent product behavior.

## 3. Definition of Ready

Do not implement until `docs/DEFINITION_OF_READY.md` is satisfied. At minimum identify:

- Requirement and acceptance IDs.
- Actor and permission.
- API and data impact.
- Sync versus async behavior.
- Idempotency/concurrency behavior.
- Threats and data classification.
- Tests and observability.

## 4. Architecture boundaries

```text
GodForge.Domain         -> no project dependency
GodForge.Application    -> GodForge.Domain
GodForge.Infrastructure -> GodForge.Application + GodForge.Domain
GodForge.Api            -> GodForge.Application + GodForge.Infrastructure
GodForge.Worker         -> GodForge.Application + GodForge.Infrastructure
```

### Domain

Own entities, value objects, invariants, state transitions and domain events. No EF Core, HTTP, Git, queue, cache, storage, AI, filesystem or configuration dependency.

### Application

Own commands, queries, DTOs, validators, permission decisions, interfaces and orchestration. Do not directly use EF Core/provider clients/filesystem.

### Infrastructure

Implement PostgreSQL, Redis, RabbitMQ, MinIO, Git/Forgejo, email, Gemini, encryption and observability adapters. Do not place business policy here.

### API

HTTP only: bind, authenticate, call Application and map response. No business logic, direct database, Git operations or long work.

### Worker

Consume durable jobs, orchestrate safe stages, report progress and enforce retry/timeout/cancellation/cleanup. Business authorization to create a job remains in Application; workers revalidate stale state before publishing output.

## 5. Product invariants

- Forgejo/external provider is Git-object/ref source of truth.
- PostgreSQL is GodForge business/job/analysis source of truth.
- Redis is cache/lock, not business truth.
- RabbitMQ is transport, not job state.
- MinIO stores bytes; PostgreSQL stores metadata and authorization references.
- Parser/rule engine is authoritative; Gemini is advisory.
- Analysis is bound to immutable commit SHA and version identity.
- No untrusted repository code is executed.
- Private assets are not made private by hiding paths in a public Git repository; use Asset Vault.

## 6. CQRS and use cases

- One user intent per command/query.
- Commands change state and return typed results.
- Queries return DTO projections and never domain entities.
- Validators handle boundary shape; domain/application handle business invariants.
- All project queries/mutations enforce current actor scope in Application.
- Write use cases define activity/audit intent and transaction boundary.
- Heavy use cases create durable jobs and return quickly.

## 7. API rules

- Use `/api/v1` and standard envelopes.
- Use stable errors from `docs/ERROR_CODES.md`.
- No stack trace, SQL, credential, signed URL, provider payload or workspace path in client errors.
- Request DTOs expose only allowed fields.
- Paginate lists; cap page size and validate sort/filter allow-lists.
- Use 202 for durable async work.
- Project-level authorization is never implemented only with controller attributes.
- Webhooks use provider signature, replay and repository identity checks, not user JWT.

## 8. Database and migration rules

- Read `docs/SRS/04-database.md` and `docs/DATABASE_CHANGE_CHECKLIST.md`.
- Every tenant-owned row has an explicit scope path.
- Add database constraints for business uniqueness and idempotency.
- Add indexes for documented high-volume queries.
- Avoid deep `Include`; use projections and bounded queries.
- Do not store large source/binary objects in PostgreSQL.
- Migrations are forward-only and tested on clean and prior schema.
- Never delete production migrations or use destructive reset as a migration strategy.
- Data backfill must be bounded, resumable and observable.

## 9. Worker and messaging rules

- Durable job row and outbox event are created atomically for production paths.
- Messages include schemaVersion, messageId, jobId, organizationId, projectId, correlationId, attemptCount and inputHash.
- Messages contain identifiers/references, not credentials or large payloads.
- Consumers deduplicate through inbox/idempotency identity.
- Classify errors before retry.
- Use bounded exponential backoff and DLQ.
- Cancellation tokens and timeouts propagate through all I/O.
- Progress is monotonic and heartbeat is periodic.
- Completion is emitted only after output commit.
- Temporary workspace and locks are released on all terminal paths.

## 10. Repository and workspace security

- Treat repository content as hostile.
- Allow only configured remote schemes/hosts and apply SSRF checks.
- Use safe process argument invocation, never shell string concatenation.
- Never embed credentials in remote URL or logs.
- Canonicalize paths and reject traversal/symlink escape.
- Enforce repository/file/depth/timeout/disk quotas.
- Run workers non-root and without Docker socket.
- Do not run Godot Editor, scripts, plugins, native extensions, builds or exports.

## 11. Godot parser and analysis rules

- Parser output must be deterministic and canonically ordered.
- Keep parser, rule-set and profile versions explicit.
- Missing/malformed individual files should become diagnostics/findings when safe rather than destroying unrelated output.
- Health score is a documented deterministic calculation.
- Incremental analysis must preserve equivalence and safely fall back to full analysis.
- Historical output is immutable by version; do not overwrite under a different engine version.

## 12. AI rules

- Gemini is optional and server-side only.
- Build context from approved deterministic metadata/findings and bounded excerpts.
- Exclude/scan/redact secrets before provider calls.
- Treat repository content as untrusted prompt data.
- Validate JSON/schema; invalid result is degraded, not authoritative.
- Record provider, model, prompt version, input hash, usage and latency.
- AI cannot mutate code, Git, users, permissions, assets, jobs or health score.
- Do not claim AI output is correct without evidence references.

## 13. Asset Vault rules

- Object bytes live in private MinIO storage unless explicitly public.
- Manifest contains logical path, asset/version ID and checksum.
- Authorization is checked before signed URL issuance.
- Signed URL TTL is short; never expose bucket credentials.
- Validate size, MIME/magic and quarantine status.
- Audit protected downloads and policy changes.
- Never claim secrecy for bytes already committed to public Git history.

## 14. Frontend rules

- Vue 3 + TypeScript strict.
- Typed API modules; no ad-hoc untyped response access.
- Server state remains re-fetchable; SignalR is not source of truth.
- Implement loading, empty, error, degraded and permission states.
- Escape/sanitize repository content, Markdown, comments and filenames.
- Paginate/virtualize large trees/tables and bound graphs.
- UI hiding is not authorization.

## 15. Observability and privacy

Use structured logs with safe IDs, correlation, job and error code. Never log Restricted data. Add metrics for latency, job lifecycle, queue lag, provider calls, analysis stages and cleanup failures. Add traces without raw sensitive payloads.

## 16. Testing requirements

At minimum for changed behavior:

- Unit tests for business rules.
- Integration tests for API/persistence/authorization.
- Cross-tenant tests for every project resource.
- Worker duplicate/retry/timeout/cancellation tests for async features.
- Security regression for affected threat-model entries.
- Performance/query tests for high-volume paths.
- Migration test when schema changes.

## 17. Documentation synchronization

Behavior changes update all affected:

- Functional SRS.
- Database/API/security/NFR/workflows.
- RBAC/error codes.
- Traceability and tests.
- ADR when foundational.
- Implementation status only after evidence.

## 18. Prohibited shortcuts

- Do not bypass authorization for convenience.
- Do not return entities directly.
- Do not perform heavy work synchronously.
- Do not use `Task.Run` as a job system.
- Do not use Redis as durable state.
- Do not swallow exceptions or retry every error.
- Do not log secrets or raw repository/provider output.
- Do not weaken security minimums through project settings.
- Do not create a custom Git server.
- Do not execute untrusted Godot projects.
- Do not mark work complete without running applicable gates.

## 19. Completion report

When finishing a task, report:

- Requirement IDs implemented.
- Files changed.
- Security/data/async decisions.
- Tests and exact commands run.
- Known limitations and deferred work.
- Documentation synchronized.

Follow `docs/DEFINITION_OF_DONE.md` and invoke the `ci-quality-gate` skill before declaring completion.
