# Backend Feature Evidence

Reviewed 2026-10-26. This is a feature inventory, not a claim that all Core requirements are implemented. The backend-first request retains the product requirements and milestone dependencies. API route presence, EF entity presence and a passing baseline test suite are insufficient to close a feature.

| Scope | Current executable evidence | Remaining completion gate |
|---|---|---|
| .NET 10 API/Worker runtime | All seven projects target `net10.0`. Root-context multi-stage Dockerfile publishes API/Worker into .NET 10 runtime images. Local Compose runs PostgreSQL, Redis, RabbitMQ, Mailpit, API and Worker; API readiness and Worker queue subscription verified. | Exact-snapshot clean CI, image vulnerability/secret scans, immutable production artifacts/configuration and release validation. |
| Registration/login/recovery (`FR-01.*`) | PostgreSQL HTTP tests plus live Compose smoke: OTP delivery through encrypted outbox/Worker/SMTP, verified registration, two device sessions, remote session revocation, refresh, reset email/atomic invalidation and logout. | MFA remains the documented Should extension; production email delivery, abuse/load and release evidence remain required. |
| Organization/project lifecycle (`FR-27`, `FR-03`) | Live organization/project creation and idempotency, idempotent invitation creation (`Idempotency-Key` scope `organization.invitation.create`), invitation email/serialized acceptance, membership grants/role updates/removal, ownership transfers (requiring an active target user account), general settings updates, deletion request (`deleting` state), archive/restore and immediate organization-to-project revocation. PostgreSQL HTTP tests (`OrganizationManagementHttpTests`, `TenantProjectLifecycleHttpTests`, `TenantBoundaryAndRbacTests`) cover the full RBAC matrix, all 10 invitation security cases, all 6 concurrency cases, boundary lengths, >100-member pagination/search/transfer, multi-owner cross-page removal, archived/non-active mutation rejection, and consumed transfer versions; see [acceptance evidence](ORGANIZATION_MANAGEMENT_ACCEPTANCE_EVIDENCE.md). | Required durable external-provider reconciliation (`FR-27.2` / `AC-FR-27.2-02`) remains open; project management UI is covered in the current project addendum; deletion currently enters a retained `deleting` state and does not prove completed purge. Full M1 acceptance is still open. |
| Project settings (`FR-17`) | Existing versioned project settings read/write use authorization, archived-state checks and serialized transaction/audit. | Effective organization/platform policy sources, user preferences and complete versioned analysis-profile behavior require acceptance evidence. |
| Repository/hosted Git (`FR-04`–`FR-07`, `FR-21`) | Linked repository API and partial Git workspace/provider adapters exist. | Complete hosted provisioning/permission synchronization, branch/commit/tree/blob APIs, signed webhook/replay and real team clone/push workflows after milestone dependencies. |
| Jobs (`FR-19.*`) | Job/outbox entities, dispatcher, RabbitMQ pipeline, claiming/retry/lock code and selected tests exist; actual Worker email dispatch verified. | Complete job cancellation/retry APIs, inbox/DLQ/restart/outage and secure workspace acceptance; no claim of complete durable-pipeline behavior. |
| Validation/parser (`FR-20.*`, `FR-08.*`) | Static deterministic analyzer/parser fixtures and selected workspace/security tests exist. | Supported corpus, normalized revision read APIs, parser provenance and full hostile-input/reproducibility gate. |
| Health/graph/explorers (`FR-09.*`–`FR-12.*`) | Latest health/dependency query endpoints and deterministic engine components exist. | Revision-bound scene/asset explorer APIs, bounded graph/impact, finding policies and complete acceptance evidence. |
| Incremental/diff (`FR-26`, `FR-13.*`) | Target schema/design and partial pipeline structures. | Executable incremental/full equivalence, fallback, revision/scene comparison and benchmarks. |
| Gemini advisory (`FR-22.*`) | Optional provider/context/redaction components and latest advisory query. Disabled in the local runtime stack. | Evidence-bound provider request validation, quotas, prompt/schema/degraded safety acceptance and configured provider exercise. |
| Asset Vault (`FR-23.*`) | Target data/schema design and storage entities. | Complete authorized upload/versioning/quarantine/manifest/download/hydration service and private object-store integration. |
| Collaboration/dashboard/search/reporting (`FR-14.*`–`FR-16`, `FR-18.*`, `FR-24.*`, `FR-25.*`) | Selected activity/job reads and domain/persistence structures. | Finding workflows, notifications, dashboard aggregates, bounded search, PDF/JSON/CSV durable export and authorization/retention evidence. |
| Production operations | Local health endpoints and operational documentation. | Observability/alerts, retention, performance, security assessment and encrypted backup/restore drill. |

## Local backend commands

```powershell
# Start the complete local M1 backend environment without a frontend.
docker compose -f docker-compose.backend.yml up -d --build --wait

# Real HTTP/API/Worker/SMTP acceptance; creates disposable example.test fixtures.
npm.cmd run test:backend:smoke

# Isolated PostgreSQL/Redis unit/integration gates, without rebuilding a running API executable.
npm.cmd run verify:backend
```

Swagger: `http://127.0.0.1:15072/swagger`; readiness: `http://127.0.0.1:15072/health/ready`; local-only email inbox: `http://127.0.0.1:18025`. Development initializes schema only through the API's guarded Development initializer. The Worker runs in Production mode and never migrates on startup. These example credentials and SMTP settings are confined to this local stack; production migrations remain a separate controlled release step.

`docker compose -f docker-compose.backend.yml down` stops only this stack and preserves its local volumes. The verification runner uses a different disposable Compose project and cannot drop the runtime stack's database. Never point persistence fixtures at a retained database.

The smoke suite asserts HTTP responses without printing OTPs, challenge/refresh/access tokens, passwords or message bodies. Mailpit receives all email locally. It does not contact external recipients, Forgejo or Gemini. The smoke suite currently proves M1 paths, not later modules listed above.


## Project management hardening — 2026-10-30

Project member assignment now honors optional Idempotency-Key with existing ops.idempotency_records and scope project.member.add. Replay rechecks current access/lifecycle/target eligibility; no duplicate audit/membership is produced and a revoked recorded membership cannot be reactivated by replay. Ownership/assignment require an active non-deleted account. Inactive role-update targets return MEMBERSHIP_NOT_FOUND instead of a domain exception. Member DTOs expose accountStatus for candidate selection; directory ordering ends with unique ID. No schema, consumer, purge job or automatic migration was added. All project routes are integrated into the browser. [Executed evidence and open FR-17/provider prerequisites](PROJECT_MANAGEMENT_ACCEPTANCE_EVIDENCE.md).
