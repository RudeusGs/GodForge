# Testing and Verification Guide

This document is the authoritative developer-facing guide for testing, quality verification, and regression prevention in GodForge.

---

## 1. Overview & Verification Strategy

GodForge employs a multi-tiered verification pipeline designed to validate correctness, security invariants, concurrency handling, and tenant isolation without relying on unverified assumptions:

```text
+--------------------------------------------------------------------------+
|                     Continuous Integration / Local Gates                 |
+--------------------------------------------------------------------------+
|  [Security & Secrets]     Gitleaks secret scan, Trivy container audit,   |
|                           npm & NuGet dependency vulnerability checks    |
+--------------------------------------------------------------------------+
|  [Live-Service E2E]       Playwright browser tests against live API,     |
|                           Worker, Postgres, Redis, RabbitMQ, Mailpit     |
+--------------------------------------------------------------------------+
|  [Controlled E2E]         Playwright browser tests against Vite app with |
|                           contract-driven mock API responses             |
+--------------------------------------------------------------------------+
|  [HTTP Smoke Gate]        Real HTTP workflow assertions via Node script  |
+--------------------------------------------------------------------------+
|  [Integration Tests]      ASP.NET Core WebApplicationFactory against     |
|                           isolated PostgreSQL 16 and Redis 7 containers  |
+--------------------------------------------------------------------------+
|  [Unit Tests]             xUnit (C#) and Vitest (TypeScript/Vue 3)       |
|                           for domain invariants, CQRS, and UI state      |
+--------------------------------------------------------------------------+
```

---

## 2. How to Run Verification

### 2.1 Full Automated Local Verification Gate

The primary verification command runs documentation link checks, provisions disposable PostgreSQL and Redis containers on dedicated test ports (`15433` and `16379`), executes backend and frontend builds, formats, lints, typechecks, unit tests, integration tests, dependency audits, and controlled browser tests, then tears down all test containers automatically:

```powershell
npm.cmd run verify
```

*(On Linux / macOS: `npm run verify`)*

### 2.2 Backend-Only Verification

To run backend formatting, building, unit tests, and real PostgreSQL/Redis integration tests:

```powershell
npm.cmd run verify:backend
```

Or execute directly with the .NET CLI:

```bash
cd GodForge-BE
dotnet restore
dotnet format --verify-no-changes
dotnet build --no-restore
dotnet test --no-build
```

### 2.3 Live Browser-to-API Acceptance Suite

To verify actual browser interactions against live containerized services with real database transactions, JWT cookies, outbox event dispatching, and Mailpit email delivery:

```powershell
npm.cmd run test:workspace:live
```

This command builds the .NET 10 API and Worker runtime images, starts isolated containers using `docker-compose.workspace-test.yml` on dedicated loopback ports (API: `25072`, Vite: `25173`, Mailpit: `28025`), runs Playwright live tests, and removes all containers and fixture volumes on exit.

### 2.4 Backend Runtime Smoke Suite

To run real HTTP integration smoke checks against containerized backend services:

```powershell
# Start local backend stack
docker compose -f docker-compose.backend.yml up -d --build --wait

# Run smoke assertions
npm.cmd run test:backend:smoke

# Tear down stack
docker compose -f docker-compose.backend.yml down
```

### 2.5 Documentation Link Verification

To verify that all relative Markdown file and directory links resolve:

```powershell
npm.cmd run docs:check
```

---

## 3. Test Suites Breakdown

### 3.1 Backend Unit Tests (`GodForge.UnitTests`)

- **Location:** `GodForge-BE/tests/GodForge.UnitTests/`
- **Scope:**
  - Domain invariants: `Organization`, `Project`, `User`, `SlugRules`, `UserInvite`, `ProjectSettings`.
  - CQRS command and query handlers: validation, error-code generation, and business rule enforcement.
  - Pipeline behaviors: MediatR FluentValidation, rate limiting, and context enrichment.
  - Security evaluators: `TenantAuthorizationService`, `OrganizationRolePermissions`, `ProjectRolePermissions`.

### 3.2 Backend Integration Tests (`GodForge.IntegrationTests`)

- **Location:** `GodForge-BE/tests/GodForge.IntegrationTests/`
- **Infrastructure:** Disposable PostgreSQL 16 and Redis 7 containers started per test session.
- **Scope:**
  - **Identity & Session Management:** Multi-device session tracking, refresh token rotation with replay detection, password reset challenge consumption, brute-force lockout, and distributed rate limiting.
  - **Organization Management & RBAC:** Full role matrix enforcement (`OrganizationOwner`, `OrganizationAdmin`, `OrganizationMember`), cross-tenant `404` masking, 10 invitation security invariants, boundary length validation, and >100 member scale pagination.
  - **Project Management:** Project lifecycle (creation, archive, restore), role management, atomic ownership transfer, sole-owner leave prevention, and optional `Idempotency-Key` replay.
  - **Settings & Policy:** Own-user preferences, organization policy inheritance, effective AI policy precedence, and additive migration verification on clean and upgraded schemas.
  - **Concurrency & Transaction Atomicity:** Advisory lock serialization on membership changes, simultaneous ownership transfers, and race conditions between member removal and project creation.

