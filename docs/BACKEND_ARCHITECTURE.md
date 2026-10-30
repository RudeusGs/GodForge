# Backend Architecture

Settings extension: MediatR own-user query/command handlers use typed repositories, current session actor, strict boundary DTOs, FluentValidation and independent EF concurrency tokens. OrganizationPolicyHandler applies current Owner/Admin authorization under the shared organization-membership lock. ProjectSettingsService enforces EffectiveSettingsPolicy inside the existing serialized mutation before changing the row. Gemini.Enabled is the platform ceiling; organization allowance is a restriction preference, never a permission grant. Fresh database projections bypass tracking/cache for queued AI eligibility before provider/cache use and after provider response. Existing job/outbox/claim flows remain unchanged. Successful explicit writes commit safe before/after audit summaries atomically and emit the bounded `GodForge.Settings` meter counter `godforge.settings.updates` with scope/outcome tags; production metric export remains an operations gate. [Settings contract](SRS/05-api-contracts/settings.md); [evidence and dependencies](M1_SETTINGS_POLICY_ACCEPTANCE_EVIDENCE.md).

## Runtime target

Stabilization: remote session revocation retries up to three optimistic conflicts from fresh state, rechecks ownership, revokes current token descendants and invalidates cache before persistence. Exhaustion returns conflict. A PostgreSQL HTTP barrier commits refresh between revocation read/save and verifies final revocation plus exactly one audit. AI policy is rechecked after cache lookup; these are point-in-time checks, not atomic publication fencing. Legacy trigger AnalysisProfile names AI advisory intent, validated independently of deterministic registry keys.

- **.NET 10 LTS / ASP.NET Core 10 / EF Core 10**.
- Clean Architecture with Domain, Application, Infrastructure, API and Worker hosts.
- Command/query use cases with explicit transaction and authorization decisions.
- Boundary validation through dedicated validators.
- PostgreSQL is the primary business database.

All seven current backend projects target `net10.0`. Locked restore, build, tests and .NET 10 API/Worker runtime images have current executable evidence in the [final stabilization record](M1_SETTINGS_POLICY_ACCEPTANCE_EVIDENCE.md). Production release acceptance remains separate.

## Layer responsibilities

### Domain

- Enterprise invariants, state transitions, value objects and domain events.
- Organization/project membership rules and ownership-transfer invariants.
- No EF Core, HTTP, Git, queue, cache, storage, AI, filesystem or configuration dependency.

### Application

- Commands, queries, DTOs, validators, permission decisions and transaction orchestration.
- Interfaces for persistence, Git, queue, cache, storage, AI, clock, email and security.
- Stable typed error model mapped to `docs/ERROR_CODES.md`.
- Tenant scope is resolved from authenticated actor and durable membership state, not from client IDs alone.

### Infrastructure

- EF Core mappings and migrations.
- PostgreSQL repositories and transaction/outbox implementation.
- Redis cache, rate-limit backing and distributed lock provider.
- RabbitMQ transport, outbox dispatcher and inbox deduplication.
- MinIO object storage.
- Forgejo and external Git adapters.
- Gemini adapter, redaction pipeline and response-schema validation.
- Email, secret protection, observability and provider reconciliation.

### API

- Authentication middleware, request binding, OpenAPI, rate limiting and response envelopes.
- Calls Application use cases only.
- No direct database access, Git operations, filesystem work, parsing, report rendering or Gemini calls.
- Heavy operations return `202 Accepted` with a durable job reference.

### Worker

- Durable job consumption, stage orchestration, progress, heartbeat, retry, timeout, cancellation and cleanup.
- Isolated, non-root repository workspaces.
- Revalidates current project/repository state before publishing output.
- Never executes untrusted Godot scripts, plugins, native extensions, builds or exports.

## Tenant and authorization boundary

- Organization is the tenant boundary.
- Project belongs to exactly one organization.
- Active project membership requires active organization membership in the same organization.
- Organization administrative roles do not implicitly grant project-content access.
- Effective permission is evaluated as:

```text
platform minimum
INTERSECT organization policy
INTERSECT project role permission
INTERSECT resource-specific policy
```

- Removed or suspended organization membership invalidates all project access and emits provider-reconciliation events.

## Transaction rule

A state-changing use case defines one business transaction boundary. Business changes, audit intent and outbox records are committed atomically. Provider side effects that cannot be transactional require idempotency, reconciliation and compensating behavior.

Examples:

- Refresh-token rotation updates token family and session state atomically.
- Organization-member removal updates all affected project memberships and writes Forgejo reconciliation events atomically.
- Project creation, creator ProjectOwner membership and audit/outbox records commit atomically.

## Read-model rule

- Apply tenant and permission filtering before projection.
- Return explicit DTO projections; never return domain entities.
- Paginate lists and cap page size.
- Avoid deep `Include` graphs and accidental N+1 queries.
- Inspect query plans and query counts for high-volume endpoints.
- Cache only permission-safe read models with tenant-aware keys.

## Error rule

Domain/Application return typed errors. API maps them to `docs/ERROR_CODES.md`. Worker maps failures to retryable, non-retryable, degraded or dead-letter outcomes. Public errors never contain stack traces, SQL, credentials, provider payloads or workspace paths.

## Database migration rule

- Migrations are forward-only and reviewed.
- Fresh-database and upgrade-from-prior-release paths are tested.
- Production migration is a controlled deployment step, not automatic startup work across API replicas.
- Destructive changes require backup, compatibility plan and documented rollback/recovery behavior.
