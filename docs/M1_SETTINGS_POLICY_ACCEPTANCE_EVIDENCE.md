# M1 Settings and Policy Final Stabilization Evidence

Date: 2026-10-30. Requirements: FR-17.1, FR-17.2, bounded FR-17.3, FR-17.4 selection foundation, FR-01.4 concurrency recovery and achievable M0 gates. This record replaces intermediate results and the final-verification placeholder. No production, full immutable-analysis or Forgejo-reconciliation acceptance is claimed.

## 1. Executive summary and source identity

Organization policy saves now distinguish conflicts, forbidden access, validation rejection and uncertain write outcomes. Authoritative read reconciliation preserves edits, prevents automatic mutation replay and allows deliberate conditional retry. Tenant/disposal generations suppress stale completions. Project settings reject unknown properties. A verified remote session-revocation/refresh race is fixed with bounded fresh-state retries. AI policy is rechecked after cache lookup. Deterministic keys and optional AI advisory modes have independent validation.

- Initial HEAD: `a3418a328f61882249fee6c629952ba3b5e256de`, branch `feat/m1-workspace-management`, **dirty**, including the existing Settings implementation and lockfiles. Unrelated services/data were preserved. The older report's initial-clean statement was incorrect for this task.
- Final executable source: `1e56c7f757a95ec5e41138bd410b8bbca281c410`; Git tree `f8b564f6ada5236c08b20756aafc94f40e86f634`.
- Clean detached checkout: `artifacts/stabilization-clean`; tracked Git status was empty before verification and remained empty afterward. No existing node_modules or retained developer database was reset.
- A first copied-source run passed before the final strict project-request regression. Its 151 integration result is intermediate; **153** is the final count. A clean-checkout format failure in that regression fixture was fixed in `1e56c7f` and the full gate rerun.
- Documentation-only finalization follows that executable commit. Reproduce with `git rev-parse HEAD` and `git diff 1e56c7f HEAD --name-only`; later evidence commits must have their own CI checks. The successful source run below must not be represented as an exact check of a different commit.

Local evidence is retained under ignored `artifacts/stabilization-*`, including clean full/backend logs, TRX, live/smoke logs, build logs, scanner JSON and CI job logs. These local artifacts are not committed credentials or release deliverables.

Exact-input proof: the independent backend-only command and final API/Worker build, smoke and scans ran at `47d6381`. Its GodForge-BE tree and the final `1e56c7f` GodForge-BE tree are both `4b7cff8f0550648ebb15a205e3e592c7bb4ec39e`; Dockerfile, global.json, root Directory.Build.props and Docker context exclusions are unchanged. Only two frontend policy-panel files changed afterward. The final clean full-root command nevertheless reran backend tests at `1e56c7f`. Final live execution used a separate install directory; all **816 non-documentation source files** were compared with the clean checkout and matched after CRLF normalization (zero differing files). Thus neither copied-source nor retained image evidence assumes unproved input equivalence.

## 2. Defects fixed

| Defect | Verified cause | Change | Regression evidence |
|---|---|---|---|
| All policy failures became conflicts | Catch unconditionally set conflict=true | Classify stable codes/statuses; preserve safe validation messages; handle empty rejection bodies without throwing | Policy panel suite: seven failures reproduced before recovery fix; final 14 pass |
| Uncertain save could permanently lock form | No authoritative reconciliation or retry state | GET values/version; block until reconciliation; never auto-PUT; deliberate versioned retry | Timeout before completion, lost response after commit, failed reconciliation and retry |
| Late tenant response could overwrite current panel | Mounted-only read and disposal-only guard | Watch organization ID; guard every read/write/reconciliation generation | Save and reconciliation switching, unmount success/failure, duplicate submits |
| Historical CI remote revoke returned 409 | Refresh changed session/token optimistic versions between revoke read/save | Retry whole revoke up to three times from fresh state; recheck ownership, revoke descendants, preserve cache barrier/audit atomicity | Deterministic PostgreSQL HTTP audit barrier plus existing racing HTTP test |
| Project settings ignored unknown properties | Existing DTO permitted unmapped members | Reject unknown tenant/platform properties before mutation | Two PostgreSQL HTTP cases verify 400 and unchanged version/values/audit |
| Policy revocation during AI cache lookup | Only pre-lookup policy check | Recheck before cached output or provider use | Cache hit/miss revocation tests; revoked in-flight provider result still discarded |
| Advisory mode confused with deterministic profile | Legacy trigger field named AnalysisProfile with inline string allow-list | AiAdvisoryModes abstraction, independent validator and typed frontend alias; wire names preserved | Six concept-boundary tests plus HTTP rejection of advisory strings as deterministic keys |