### 3.3 Frontend Unit Tests (`GodForge-FE`)

- **Location:** `GodForge-FE/src/**/*.spec.ts`
- **Runner:** Vitest with Vue Test Utils.
- **Scope:**
  - HTTP transport: Axios interceptors, `baseApi` payload unwrapping, and automatic 401 token refresh retry.
  - Authentication coordination: Web Locks deduplication, BroadcastChannel cross-tab invalidation, and IndexedDB lease fallback.
  - Workspace state: Shared domain validation rules (`validateOrganizationFields`), request generation tracking, and disposal hooks.
  - Views & Components: Organization switcher, member directories, invitation lifecycle, settings panels, theme preferences, and error/empty/loading states.

### 3.4 Controlled Browser Tests (`GodForge-E2E`)

- **Location:** `GodForge-E2E/tests/*.spec.ts`
- **Runner:** Playwright (Chromium).
- **Scope:**
  - `TC-AUTH-BROWSER-*`: Login return-path preservation, session restoration via refresh, multi-tab logout propagation, session revocation, and password-reset link scrubbing.
  - `TC-WORKSPACE-BROWSER-*`: Organization onboarding, section-preserving tenant switching, catalog metadata separation, member role management, invitation creation/revocation, settings updates, and deletion request confirmation.
  - `TC-PROJECT-BROWSER-*`: Project member assignment, role updates, project settings modification, and archive/restore workflows.
  - `TC-SETTINGS-BROWSER-*`: User preferences update, theme toggling, and organization policy gating.

### 3.5 Live Full-Stack Browser Tests (`GodForge-E2E`)

- **Config:** `GodForge-E2E/playwright.live.config.ts`
- **Scope:**
  - `TC-WORKSPACE-LIVE-001`: User registration via OTP, login, organization/project onboarding, and state restoration on page reload.
  - `TC-WORKSPACE-LIVE-002`: Invitation creation, outbox worker email delivery to Mailpit, unauthenticated link redirect with URL token scrubbing, token acceptance, member role promotion, settings rename, candidate search, ownership transfer, server-enforced `LAST_OWNER_REQUIRED` error display, member removal, and organization deletion request.
  - `TC-PROJECT-LIVE-*`: Full project collaboration, candidate picker traversal beyond 100 members, ownership transfer, and project archive/restore.
  - `TC-SETTINGS-LIVE-*`: Real preferences persistence and organization AI policy restrictions.

---

## 4. Key Verification & Regression Scenarios

### 4.1 Domain-Aligned Validation Bounds
- Organization names: `1–160` characters after trimming.
- Organization and project slugs: `1–80` lowercase alphanumeric characters with single interior hyphens (`^[a-z0-9]+(?:-[a-z0-9]+)*$`).
- Verified in frontend forms, request validators, and PostgreSQL integration tests.

### 4.2 Authoritative Last-Owner Protection
- Client-side directories display server-paginated slices (`pageSize = 20`) and never speculate on total owner counts.
- Last-owner checks are enforced authoritatively by the backend under serialized membership locks (`409 LAST_OWNER_REQUIRED`).
- Verified in both single-page and cross-page multi-owner removal scenarios.

### 4.3 Scalable Ownership-Transfer Candidate Selection
- Candidate selection supports organizations of arbitrary size (>100 members) using server-side pagination and search queries (`GET /api/v1/organizations/{id}/members?status=active&search=...`).
- Selected candidates remain pinned in UI state across pagination and search clearing until confirmed.

### 4.4 Generation-Guarded State & Tenant Isolation
- Form loading composable (`useResource`) uses request generations and disposal flags (`onBeforeUnmount`) to prevent slow responses from an earlier organization from overwriting active tenant form inputs.
- Active tenant mutations track `activeTenantEpoch` to discard out-of-order completions.

### 4.5 Token Hygiene & URL Scrubbing
- Invitation tokens received via URL queries (`?token=...`) are captured into in-memory module state and immediately scrubbed from browser history via `router.replace`.
- Raw tokens are never written to `localStorage`, `sessionStorage`, cookies, or client logs.

### 4.6 Concurrency & Fresh-State Retries
- Session revocation racing with token refresh retries from fresh database state to prevent spurious `409 Conflict` failures.
- Member role updates, ownership transfers, and invitation acceptances execute within explicit serialized locks.

---

## 5. Security & Static Analysis Gates

| Gate | Tool | Target | Enforcement |
|---|---|---|---|
| Secret Scanning | Gitleaks | Whole repository | Blocks committed credentials, private keys, and tokens |
| Container Scanning | Trivy | API & Worker Docker images | Blocks High and Critical CVE vulnerabilities |
| NuGet Vulnerabilities | .NET CLI | Backend solution | Blocks vulnerable transitive and direct packages |
| npm Vulnerabilities | npm audit | Frontend & E2E packages | Blocks High and Critical severity packages |
| Tenant Isolation | xUnit | API endpoints | Negative tests verifying cross-tenant `404` masking |

---

## 6. Current Verified Baseline & Known Limitations

