# Implementation Status

Evidence basis: the repository snapshot and source-level verification performed on 2026-08-05. This file records only behavior present in the current snapshot; future milestones are not counted as current defects.

## Current evidence

| Area | Status | Current evidence |
|---|---|---|
| Baseline and CI | Source aligned; executable gate still required | Backend projects and CI resolve SDK `10.0.302` from `GodForge-BE/global.json`. Frontend CI uses existing scripts. `GodForge-FE/package-lock.json` is still absent from this snapshot, so reproducible `npm ci` remains blocked until the lockfile is generated and committed from a registry-enabled environment. |
| Authentication and sessions | Source implementation expanded; runtime verification required | Registration and reset challenges are stored as keyed hashes in PostgreSQL, active challenge scope is protected by a filtered unique index, email enumeration is avoided, and email delivery uses the encrypted outbox. Login creates a server-side session and session-bound JWT. Raw refresh tokens are transported only in an HttpOnly SameSite=Strict cookie and are no longer exposed to frontend JavaScript. Refresh reuse, logout, session revocation and password reset emit security audit records. |
| User session API | Implemented in source | `GET /users/me`, `GET /users/me/sessions` and `DELETE /users/me/sessions/{sessionId}` are present and require an active session-bound JWT. |
| Organization lifecycle | Source implementation expanded; runtime verification required | Lifecycle, ownership, membership and invitation routes include audit records, current M1 quotas and persistent idempotency for create operations. Organization suspension/removal propagates project membership changes with set-based SQL. The previous unconsumed provider-reconciliation intents were removed. |
| Project lifecycle | Source implementation expanded; runtime verification required | Lifecycle, ownership, membership and settings routes include audit records, project quota, persistent create idempotency and active organization-membership intersection. The previous unconsumed provider-reconciliation intents were removed. |
| Tenant persistence | Source migration added; PostgreSQL execution required | Migration `20260805123000_CloseM1SourceGaps` adds the active-challenge uniqueness invariant and persistent idempotency records. Existing identity/session and composite project-tenant constraints remain in the model. The new migration has not yet been exercised in this snapshot. |
| Frontend authentication | Implemented in source | Frontend request/response models match the current auth API. Registration consumes the `201 UserSummary` response and then performs a normal login to establish a session. Logout sends an empty body. |
| Linked repository and later modules | Partial / unchanged | Existing repository, worker, parser, graph, advisory and other post-M1 foundations remain at their prior snapshot status. This patch does not claim completion of M2 or later milestones. |
| Production deployment | Not ready | Production secrets, backup/restore evidence, full observability, hosted-provider membership synchronization and release hardening remain outside the current M1 source patch. |

## Added verification coverage

The integration-test source now includes an organization-role × project-role authorization matrix, suspended/removed membership checks, cross-tenant masking and organization-only access denial. These tests are source evidence only until `dotnet test` completes successfully against the required SDK and PostgreSQL-backed constraint tests also pass.

## Verification state

The source and contracts have been synchronized in this patch. The complete executable quality gate must still be run in an environment with:

- .NET SDK `10.0.302` and access to the configured NuGet feeds.
- Node.js 22/npm 10 and access to the npm registry.
- PostgreSQL 16 for migration and tenant-constraint integration tests.

Do not mark a milestone complete solely from this status file. A milestone is complete only after the commands in `QUALITY_GATES.md` pass from a clean checkout and the database migration is exercised against PostgreSQL.

## Status update rule

A row may be marked fully verified only when code, automated tests, documentation and a reproducible execution record are available. Entity or interface presence alone is not proof of runtime completion.