No failing tests were removed/weakened, Redis fail-closed behavior was not bypassed, and no later-milestone profiles or completed analyses were fabricated.

## 3. Files modified

The executable commit includes the pre-existing Settings work requested for stabilization, not a rewrite of authentication or tenancy. Complete inventory: `git diff --name-only a3418a3 1e56c7f` (97 files).

- Frontend production: OrganizationPolicyPanel, typed settings/repository/workspace APIs, preferences/theme, account/workspace navigation and project settings presentation.
- Backend production: typed API settings contracts/controllers/schema filter; Settings services/registry/policy; project settings service; user/organization setting entities, EF mappings/repositories and additive migration; AiExecutionStateReader; RevokeUserSessionCommandHandler; trigger validator/AiAdvisoryModes; RepositoryAiAnalysisStage; DI and seven package lockfiles.
- Tests: OrganizationPolicyPanel.spec, AccountPreferencesView.spec, project-view specs, SettingsHttpTests, SettingsMigrationTests, AnalysisConceptBoundaryTests, EffectiveSettingsPolicyTests, QueuedAiPolicyTests, deterministic AuthPostgresHttpTests race, OpenAPI and existing controlled/live settings regressions.
- Verification: CI workflow diagnostics/TRX/locked restore/container gates, Dockerfile libbrotli remediation and root verification runner isolation/locked artifact build/TRX.
- Documentation: this record, implementation status/plan, backend feature evidence, quality gates, frontend/backend architecture, settings/auth/security/data/API contracts and traceability/testing documents. Evidence-only finalization additionally updates the auth retry contract.

## 4. Settings and security verification

Real PostgreSQL HTTP tests verify own-user isolation, authenticated actor derivation, required/strict user and organization fields, unsupported enums, policy/project overposting rejection, independent positive versions, concurrent initialization, stale writes and atomic audit. OrganizationOwner/Admin require current active membership; members receive 403 and outsiders/removed administrators masked 404. Organization administration does not confer project settings permission. Existing role-matrix, membership revocation, archived/deleting restrictions, authentication/session/Redis tests remain in the complete regression run.

Platform Gemini.Enabled dominates organization allowance; organization restriction dominates project preference. Preferences cannot grant capabilities. Rejected project updates leave stored settings/version and audit unchanged. Policy changes do not configure a provider/key or certify Gemini execution. No effective-policy cache exists.

Queued AI reads fresh database projections, checks current project/organization status and requested/effective policy before lookup, after lookup and after provider execution. Cache-hit/cache-miss revocation and revoked in-flight output are tested. **Already transmitted provider requests cannot be retracted.** These are point-in-time checks; atomic policy fencing through final job publication is not implemented or claimed. Provider certification, complete job publication fencing and production metrics/export remain later pipeline/release work.

## 5. Analysis profile contract

`analysisProfileKey=current-default-v1`, immutable registry version `1`, selects the only existing deterministic inventory/resource-check definition. No alternate/disabled/engine-specific profiles are invented. The legacy repository-trigger `analysisProfile` field selects optional AI advisory intent: health_overview, commit_review or architecture_review. Separate validators reject interchange without breaking supported wire fields.

Current AnalysisRun persists project/repository/snapshot/job links and lifecycle. RepositorySnapshot binds the commit; context carries inputHash, while jobs use manual trigger identity and persisted outputs are per-job. AnalysisRun does **not** persist/consume the complete parserVersion + ruleSetVersion + analysisProfileVersion + inputHash deterministic uniqueness required by ADR 0010. AI runs separately persist commit/mode/provider/model/prompt/input identity. **AC-FR-17.4-01 remains OPEN** for M4–M6 version propagation, immutable uniqueness, historical preservation and duplicate/version-change execution tests. M1 profile selection is a foundation only.

## 6. Automated verification

Environment: Windows PowerShell; Node **22.23.4**, npm **10.9.2**, .NET SDK **10.0.400** resolving the repository global.json; Docker Engine **29.6.2**, Compose **v5.3.1**. API and Worker runtime **10.0.14**; PostgreSQL 16-alpine and Redis 7-alpine are disposable verification services. Live/smoke additionally use isolated RabbitMQ and Mailpit. Chromium only; no all-browser/staging claim.

| Suite, final executable source | Passed | Failed | Skipped | Exit |
|---|---:|---:|---:|---:|
| Backend unit | 271 | 0 | 0 | 0 |
| Backend PostgreSQL integration | 153 | 0 | 0 | 0 |
| Frontend unit, 19 files | 148 | 0 | 0 | 0 |
| Controlled Playwright | 22 | 0 | 0 | 0 |
| Live Playwright | 10 | 0 | 0 | 0 |
| Backend smoke HTTP assertions | 41 | 0 | 0 | 0 |
| Root full verification | 1 gate, 594 constituent tests | 0 | 0 | 0 |