### 6.1 Baseline Execution Status
- **Target Runtime:** .NET 10 LTS (`net10.0`), Node.js 22, Vue 3 + TypeScript.
- **Backend Unit Tests:** 305 passed, 0 failed (including final retry-actor and linked URL credential guards).
- **Backend Integration Tests:** 165 passed, 0 failed; 1 provider adapter test skipped in the provider-free regression gate. Includes clean/prior-M1 migration checks, lease recovery, delayed retries, generation fencing and UTC Git metadata.
- **Frontend Unit Tests:** 155 passed, 0 failed.
- **Controlled Browser Tests:** 22 passed, 0 failed.
- **Live Browser Workflows:** 10 passed, 0 failed.
- **Backend Smoke Assertions:** 41 passed, 0 failed.

### 6.2 Deferred Features & Milestone Boundaries
- **Hosted Git Reconciliation (`FR-27.2` / `AC-FR-27.2-02`):** Transactional reconciliation intents, desired-state Worker processing, access generations and drift repair are implemented. PostgreSQL tests cover durable intent and stale-generation fencing. External acceptance additionally requires real authenticated Git revocation; publishing an outbox record alone does not prove it.
- **Godot Parsing & Health Analysis (`M4–M6`):** Scene parsing, deterministic AST generation, and dependency graph calculation are scheduled for future milestones.
- **Background Retention Purge (`FR-14`):** Soft-deleted and `deleting` lifecycle states are active; asynchronous purge jobs belong to the data retention milestone.

## M2 repository tests

The main verification gate includes `HostedGitTests` (path/ref/transport bounds,
raw HMAC, lifecycle, ownership recovery), `HostedRepositoryPersistenceTests`
(real PostgreSQL concurrent provisioning, atomic reconciliation intent after
removal/suspension, concurrent delivery/revision deduplication), and
`ProjectRepositoryView.spec.ts` (private creation, pending state, role gating,
escaped source, binary fallback and stale tenant responses).

Run the real provider adapter test only against disposable accounts/repositories:

```powershell
# Supply these through the process environment/ignored local configuration:
# GODFORGE_TEST_FORGEJO_URL
# GODFORGE_TEST_FORGEJO_SERVICE_TOKEN (dedicated non-admin hosting user)
# GODFORGE_TEST_FORGEJO_DEVELOPER_TOKEN
# GODFORGE_TEST_FORGEJO_VIEWER_TOKEN
# Personal test tokens need read:user plus repository access for Git operations.
dotnet test GodForge-BE/tests/GodForge.IntegrationTests --filter FullyQualifiedName~RealForgejoTests
```

Without these variables the adapter test explicitly skips. Run the disposable
harness to supply them automatically; the adapter suite has executed with one
pass and zero skips against pinned Forgejo 15.0.11:

```powershell
npm.cmd run test:m2:provider
npm.cmd run test:m2:acceptance
```

The full suite starts an owned Compose project, generates non-admin service and
distinct user accounts, registers GodForge users via Mailpit and exercises HTTP
202/outbox/RabbitMQ/Worker provisioning, managed hook installation, actual Git
HTTPS/SSH pushes, signed provider delivery, revision/browse APIs, replay,
permission downgrade/removal, concurrent changes, provider outage and restart.
It also runs a trusted disposable HTTPS linked fixture and three real-provider
browser cases. Tests refuse to reuse an existing project; cleanup removes only
owned resources. Temporary credentials remain in process memory/ignored fixture
files. No production credentials or retained developer databases are used.

`M2WorkerConcurrencyTests` pauses provider calls while membership mutations
commit, checks there are no idle database transactions, rejects superseded
leases/generations and tests durable retry exhaustion and persistence failure
after an external side effect. These PostgreSQL tests supplement real-provider
acceptance; their controlled provider is not external revocation evidence.

`npm.cmd run test:workspace:live` now runs the existing 41-assertion backend
smoke suite against its own disposable ports after browser tests. It does not
restart or replace an existing developer backend on port 15072. The standalone
smoke script also accepts loopback-only `GODFORGE_SMOKE_API` and
`GODFORGE_SMOKE_MAILBOX` overrides.

Fresh regression commands: `npm.cmd run verify`, `npm.cmd run verify:backend`
and `npm.cmd run test:workspace:live`. The full gate passed 305 backend unit,
163 PostgreSQL integration, 155 frontend and 22 controlled browser cases. The
subsequent backend gate includes the additional crash/UTC tests (165 PostgreSQL
passes). The live gate passed 10 browser cases and 41 smoke assertions. NuGet
audit has zero findings; npm has no High/Critical findings. Source-snapshot
Gitleaks finds no leaks. Global whitespace and documentation links pass,
including the corrected `.env.docker` EOF.

The real application workflow has demonstrated HTTPS/SSH revocation, automatic
push delivery, replay, linked browsing and outage recovery. Its first browser
run passed two cases and failed an exact-label locator; the locator is corrected
and the full suite must pass before acceptance is closed. Trivy database access
has encountered registry timeouts/HTTP2 errors; no successful runtime CVE scan or
remote CI run is claimed while verification is outstanding.
