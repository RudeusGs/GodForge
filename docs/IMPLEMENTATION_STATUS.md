# Implementation Status

This document reflects the verified implementation state of GodForge based on executable source code and automated tests.

---

## 1. Feature Implementation Matrix

| Module | Status | Implemented Functionality | Verified Scope | Remaining Work |
|---|---|---|---|---|
| **M0: Foundation & Runtime** | Implemented | Clean Architecture (.NET 10 LTS), ASP.NET Core API, Worker host, PostgreSQL 16 migrations, Redis 7 cache/locks, RabbitMQ 3.13, Docker Compose. | Verified locally & in CI | Production container hardening, release observability deployment. |
| **M1: Authentication & Sessions** (`FR-01.*`) | Implemented | OTP registration via email, secure login, HttpOnly SameSite=Strict refresh cookies, multi-device session management, remote session revocation, password reset, distributed Redis abuse limiters. | Tested locally & verified in CI | MFA (`FR-01.6`, documented extension). |
| **M1: User Session Management** (`FR-01.4`) | Implemented | `GET /users/me`, session listing, single session revocation, current session logout, cross-tab invalidation, safe return path preservation. | Tested locally & verified in CI | None for M1 baseline. |
| **M1: Public Landing Page** (`FR-00.1`) | Implemented | Responsive landing page, product positioning bento, accessibility landmarks, reduced-motion compliance. | Tested locally & verified in CI | None. |
| **M1: Organization Management** (`FR-27`) | Implemented | Organization creation, slug normalization, overview, general settings (1–160 char name, 1–80 char slug), member directory with search/filters, invitations, URL token scrubbing, ownership transfer, deletion requests (`deleting` lifecycle state). | Tested locally & verified in CI | None for internal tenancy. |
| **M1: Tenant & Project Membership Hierarchy** (`FR-27.1`, `FR-27.2`) | Implemented (Internal) | Internal GodForge membership revocation: removing or suspending an organization member atomically revokes all project memberships in that organization. | Tested locally & verified in CI | External Forgejo hosted-provider reconciliation (`FR-27.2` / `AC-FR-27.2-02`), scheduled as an M2 acceptance requirement. |
| **M1: Project Lifecycle & Catalog** (`FR-03`, `FR-27.3`) | Implemented | Project creation within organization, slug uniqueness, metadata overview, assigned project list vs administration catalog separation, project archive and restore. | Tested locally & verified in CI | None for M1 project lifecycle. |
| **M1: Project Membership & Settings** (`FR-03.1`, `FR-17`) | Implemented | Project member listing, candidate picker beyond 100 members, role assignment, role updates, member removal, project ownership transfer, project settings updates, deletion requests. | Tested locally & verified in CI | Complete analysis profile integration (deferred to M4–M6). |
| **M1: Settings & Policy Management** (`FR-17.1`, `FR-17.2`) | Implemented | Own-user preferences, organization-wide AI policy restrictions, effective policy resolution (Platform > Org > Project), schema upgrades. | Tested locally & verified in CI | Advanced asset-retention and quota policies. |
| **M2: Hosted Git & Repository Foundation** (`FR-04`–`FR-07`, `FR-21`) | Partial; final gates open | Short claim/I/O/finalize boundaries, leased/fenced jobs, desired-state generations, automatic managed hooks, hosted and linked browsing/sync, Vue polling and protection. | Real Forgejo API/outbox/Worker provisioning, HTTPS/SSH push/revocation, signed push/replay, linked HTTPS browsing, role races/outage recovery; PostgreSQL concurrency and M0/M1 regressions. | Final real-provider browser rerun and runtime vulnerability scanning. Retained Forgejo upgrades are not tested. |
| **M3: Durable Job Processing** (`FR-19.*`) | Partially implemented | Outbox message dispatcher, Worker queue consumer, RabbitMQ retry with exponential backoff. | Tested locally | Job cancellation, DLQ inspection API, distributed worker coordination. |
| **M4–M6: Godot Validation, Parser & Engine** (`FR-08`, `FR-09`, `FR-10`, `FR-11`, `FR-12`, `FR-20`) | Planned | Basic project.godot validation rules and analyzer structure. | Planned | Deterministic parser for `.tscn`, `.tres`, `.gd`, dependency graph builder, health score engine. |
| **M7: AI Advisory Services** (`FR-22.*`) | Planned | Structured context builder abstraction, secret redaction filter, policy gating. | Planned | End-to-end Gemini advisory integration, advisory display in UI. |
| **M8: Asset Vault** (`FR-23.*`) | Planned | Storage abstraction, MinIO bucket layout, access policy model. | Planned | Chunked upload, signed URL generation, manifest verification, quarantine pipeline. |
| **M9: Collaboration, Activity & Reports** (`FR-14`, `FR-16`, `FR-18`, `FR-24`, `FR-25`) | Planned | Audit log persistence, activity writer interface. | Planned | Finding collaboration workflows, activity feed, PDF/JSON/CSV report generation. |
| **M10: Production Hardening & Operations** | Planned | Local Docker Compose, health check endpoints, backup/restore runbook. | Planned | Production secret management, OpenTelemetry metric export, staging deployment drill. |

---

## 2. Verification Summary

For detailed test commands, architecture breakdown, regression scenarios, and reproducing test runs, refer to the [Testing and Verification Guide](TESTING.md).

- **Backend Unit Tests:** 305 passed (including final retry-actor and linked URL credential guards).
- **Backend Integration Tests (PostgreSQL 16 & Redis 7):** 165 passed; 1 provider adapter skipped in the provider-free gate. The isolated real adapter command separately passes with zero skips. Clean and preceding-M1 schema upgrade tests pass.
- **Frontend Unit Tests (Vitest):** 155 passed.
- **Controlled Browser Tests (Playwright Chromium):** 22 passed.
- **Live Browser-to-API Acceptance Tests:** 10 passed.
- **Backend Smoke Gate:** 41 HTTP assertions passed.
- **Vulnerability & Secret Audits:** 0 High/Critical findings in npm and NuGet dependencies.

---

## 3. Status Maintenance Rules

1. A module may be marked **Implemented** only when its production code exists and satisfies documented functional requirements.
2. A module may be marked **Verified** only when automated unit, integration, or E2E tests have been executed with passing evidence.
3. Interfaces, entities, or design documentation alone do not constitute proof of implementation.
4. Blocked dependencies must remain explicitly identified.

## M2 verification boundary

The pinned Forgejo 15.0.11 image uses isolated volumes; the legacy root
Forgejo 11 deployment has not been upgraded. No existing developer database was
reset. `npm.cmd run test:m2:provider` bootstraps the real adapter configuration;
`npm.cmd run test:m2:acceptance` exercises the full HTTP/outbox/Worker/provider/Git
path and real browser tests. An adapter-only pass does not establish full
GodForge membership-to-provider acceptance.

`AC-FR-21-01`, `AC-FR-21-02`, `AC-FR-21-03`, `AC-FR-07-01` and
`AC-FR-27.2-02` are tracked against the full suite; provider-free tests alone
cannot close them. Final browser/security gates remain open. M3 remains gated.
