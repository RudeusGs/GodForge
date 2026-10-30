# Project Management Acceptance Evidence — 2026-10-30

This report concerns the local project management slice, not full M1 or release readiness. The repository started with extensive uncommitted organization/backend/frontend changes; these were preserved. Earlier organization evidence is historical and remains intact.

## Implemented behavior

Project Overview, Members and Settings are integrated with the existing organization shell. Members supports server pagination/search/role/status filters, eligible organization candidate selection beyond 100 entries, idempotent assignment/reactivation, confirmed role changes, removal and self-leave. Settings supports domain-aligned name/slug/description/visibility, separate settings concurrency, the stored analysis key/AI preference/private-or-internal asset visibility, ownership selection/transfer, archive/restore and exact-slug deletion requests. Archived information remains readable; deletion is reported as retained `deleting`.

ProjectOwner/Maintainer controls follow `RolePermissions`; only ProjectOwner receives transfer/deletion UI. Organization administrative catalog metadata grants no project-content navigation. Backend checks remain authoritative. No repository statistics, provider reconciliation, Gemini operation, notification delivery or purge completion is fabricated.

## API and persistence decisions

All existing project routes are reused. No schema change, new HTTP client, automatic migration or provider job was introduced. Mutation locks remain organization membership → organization catalog → project membership; access is reread after locks. Project aggregate, membership and settings versions are independent. Audit writes share the successful database transaction.

Corrected defects:

| Root cause | Production fix | Regression evidence |
|---|---|---|
| Assignment ignored documented optional idempotency header | Controller/command/service carry the key; existing idempotency storage uses actor/scope/key and payload hash including project. Replay rechecks current access/eligibility/state and never reactivates revoked state or duplicates audit. | Concurrent identical-key replay, changed payload, replay after removal and no-key duplicate PostgreSQL cases |
| Ownership did not validate target account status | Active non-deleted account and explicit active project/organization membership checks under existing locks | Active/suspended/removed/locked/disabled target matrix, persisted role/version/audit assertions |
| Assignment could add an inactive account | Validate active non-deleted account; member DTOs expose accountStatus so pickers exclude inactive accounts | Ineligible-target PostgreSQL matrix; inactive ownership-candidate frontend test |
| Role update of removed/suspended target could throw a domain exception | Application returns stable MEMBERSHIP_NOT_FOUND before domain transition | Removed/suspended role-update tests; explicit reactivation preserves original row and advances version |
| Equal role/join timestamps lacked unique pagination ordering | Add membership ID as final ordering key in both directories | 110-member PostgreSQL directory traversal checks no duplicates and adds an eligible candidate from page 6 |
| Browser could not resolve the role selector by exact associated label | Explicit label-for/control-ID association | Controlled and live role-change browser scenarios |
| Root package lock contained versions incompatible with package.json | Regenerate only the root lockfile against the existing declared dependency ranges | Node 22 isolated root npm ci succeeds; root npm audit reports zero vulnerabilities |
| Post-save follow-up could reload settings after unmount, or act on a later context | Capture the workspace epoch/disposal guard before member/settings mutations and guard follow-up work | New settings-save-after-unmount regression; final frontend suite passes 127 tests |

Frontend request generations clear old resources and suppress stale success/failure after navigation/unmount. Mutation closures capture project identity and versions. Duplicate submits are blocked. General conflict reloads the current version while preserving edits and requires deliberate resubmission; member conflict requires reselecting a refreshed row. Candidate selection is bounded and server revalidation remains mandatory. Non-modal action dialogs focus on open and restore their preceding connected control; existing visible-focus and responsive design tokens are reused.

## Changed source inventory

- Backend: ProjectsController; AddProjectMemberCommand; IProjectMembershipService; ProjectMembershipService; project/organization member DTOs; ProjectMemberRepository and OrganizationMemberRepository.
- Frontend: projects.api.ts and its transport tests; workspace.models.ts/workspaceState.ts; projectWorkspace.ts; ProjectSectionNav, ProjectMemberPicker, WorkspaceActionPanel; ProjectDetailView, ProjectMembersView, ProjectSettingsView; router and active navigation styling; project management/workspace unit tests.
- Tests/tooling: ProjectLifecycleHttpTests; ProjectServicesTests; ConcurrencyPersistenceTests constructor wiring; workspace.spec.ts and workspace.live.spec.ts; isolated workspace live runner; root package-lock.json synchronization.
- Documentation: implementation status/plan, backend status, frontend architecture, RBAC, quality gates, foundation evidence, project/settings SRS, project/organization API contracts, traceability and testing acceptance.

## Test mapping