Commands and observed results:

| Command | Final result / evidence |
|---|---|
| `npm.cmd ci --no-audit --no-fund` at clean root | Exit 0, lockfile install; no developer dependency cleanup |
| `npm.cmd run verify` in clean detached checkout | Exit 0: docs, locked restore, format, build (0 warnings/errors), all backend/frontend tests, FE lint/typecheck/build, E2E typecheck, dependency audits and controlled Chromium |
| `npm.cmd run verify:backend` in clean checkout | Exit 0; independently repeats 271 unit/153 integration, format/build and seven-project audit |
| `npm.cmd run test:workspace:live` | Exit 0, 10 real-service tests, no HTTP interception; all prior org/project workflows plus settings; project-scoped cleanup completed |
| `npm.cmd run test:backend:smoke` | Exit 0, 41 HTTP assertions against the final rebuilt API/Worker images in isolated project godforge-stabilization-smoke |
| `npm.cmd audit --audit-level=high` at root, FE and E2E | Each exit 0, zero vulnerabilities reported |
| `node scripts/check-dotnet-vulnerabilities.mjs` | Exit 0; 7 projects, zero reported vulnerable packages |
| `node scripts/check-doc-links.mjs` | Exit 0, 126 Markdown files; local links only, excluding anchors |
| `git diff a3418a3 HEAD --check` | Exit 0 on finalized change |
| `docker run --rm -v <checkout>:/repo:ro -w /repo rhysd/actionlint:latest -color .github/workflows/ci.yml` | Exit 0; mandatory jobs retained, no continue-on-error |

Full-root log: artifacts/stabilization-clean-verify.log. Independent backend log: artifacts/stabilization-clean-backend.log. Live: artifacts/stabilization-live.log. Final smoke: artifacts/stabilization-final-smoke.log. PowerShell renders native stderr (Docker progress/npm deprecation notices) as NativeCommandError records; command exit codes and actual suite summaries above determine results, not progress-line color. The old EPERM lock was avoided through separate directories and Node 22; no dependency/lockfile edit was used to bypass it.

Smoke runtime startup used the repository Compose file plus an ignored override selecting godforge-api:stabilization-final and godforge-worker:stabilization-final, project godforge-stabilization-smoke, `up -d --no-build --wait`. Only that project's disposable containers/volumes were removed in finally. The live runner uses its own fixed acceptance project and refuses to replace an existing invocation. Verification infrastructure is isolated on 15433/16379; developer processes/volumes were retained.

## 7. CI verification

