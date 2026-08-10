# Implementation Status

Evidence basis: the repository snapshot and executable local verification performed on 2026-08-10. This file records only behavior present in the current snapshot; future milestones are not counted as current defects.

## Current evidence

| Area | Status | Current evidence |
|---|---|---|
| Baseline and CI | Local executable gates passed | Backend projects resolve SDK `10.0.302`; restore, format verification, build and automated tests pass locally. Frontend has a committed lockfile and its install, lint, typecheck, unit-test and build gates pass locally. Clean-checkout CI reproducibility remains a release-gate concern until CI runs this exact snapshot. |
| Authentication and sessions | Locally verified for M1 Must workflows | Registration/reset challenges are keyed hashes with active-scope uniqueness and attempt-exhaustion handling. Login lockout/state checks, session-bound JWT validation, one-time refresh rotation/reuse response, logout, password reset and own-session revocation are covered by automated tests. Refresh tokens remain only in the scoped HttpOnly SameSite=Strict cookie. IP/user-agent session metadata is keyed-hashed before persistence. `FR-01.6` MFA remains the documented Should/future extension. |
| User session API and UI | Locally verified | `GET /users/me`, `GET /users/me/sessions` and `DELETE /users/me/sessions/{sessionId}` require an active session-bound JWT. The authenticated UI provides loading, empty, recoverable-error and revocation states and clears local authentication after revoking the current session. |
| Organization lifecycle | Source implementation expanded; runtime verification required | Lifecycle, ownership, membership and invitation routes include audit records, current M1 quotas and persistent idempotency for create operations. Organization suspension/removal propagates project membership changes with set-based SQL. The previous unconsumed provider-reconciliation intents were removed. |
| Project lifecycle | Source implementation expanded; runtime verification required | Project HTTP use cases enter through MediatR validation before delegating to lifecycle/membership services. Lifecycle, ownership, membership and settings routes include audit records, project quota, persistent create idempotency and active organization-membership intersection. The previous unconsumed provider-reconciliation intents were removed. |
| Tenant persistence | Locally verified on isolated PostgreSQL | The complete migration chain applies to a clean database and upgrades from `20260810130447_PendingModelChanges`. Migration `20260810135055_ProtectSessionClientMetadata` removes legacy raw session IP/user-agent columns and adds nullable bounded hash columns. PostgreSQL persistence/concurrency integration tests pass. |
| Frontend authentication | Locally verified | Typed request/response models match the auth API. Registration uses the server-provided OTP resend cooldown and performs normal login after account creation. Refresh-cookie restoration, logout, reset-password and session-management behavior pass unit tests and production build. |
| Linked repository and later modules | Partial; hardening fixes added | Git tree file counting is streamed and workspace scans use bounded backoff. Dependency graphs track `.cs` scripts and preserve `extends`/`preload`/`load` relations. This patch does not claim completion of M2 or later milestones. |
| Worker host startup | Locally verified | The Worker composition root is independent of HTTP/Identity-only services, applies development database initialization only in Development, and retries an unavailable initial RabbitMQ connection with bounded exponential backoff and jitter instead of stopping the host. Composition and broker-outage regressions are covered by automated unit tests. |
| Production deployment | Not ready | Production secrets, backup/restore evidence, full observability, hosted-provider membership synchronization and release hardening remain outside the current M1 source patch. |

## Added verification coverage

The executable integration suite includes organization-role by project-role authorization, suspended/removed membership checks, cross-tenant masking, organization-only access denial, soft-deleted-user pagination, session validation and PostgreSQL concurrency/migration checks. Identity unit coverage includes OTP exhaustion, non-active account login, session metadata hashing, refresh rotation/replay/concurrency and atomic password-reset side effects. Frontend unit coverage includes refresh-cookie state and authoritative own-session listing/revocation.

## Verification state

The source, contracts and local executable evidence are synchronized for Identity M1. Production/release gates remain separate: dependency/secret scanning, staging observability, backup/restore, production configuration and a clean-checkout CI run are not claimed by this local feature verification.

Do not mark the entire product milestone or production release complete solely from this status file.

## Status update rule

A row may be marked fully verified only when code, automated tests, documentation and a reproducible execution record are available. Entity or interface presence alone is not proof of runtime completion.