| Area | Executable evidence |
|---|---|
| FR-03 project metadata/lifecycle and versions | ProjectLifecycleHttpTests; ProjectManagementViews.spec.ts; TC-PROJECT-BROWSER-002; TC-PROJECT-LIVE-004/005 |
| FR-03.1 grant boundaries/ownership/last-owner | ProjectServicesTests; PostgreSQL eligibility/role/transfer/removal tests; TC-PROJECT-BROWSER-001; TC-PROJECT-LIVE-001/002/003 |
| FR-27.1 and internal FR-27.2 | PostgreSQL target/actor revocation tests observe the lock wait before revocation commit; existing organization regression; TC-PROJECT-LIVE-006 |
| Tenant/RBAC separation | Existing TenantRoleMatrixIntegrationTests, new project role negatives, route mismatch and stale completion tests, live tenant reads/mutations/revocation |
| Settings independence/conflicts | PostgreSQL concurrent settings/archived rejection/audit tests, frontend transport/component tests, TC-PROJECT-LIVE-005 |
| Requested races | Concurrent ownership transfers; remove-versus-transfer; simultaneous roles/settings; archive-versus-settings/add; target and actor organization revocation; delete-versus-archive; duplicate adds; simultaneous owner self-removal |
| Organization/auth regression | All existing unit/integration/controlled/live tests retained; backend HTTP smoke |

Live cases use real local API/PostgreSQL/Redis/RabbitMQ/Worker/Mailpit and real OTP/invitation email delivery. No live HTTP interception is used. Each case creates distinct identities/tenants through the API. Between independent cases only `auth:rate:*` counters are reset in the explicitly fixed disposable workspace Redis; production limits stay enabled. Compose cleanup removes only that acceptance project's volumes.

## Execution record

Baseline `npm.cmd run verify` passed: 250 backend unit, 85 integration, 96 frontend unit and 19 controlled Chromium tests. A first expanded run exposed a fixture selecting an already assigned member from the last directory page; the fixture now selects an eligible newly created candidate. First live expansion encountered the shared OTP/IP budget; test fixture isolation was corrected. The next run exposed the selector association above; production markup was corrected.

Environment: Windows PowerShell; baseline Node v26.5.0; isolated verification Node v22.23.4; .NET SDK 10.0.400 targeting net10.0; PostgreSQL 16, Redis 7, RabbitMQ 3.13, Mailpit, Chromium and rebuilt .NET 10 API/Worker images. Dependency versions are locked. The first isolated root clean install found the pre-existing incompatible root package lock; after synchronization, the clean install and full pipeline passed.

| Executed command | Actual result |
|---|---|
| `npm.cmd run verify` baseline | 250 unit, 85 integration, 96 frontend and 19 controlled cases passed |
| Node 22 `artifacts/verify-project-snapshot.mjs` with `C:/Program Files/nodejs/node_modules/npm/bin/npm-cli.js` | Fresh copy of 882 source files, no inherited node_modules/bin/obj; root lockfile clean install and every stage of `npm run verify` passed. PowerShell's outer redirected invocation returned 1 after native stderr warnings; the subsequent final workspace run explicitly captures the native exit code. Backend restore/format/build (zero warnings/errors), 255 unit, 122 PostgreSQL integration, FE clean install/audit/lint/typecheck/126 unit/build, E2E clean install/audit and 21 controlled Chromium cases passed. No skipped tests. |
| Final frontend `npm.cmd run lint`, `typecheck`, `test:unit`, `build` | Passed after simplifying three explanatory UI strings; 126 tests in 17 files. The isolated source snapshot precedes those text changes, the final post-save context guard/regression and final evidence documentation. The final workspace pipeline verifies the final source separately. |
| Final workspace Node 22 `node.exe C:/Program Files/nodejs/node_modules/npm/bin/npm-cli.js run verify` | Passed with explicitly captured native exit **0**. Restore, format, build (zero warnings/errors), **255 backend unit, 122 PostgreSQL integration, 127 frontend unit and 21 controlled browser tests** passed, zero skipped; FE/E2E clean installs, audits, lint/typecheck/build and cleanup passed. This run includes the final context guard and UI text. |
| `npm.cmd run test:workspace:live` | Final run explicitly captured native exit **0**: 8 passed, 0 failed/skipped (58.5 seconds), TC-PROJECT-LIVE-001–006 plus both existing workspace/organization cases. Runtime images rebuilt from current backend source. Project-owned fixture volumes removed; subsequent compose ps is empty. |
| `docker compose -p godforge-project-smoke -f docker-compose.backend.yml up -d --build --wait`; `npm.cmd run test:backend:smoke` | All 41 HTTP assertions passed. Separate disposable project, real services and SMTP. |
| `docker compose -p godforge-project-smoke -f docker-compose.backend.yml down --volumes` | Only this task's project volumes/resources removed; subsequent compose ps was empty and repeated down returned exit 0. |
| `node ../GodForge-FE/node_modules/typescript/bin/tsc --noEmit --ignoreDeprecations 6.0` in E2E | Passed for controlled/live configs and tests |
| `dotnet list GodForge-BE/GodForge.sln package --vulnerable --include-transitive` | No vulnerable packages reported in all seven projects |
| `npm.cmd audit --audit-level=high` at root; FE and E2E audits in verify | All reported zero vulnerabilities |
| `node scripts/check-doc-links.mjs`; `git diff --check` | Passed; 124 Markdown files checked for local targets |

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Backend Unit | 255 | 0 | 0 |
| Backend Integration | 122 | 0 | 0 |
| Frontend Unit | 127 | 0 | 0 |
| Playwright Controlled | 21 | 0 | 0 |
| Playwright Live | 8 | 0 | 0 |
| Backend Smoke (HTTP assertions) | 41 | 0 | 0 |
| Root Verification (final workspace pipeline, exit 0) | 1 | 0 | 0 |