Repository: RudeusGs/GodForge. Branch: feat/m1-workspace-management. Draft [PR 12](https://github.com/RudeusGs/GodForge/pull/12); no merge, force push or protected-branch/settings change.

**Successful exact-source run:** [41066217337](https://github.com/RudeusGs/GodForge/actions/runs/41066217337), head SHA **1e56c7f757a95ec5e41138bd410b8bbca281c410**, conclusion **success**, all **eight** mandatory jobs completed successfully: backend, frontend, documentation, runtime smoke, controlled browser, workspace live, secrets and container security. Backend logs independently confirm 271/153 tests, no failures/skips and seven-project dependency audit; remote live logs confirm 10 passed. TRX is uploaded with always(), service logs are captured on backend failure and SDK/runtime diagnostics are recorded. No mandatory job was disabled.

Historical run [40998258136](https://github.com/RudeusGs/GodForge/actions/runs/40998258136) at a3418a3 failed because RemoteRevoke_RacingRefresh_LeavesOnlySelectedSessionRevoked received **409 Conflict**, expected **204 NoContent**. The credential helper made job **123876360611** logs available during this task; the earlier 403 is no longer an evidence blocker. Actual failure is a production optimistic-concurrency race, not guessed service readiness/SDK/migration failure. The new deterministic barrier reproduces that ordering and the bounded retry fixes it. The first stabilization run at 4dac2da also exposed fixture formatting, fixed in 47d6381. Final 1e56c7f includes the additional empty-error-body regression and has its own successful acceptance run. No older green main run is substituted.

The GitHub plugin returned Unknown tool. Authorized Git credential-helper/API access and a normal branch push were used; credentials were never printed. Evidence-only follow-up commits require their own successful run, observed separately in PR checks and the final execution report.

## 8. Security scans and images

Both targets were rebuilt from the clean executable source with `docker build --target api|worker -f GodForge-BE/Dockerfile -t godforge-api|worker:stabilization-final .` (exit 0). Current libbrotli1 **1.1.0-2ubuntu0.1** is installed in both; no vulnerability suppression was added. Runtime images run as APP_UID and retain existing restricted runtime configuration.

Scan command for each image: `docker run --rm -v /var/run/docker.sock:/var/run/docker.sock -v <artifacts>:/reports aquasec/trivy:0.69.3 image --exit-code 1 --severity HIGH,CRITICAL --scanners vuln --format json --output /reports/<image>-trivy.json <image>`.

| Image | Image ID/digest | Scan UTC | HIGH | CRITICAL | Exit |
|---|---|---|---:|---:|---:|
| godforge-api:stabilization-final | sha256:0d9cd38ad55dcd809999df5f7640f7a4b8a0d0ccffa2ba0961c4361c83a69e20 | 2026-10-30 13:37:41 | 0 | 0 | 0 |
| godforge-worker:stabilization-final | sha256:49e19bead81916ab4759c1fb40413ec6246c5f4740312de445598113eb31ec85 | 2026-10-30 13:38:09 | 0 | 0 | 0 |

These are completed feed-backed HIGH/CRITICAL scans, not an assertion of zero lower-severity vulnerabilities. Scanner version **Trivy 0.69.3**; JSON reports include image IDs/timestamps. Both final source images were scanned separately. NuGet and root/FE/E2E npm audit results are above. **Gitleaks v8.30.1**, `dir /repo --redact --no-banner`, exit 0/no leaks on isolated source; exact-source remote secret scanning also passed. Existing exact-line synthetic-fixture annotations were preserved; no new scan exemption was introduced. Final source scanning after evidence-only changes is additionally retained in the execution log.

## 9. Migration evidence

Migration `20261030055044_VersionedPreferencesAndOrganizationPolicy` was preserved. SettingsMigrationTests runs both empty-schema and upgrade-from-20260814133000 paths in random disposable databases and deletes only those databases in finally. Both cases pass in the 153-test suite, locally and remotely.

Verified: user settings backfill to positive bigint version 1; original ID, timestamps, Dark/Weekly/non-default flags and user uniqueness survive; existing project organization/name/description/Godot version/visibility/version survive; no policy rows silently backfill; unique organization scope, organization FK, positive versions and actual PostgreSQL stale EF writes for user and organization settings; complete migration chain and no pending model differences. HTTP tests cover concurrent initialization and update/audit conflicts. No retained developer/production database was migrated or reset. Production remains a controlled forward release migration, never multi-instance startup migration.

## 10. Remaining blockers and limits

- Missing bounded stabilization implementation: none identified by the executed regression/security review. No mandatory local verification/scanner failure remains.
- Remote evidence: no access blocker for the successful source run. Later commits need their own checks; the final report identifies the observed latest run.
- **FR-27.2 / AC-FR-27.2-02:** external provider reconciliation remains OPEN. Real Forgejo adapter, durable consumer/retry/reconciliation and provider-verified revocation are M2 dependencies; no unconsumed permission message or provider success is fabricated.
- **AC-FR-17.4-01:** OPEN, M4–M6 complete immutable execution dependency described above. This is separate from the working M1 profile selection foundation, not a new profile or M2 parser implementation requirement.
- M2 milestone readiness remains blocked by the unresolved M1 exit gate and its documented provider-reconciliation dependency. M0 gate closure alone cannot be represented as M1 acceptance. A scoped M2 integration must resolve that gate dependency explicitly before production integration.
- Broader FR-17.3 quota/provider/asset policies, real Gemini certification, atomic final AI publication fencing, production monitoring/metrics, staging/all-browser acceptance, controlled release rollout and backup/restore remain separate later-scope/release gates. Historical advisory output is not evidence that current policy permits a new provider call.

## 11. Acceptance decision

| Scope | Decision |
|---|---|
| M1 Settings stabilization | **PARTIALLY VERIFIED** overall; bounded preferences/policy/recovery/selection implementation and applicable executed gates pass; full FR-17.4 execution remains OPEN |
| M0 quality gates | **ACCEPTED for executable source 1e56c7f**, including clean local source, exact-source remote CI and both runtime/security scans |
| Overall M1 | **PARTIALLY VERIFIED**, not fully accepted while mandatory provider reconciliation and full analysis-profile execution acceptance remain unresolved |
| M2 Forgejo readiness | **BLOCKED** by unresolved M1 exit/provider acceptance; no Forgejo production integration added |

No new unresolved Critical/High finding was identified by these source reviews and completed scans. This is bounded verification evidence, not a production-security certification.
