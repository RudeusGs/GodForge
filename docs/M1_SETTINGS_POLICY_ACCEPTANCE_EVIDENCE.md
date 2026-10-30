# M1 Settings and Policy Acceptance Evidence

Date: 2026-10-30. Scope: FR-17.1, FR-17.2 policy enforcement, bounded FR-17.3 AI restrictions, FR-17.4 selection foundation and achievable M0 gates. Overall M1 is **PARTIALLY VERIFIED**. Full AC-FR-17.4-01 and external FR-27.2 remain open. No production-readiness claim.

## Contracts, permissions and behavior

The [typed settings contract](SRS/05-api-contracts/settings.md) defines new GET/PUT `/api/v1/users/me/settings` and `/api/v1/organizations/{organizationId}/settings` routes and additive project response fields. Actor IDs come from the session. User writes require every supported preference plus version, reject unknown properties/numeric enums, increment a separate concurrency token and atomically audit safe before/after summaries. Missing preferences initialize under a user lock and unique user FK. Notification preferences do not implement delivery.

`/account/preferences` restores persisted values, supports dirty/revert/conflict/error/loading states and applies actual workspace/account light/dark tokens. Public/auth editorial palettes remain independent. Navigation links it from account sessions and workspace. Late requests and duplicate submissions are guarded; project switches reset pending settings reads. Mobile and keyboard behavior are exercised by Chromium, and the mobile screenshot was visually inspected.

Active OrganizationOwner/Admin can administer organization policy under current tenant membership; members receive 403, outsiders masked 404. Organization roles never grant project settings permission. Gemini.Enabled is the platform capability ceiling; organization allowance defaults true but cannot override it. Project enablement under a higher restriction returns 409 SETTINGS_POLICY_RESTRICTED atomically. Responses distinguish stored/effective AI, source, restriction and role/lifecycle editability. Other project fields retain project scope. No effective-policy cache exists. Queued AI reads fresh database projections before provider/cache use and after provider response; revoked output is discarded. Already transmitted provider requests cannot be retracted. No Gemini provider operation is certified by these tests.

The only registry definition is current-default-v1, immutable version 1: the existing inventory/resource-check mode. Unknown versions and tenant-shaped keys are rejected. No genuine alternate, disabled or engine-specific definitions exist to advertise or test. AnalysisRun/persistence still lack authoritative parserVersion/ruleSetVersion/profileVersion/inputHash uniqueness. **AC-FR-17.4-01 stays open** for M4–M6 version propagation and historical/duplicate execution tests. The older analysis-trigger profile strings describe AI advisory modes, not deterministic registry definitions. No alternate profiles or completed analyses were fabricated.

## Schema and migration safety

Migration `20261030055044_VersionedPreferencesAndOrganizationPolicy` adds a required bigint user-settings version, constant default/backfill 1 and positive constraint, preserving values/IDs/timestamps/unique scope. It adds typed core.organization_settings with UUID id, unique restricted organization FK, allowance default true, independent positive version and UTC timestamps. Existing organizations receive no hidden backfill; authorized initialization uses the shared organization-membership lock. No unrelated identity columns, retained volumes, production data or ownership are changed. Production rollout remains a controlled forward migration. Retain additive schema on application rollback; correct via a new migration.

SettingsMigrationTests applies empty and prior-schema (`20260814133000_AddTrigramSearchIndexes`) upgrades in random disposable databases. Dark/Weekly/meaningful flags and timestamps survive; version backfills to 1; no policy rows are silently created. Tests verify no pending model differences, PostgreSQL uniqueness, actual EF stale writes and positive-version constraints. HTTP tests verify concurrent initialization/update and atomic audit. Existing main integration fixture uses only the separate disposable verification database.

## Executed checks

Starting commit: `a3418a328f61882249fee6c629952ba3b5e256de`, initially clean working tree. Local logs/screenshots are under ignored artifacts/.

| Check | Observed result before final review |
|---|---|
| Baseline npm.cmd run verify | Backend build 0 warnings/errors; 255 unit and 122 integration passed. Frontend npm ci hit EPERM on Vite's locked native binary. Package restoration succeeded without stopping the developer server or changing npm lockfiles. |
| Focused settings/OpenAPI | 15 unit, 28 integration passed; 0 failed/skipped. |
| Subsequent full backend | 263 unit, 138 integration passed; 0 failed/skipped. Final counts below include later regressions. |
| Frontend | 134 unit passed, lint/typecheck passed; final checks below include additional project-policy/navigation regressions. |
| Controlled Chromium | 21 existing cases passed; new settings case included in final run below. |
| Live Chromium | Initial 8 passed/2 failed on exact select labels; production explicit label associations fixed it. Rerun: **10 passed**, 0 failed/skipped, including all existing organization/project flows and real preference/policy cases. |
| Runtime stack | docker compose -f docker-compose.backend.yml up -d --build --wait: API/Worker/PostgreSQL/Redis/RabbitMQ/Mailpit healthy. |
| npm.cmd run test:backend:smoke | **41 HTTP assertions passed**. |
| Trivy 0.69.3 | Both images initially had HIGH CVE-2026-111497 in libbrotli1. Explicit installation upgrades to fixed 1.1.0-2ubuntu0.1. Rebuilt API rescan: zero HIGH/CRITICAL. Worker final rescan below. No suppression. |

## Remote Actions evidence

The connector returned Unknown tool; the public Actions API was accessible. Run [40998258136](https://github.com/RudeusGs/GodForge/actions/runs/40998258136), exact starting SHA above: **failure**. Runtime, frontend, controlled browser, live, documentation and secret jobs passed. backend-quality-gate failed at Test; Format/Vulnerability steps were skipped. The job-log API returned 403 without credentials, so the exact remote failing test is unavailable. This is starting-commit evidence, not verification of changed code. CI now retains backend TRX on all outcomes, uses locked restore and adds API/Worker HIGH/CRITICAL container scans. No changed-snapshot remote success is claimed.

## Redis and security boundaries

Redis configuration and authentication limiter were preserved: direct Windows API uses its published localhost endpoint, container API/Worker use redis:6379, isolated tests localhost:16379, and live API its separate Compose Redis service. Service health checks wait for readiness. No distributed authentication limiter was disabled, made fail-open or changed. No secret/provider key is accepted or exposed through settings. See [local development](LOCAL_DEVELOPMENT.md). Existing session refresh/revocation and tenant workflows remain regression covered.

## Remaining requirements and acceptance

- M1 settings slice: **PARTIALLY VERIFIED** overall because full FR-17.4 execution acceptance remains dependent on M4–M6. Own preferences, bounded AI policy and single-profile selection are locally executable.
- Overall M1: **PARTIALLY VERIFIED**; full analysis identity and mandatory FR-27.2 Forgejo reconciliation remain unresolved.
- M0 remote CI: **BLOCKED** on changed-snapshot remote execution and access to the earlier backend failure detail. Local worktree results cannot close remote acceptance.
- M2 readiness: **BLOCKED** under the milestone exit rule until M0 evidence is accepted. Actual Forgejo adapter/durable consumer/provider verification remains M2 work; settings emits no unconsumed permission messages.
- Broader FR-17.3 quota/provider/asset policy expansion is not implemented by this bounded AI policy.
- Production-only: controlled rollout, staging/provider exercise, release operations, metric export, backup/restore and production security/reliability acceptance remain open. No new Critical/High source defect is knowingly left unresolved; final scans must pass before claiming their local gates.

## Final local verification

Final clean-worktree checks and scan results are pending; actual counts and snapshot identity will be recorded here after execution. Local verification remains distinct from remote CI and production release.