Logs remain under ignored artifacts: project-node22-verified.log, project-snapshot-metadata.json, project-final-confirmed.log, project-live-final-confirmed.log, project-smoke.log, project-dotnet-audit.log and project-root-audit.log. Snapshot SHA-256: `a4627231e4e610cedaab5fdef2eb8b66a545568a2cc665f57a7bf789ae271304`. This is a local source-copy hash, not a Git commit. Desktop/mobile project screenshots were inspected; mobile horizontal overflow assertion passed. OpenAPI generation is exercised by existing integration tests; no immutable release OpenAPI artifact is certified.

## Remaining limitations and milestone status

- **FR-17.1 remains an M1 Must gap.** The named `/users/me/settings` endpoint lacks a detailed approved typed/versioned contract. `identity.UserSetting` has no concurrency version, executable application handler or API. Supported semantics plus physical version/backfill and acceptance contract must be established before implementation. This is a current M1 gap, not a later-milestone waiver.
- **AC-FR-17.2-02 remains open.** No authoritative organization/platform policy/provider source exists. Project `sourceScope=project` is a storage marker, not effective policy precedence. Frontend hiding cannot establish this requirement.
- **FR-17.4 full acceptance remains open.** No authoritative active/compatible profile registry/selection endpoint is exposed. New immutable analysis identities depend on profile/rule registry and the M4–M6 analysis pipeline. Stored keys and settings conflict handling are implemented; unavailable profiles are not invented.
- **FR-27.2 external reconciliation remains open.** Internal GodForge authorization is revoked immediately; no durable provider consumer/verified Forgejo interaction is implemented by this slice.
- Remote clean-checkout CI, release artifact/OpenAPI generation, secret/container scans, staging acceptance, all-browser accessibility/performance testing and production readiness are separate unverified gates unless explicitly recorded below.
- The root working tree was already dirty. A local copied snapshot can test dependency/toolchain isolation but cannot be represented as a remote reviewed commit or clean Git checkout.

**PROJECT MANAGEMENT PARTIALLY VERIFIED.** The executable project workflows pass local tests, but the complete requested M1 settings acceptance still has the explicit prerequisites above. Full M1 is incomplete. Next work is M0 remote CI/security/release evidence and the M1 settings contracts/policy/profile boundary; M2 linked/hosted Git and Forgejo reconciliation follows only after applicable gates are resolved.

## Pre-push cleanup and CI review

Removed the generated Repomix export and three unreferenced frontend assets (Vue/Vite template logos and the unused hero image). Replaced the frontend template README with actual GodForge development/verification instructions. Required tests, implementation evidence and requirement documents remain. Ignored local artifacts and credentials are excluded from staging.

Removed the duplicate backend npm manifest/lockfile and nested lint-staged/commitlint configs. The nested config shadowed the root C# formatter and invoked an unavailable Prettier, causing a real pre-commit failure. Root hooks now enforce the existing dotnet formatter and frontend ESLint without a second npm toolchain. Corrected the backend README's obsolete .NET 9 statement. Secret scanning identified only two synthetic security-test fixtures; exact-line annotations document these false positives without changing test data or excluding application code.

CI now includes real-service workspace acceptance, E2E TypeScript validation, root lockfile installation/audit and a backend audit that fails for High/Critical findings. Live Playwright configuration rejects focused tests in CI. The workflow passed local actionlint validation. Post-cleanup local verification and remote Actions outcomes are reported against the pushed commit; this section does not claim full M1 or production acceptance.

A real detached clean checkout exposed a CORS integration fixture relying on a developer database configuration during Development startup. The Development CORS test now explicitly uses the shared isolated PostgreSQL fixture and its serialized collection; the HTTP preflight assertions and real startup migration behavior remain intact. No production startup check or security assertion was bypassed.
