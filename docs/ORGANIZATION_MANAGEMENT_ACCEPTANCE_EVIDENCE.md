# M1 Organization Management Acceptance Evidence — 2026-10-30

## 1. Scope Completed

This record documents the stabilization, defect remediation, regression testing, and end-to-end local acceptance verification for **M1 Organization Management and Collaboration** (`FR-27`, `FR-27.1`, `FR-27.2` internal revocation, `FR-27.3`, `FR-03`; acceptance criteria `AC-FR-27-01/02`, `AC-FR-27.1-01/02`, `AC-FR-27.2-01`, `AC-FR-27.3-01/02`, `AC-FR-03-01/02`, `AC-FR-03.1-03/04`):

- **Authenticated Workspace Shell & Navigation**:
  - `WorkspaceLayout.vue`, `OrganizationSwitcher.vue`, and `OrganizationSectionNav.vue` supporting `/dashboard`, `/organizations/:organizationId`, `/organizations/:organizationId/projects`, `/organizations/:organizationId/projects/:projectId`, `/organizations/:organizationId/members`, `/organizations/:organizationId/invitations`, `/organizations/:organizationId/settings`, and `/organization-invitations/accept`.
  - Section-preserving organization switching (`overview`, `projects`, `members`, `invitations`, `settings`), reactive switcher reloads on organization mutations via `workspaceRevision`, and strict tenant isolation across navigation and unmount.
- **Organization Overview (`/organizations/:organizationId`)**:
  - Displays organization name, slug, lifecycle status, current actor role, metadata version, assigned projects preview, and role-gated administrative actions.
- **Organization Members (`/organizations/:organizationId/members`)**:
  - Server-paginated (`pageSize = 20`) directory with search, role filter, and status filter (`removed` excluded by default unless `status=removed`).
  - Two-step inline role/status updates (`PATCH /api/v1/organizations/{id}/members/{userId}` with optimistic concurrency `version`), member removal, and self-leave (`DELETE /api/v1/organizations/{id}/members/{userId}`) with authoritative backend enforcement of `LAST_OWNER_REQUIRED` (`409 Conflict`).
- **Organization Invitations (`/organizations/:organizationId/invitations`) & Acceptance (`/organization-invitations/accept`)**:
  - Role-gated invitation creation (`POST /api/v1/organizations/{id}/invitations` with `Idempotency-Key`), server-paginated/filtered listing, idempotent revocation (`DELETE /api/v1/organizations/{id}/invitations/{invitationId}`), and one-time serialized token acceptance (`POST /api/v1/organization-invitations/accept`) with immediate URL query-parameter scrubbing and in-memory-only token handling.
- **Organization Settings (`/organizations/:organizationId/settings`)**:
  - **General Settings**: Versioned updates (`PATCH /api/v1/organizations/{id}`) aligned with backend domain validation (`name` 1–160 characters after trim, `slug` 1–80 lowercase alphanumeric characters with single interior hyphens).
  - **Ownership Transfer**: Server-paginated (`pageSize = 20`) and searchable active-member candidate selector (`GET /api/v1/organizations/{id}/members?status=active&search=…`) supporting organizations of arbitrary size (>100 members), preserving the selected candidate across page changes and searches, and executing atomic versioned transfers (`POST /api/v1/organizations/{id}/transfer-ownership`).
  - **Danger Zone**: Explicit slug-confirmed organization deletion request (`DELETE /api/v1/organizations/{id}` returning `202 Accepted` and transitioning the organization to the retained `deleting` lifecycle state).

---

## 2. Defects Discovered & Root Causes

| ID | Defect | Root Cause | Remediation |
|---|---|---|---|
| **Defect A** | **Organization Validation Mismatch** in `OrganizationSettingsView.vue` | `OrganizationSettingsView.vue` hardcoded `name.length >= 2 && name.length <= 100` (`maxlength="100"`) and `slug.length >= 3 && slug.length <= 63` (`maxlength="63"`), rejecting valid backend inputs (`Organization.MaxNameLength = 160`, `Organization.MaxSlugLength = 80`, minimum length `1` in `Organization.cs` and `SlugRules.cs`). | Extracted shared domain-aligned constants and validators (`ORGANIZATION_MAX_NAME_LENGTH = 160`, `PROJECT_MAX_NAME_LENGTH = 80`, `WORKSPACE_MAX_SLUG_LENGTH = 80`, `SLUG_PATTERN`, `validateOrganizationFields`) in `workspaceState.ts` and applied them in both `OrganizationSettingsView.vue` and `CreateWorkspaceForm.vue`. Synchronized `PATCH /organizations/{organizationId}` in `docs/SRS/05-api-contracts/organizations.md`. |
| **Defect B** | **Incorrect Last-Owner Check** in `OrganizationMembersView.vue` | `OrganizationMembersView.vue` computed `activeOwnerCount` and `isLastActiveOwner` from `data.value.members.items` (the currently loaded 20-row page under active search/role/status filters) and disabled `Leave organization` / `Remove member` when `activeOwnerCount <= 1`. If a second active owner existed on page 2 or was filtered out, the UI falsely blocked removal/self-leave. | Removed speculative client-side `activeOwnerCount` and `isLastActiveOwner` gating from `OrganizationMembersView.vue`. `canRemoveRow` now checks only actor role, active organization status, non-removed member status, and role hierarchy (`isSelf` or `canManageMember`), delegating last-owner protection to the authoritative backend (`409 LAST_OWNER_REQUIRED`) and displaying the mapped alert on conflict. |
| **Defect C** | **Ownership Target Pagination Limit** in `OrganizationSettingsView.vue` | `OrganizationSettingsView.vue` fetched ownership-transfer candidates via a single `listMembers(organizationId, { page: 1, pageSize: 100, status: 'active' })` call inside the main `useResource` fetcher, making active members beyond row 100 unreachable and offering no candidate search. | Replaced the fixed `pageSize: 100` call with a dedicated paginated (`pageSize = 20`) and searchable (`status: 'active'`, `search`) candidate loader (`loadCandidates`) with `PageControls`, debounced search input + explicit `Search candidates` / `Clear search` actions, and `selectedCandidate` preservation across page navigation and searches via `displayedCandidates`. |
| **Defect D** | **Asynchronous Form State Race** in `OrganizationSettingsView.vue` & Tenant-Switch Mutation Guards | `OrganizationSettingsView.vue` mutated `nameInput.value` and `slugInput.value` inside the `useResource` fetcher callback *before* `useResource` verified `current === generation`. Switching from Organization A (slow response) to Organization B (fast response) allowed Organization A's delayed response to overwrite Organization B's form inputs. | Extended `useResource(fetcher, onSuccess)` in `workspaceState.ts` with a `disposed` flag and a generation-guarded `onSuccess` hook that runs only when `!disposed && current === generation`. Moved all form-state initialization in `OrganizationSettingsView.vue` into `onSuccess` (with an additional `result.org.id === id.value` check and `generalDirty` preservation for same-tenant reloads). Added `activeTenantEpoch`, `candidateGeneration`, and `onBeforeUnmount` disposal guards across `OrganizationSettingsView.vue`, `OrganizationMembersView.vue`, and `OrganizationInvitationsView.vue`. |
| **Defect E** | **Inactive User Ownership Transfer Check** in `TransferOrganizationOwnershipCommandHandler.cs` | `TransferOrganizationOwnershipCommandHandler` checked `targetMembership.Status != MembershipStatus.Active` and `targetUser is null`, but did not check `targetUser.Status != UserStatus.Active` when the target user account itself was soft-deleted or locked. | Added `targetUser.Status != UserStatus.Active` validation in `TransferOrganizationOwnershipCommandHandler.cs`, returning `MEMBERSHIP_NOT_FOUND` before mutating ownership. |
| **Prior Iteration Backend & E2E Fixes Preserved** | `OrganizationMember` read permission, default `removed` exclusion, serialized invitation acceptance, invitation `Idempotency-Key`, idempotent revoke/delete, project lock on member removal, Mailpit recipient matching in `workspace.live.spec.ts` | Documented in [WORKSPACE_FOUNDATION_EVIDENCE.md](WORKSPACE_FOUNDATION_EVIDENCE.md). | All fixes preserved and re-verified in the current test runs. |

---

## 3. Files Modified

### Backend Production & Test Files
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/TransferOrganizationOwnership/TransferOrganizationOwnershipCommandHandler.cs`
- `GodForge-BE/src/GodForge.Application/Common/Security/OrganizationRolePermissions.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/AcceptOrganizationInvitation/AcceptOrganizationInvitationCommandHandler.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/CreateOrganizationInvitation/CreateOrganizationInvitationCommand.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/CreateOrganizationInvitation/CreateOrganizationInvitationCommandHandler.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/CreateOrganizationInvitation/CreateOrganizationInvitationCommandValidator.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/RemoveOrganizationMember/RemoveOrganizationMemberCommandHandler.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/RequestOrganizationDeletion/RequestOrganizationDeletionCommandValidator.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/RevokeOrganizationInvitation/RevokeOrganizationInvitationCommandHandler.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/TransferOrganizationOwnership/TransferOrganizationOwnershipCommandValidator.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/UpdateOrganization/UpdateOrganizationCommandHandler.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/UpdateOrganization/UpdateOrganizationCommandValidator.cs`
- `GodForge-BE/src/GodForge.Application/Features/Organizations/Commands/UpdateOrganizationMember/UpdateOrganizationMemberCommandValidator.cs`
- `GodForge-BE/src/GodForge.Api/Controllers/OrganizationsController.cs`
- `GodForge-BE/src/GodForge.Infrastructure/Persistence/Repositories/OrganizationMemberRepository.cs`
- `GodForge-BE/tests/GodForge.UnitTests/Application/Organizations/OrganizationManagementUnitTests.cs`
- `GodForge-BE/tests/GodForge.IntegrationTests/Organizations/OrganizationManagementHttpTests.cs`

### Frontend Production & Test Files
- `GodForge-FE/src/features/workspace/workspaceState.ts`
- `GodForge-FE/src/features/workspace/workspaceState.spec.ts`
- `GodForge-FE/src/features/workspace/CreateWorkspaceForm.vue`
- `GodForge-FE/src/features/workspace/OrganizationSwitcher.vue`
- `GodForge-FE/src/features/workspace/OrganizationSwitcher.spec.ts`
- `GodForge-FE/src/components/workspace/OrganizationSectionNav.vue`
- `GodForge-FE/src/layouts/WorkspaceLayout.vue`
- `GodForge-FE/src/views/OrganizationOverviewView.vue`
- `GodForge-FE/src/views/OrganizationProjectsView.vue`
- `GodForge-FE/src/views/OrganizationMembersView.vue`
- `GodForge-FE/src/views/OrganizationInvitationsView.vue`
- `GodForge-FE/src/views/OrganizationInvitationAcceptView.vue`
- `GodForge-FE/src/views/OrganizationSettingsView.vue`
- `GodForge-FE/src/views/OrganizationManagementViews.spec.ts`
- `GodForge-FE/src/views/WorkspaceViews.spec.ts`
- `GodForge-FE/src/api/workspace/workspace.models.ts`
- `GodForge-FE/src/api/workspace/organizations.api.ts`
- `GodForge-FE/src/api/workspace/workspace.api.spec.ts`
- `GodForge-FE/src/router/index.ts`
- `GodForge-FE/src/router/index.spec.ts`

### End-to-End Test Files
- `GodForge-E2E/tests/workspace.spec.ts`
- `GodForge-E2E/tests/workspace.live.spec.ts`

### Documentation Files
- `docs/ORGANIZATION_MANAGEMENT_ACCEPTANCE_EVIDENCE.md`
- `docs/IMPLEMENTATION_STATUS.md`
- `docs/IMPLEMENTATION_PLAN.md`
- `docs/BACKEND_FEATURE_STATUS.md`
- `docs/WORKSPACE_FOUNDATION_EVIDENCE.md`
- `docs/FRONTEND_ARCHITECTURE.md`
- `docs/SRS/03-functional/project.md`
- `docs/SRS/05-api-contracts/organizations.md`
- `docs/SRS/10-traceability.md`
- `docs/SRS/11-testing-acceptance.md`

---

## 4. Implementation & Security Decisions

1. **Shared Domain-Aligned Validation (`Defect A`)**:
   - Frontend validation rules in `workspaceState.ts` match `Organization.cs` (`MaxNameLength = 160`, `MaxSlugLength = 80`) and `SlugRules.cs` (`^[a-z0-9]+(?:-[a-z0-9]+)*$`, length 1–80). Both `CreateWorkspaceForm.vue` and `OrganizationSettingsView.vue` import these shared constants and helpers so no magic numbers are duplicated.
2. **Server-Authoritative Last-Owner Protection (`Defect B`)**:
   - Client-side member pages only see a 20-row window and cannot know the total active owner count across all pages or filters. Removing the client-side `activeOwnerCount <= 1` disable allows valid removals and self-leaves when another active owner is on a different page, while the backend's serialized membership lock (`AcquireMembershipLockAsync`) and `CountActiveOwnersAsync` authoritatively reject last-owner removal with `409 LAST_OWNER_REQUIRED`.
3. **Scalable Ownership Candidate Selection (`Defect C`)**:
   - `OrganizationSettingsView.vue` loads ownership candidates only when the current user is an active `OrganizationOwner`, using server-side pagination (`pageSize = 20`, `status = 'active'`) and `search` query filtering. When a candidate is selected on page $N$, `selectedCandidate` caches the `OrganizationMember` summary so navigating to another page or clearing search keeps the selected candidate visible in the selector and confirmation banner.
4. **Generation-Guarded Async State & Tenant Isolation (`Defect D`)**:
   - `useResource` in `workspaceState.ts` never invokes `onSuccess` for superseded requests or unmounted components. `OrganizationSettingsView.vue`, `OrganizationMembersView.vue`, and `OrganizationInvitationsView.vue` track `activeTenantEpoch` and `disposed` flags across all read and write handlers so delayed responses from a previous organization never overwrite the active organization's form inputs, candidate lists, or status banners.
5. **Invitation Token Hygiene**:
   - Raw invitation tokens are captured from `?token=` into transient in-memory module state (`consumeTransientInvitationToken`) and immediately scrubbed from the browser URL via `router.replace`. Tokens are never written to `localStorage`, `sessionStorage`, cookies, or logs, and are never returned by `GET` or `POST /organizations/{id}/invitations`.

---

## 5. Executed Verification Gates & Exact Test Counts

Environment: Windows PowerShell, Node `v26.5.0`, .NET SDK `10.0.400`, Docker `29.6.2` (`.NET 10` runtime images, disposable PostgreSQL `16-alpine`, Redis `7-alpine`, RabbitMQ `3.13-management-alpine`, Mailpit `v1.21`).

| Gate / Command | Status | Pass / Fail / Skip Counts & Details |
|---|---|---|
| `npm.cmd run verify` (repository root) | **PASSED** (exit 0) | Full automated pipeline below completed with zero errors and automatic container cleanup. |
| `node scripts/check-doc-links.mjs` | **PASSED** | Checked 122+ Markdown files; 0 broken local links. |
| `dotnet restore GodForge-BE/GodForge.sln` | **PASSED** | All 7 projects restored cleanly for `net10.0`. |
| `dotnet format GodForge-BE/GodForge.sln --verify-no-changes --no-restore` | **PASSED** | 0 formatting violations. |
| `dotnet build GodForge-BE/GodForge.sln --no-restore` | **PASSED** | `0 Warning(s)`, `0 Error(s)` across all 7 projects. |
| `dotnet test GodForge-BE/tests/GodForge.UnitTests/GodForge.UnitTests.csproj` | **PASSED** | **250 passed, 0 failed, 0 skipped** (includes 22 unit tests in `OrganizationManagementUnitTests`). |
| `dotnet test GodForge-BE/tests/GodForge.IntegrationTests/GodForge.IntegrationTests.csproj` | **PASSED** | **85 passed, 0 failed, 0 skipped** on real PostgreSQL + Redis (includes 12 comprehensive HTTP integration tests in `OrganizationManagementHttpTests` covering full RBAC matrix, 10 invitation security cases, 6 concurrency cases, boundary lengths, >100 member pagination/search/transfer, and multi-owner cross-page removal). |
| `npm.cmd ci --no-audit --no-fund` & `npm.cmd audit --audit-level=high` (`GodForge-FE`) | **PASSED** | Clean lockfile install; **0 vulnerabilities**. |
| `npm.cmd run lint` & `npm.cmd run typecheck` (`GodForge-FE`) | **PASSED** | `eslint .`, `vue-tsc --noEmit -p tsconfig.app.json`, and `tsc --noEmit -p tsconfig.node.json` passed with 0 errors. |
| `npm.cmd run test:unit` (`GodForge-FE`) | **PASSED** | **96 passed, 0 failed, 0 skipped** across **15 test files** (includes deterministic regression tests for Defects A, B, C, and D in `workspaceState.spec.ts` and `OrganizationManagementViews.spec.ts`). |
| `npm.cmd run build` (`GodForge-FE`) | **PASSED** | Production Vite bundle built in `1.14s` with 0 warnings/errors. |
| `npm.cmd ci --no-audit --no-fund` & `npm.cmd audit --audit-level=high` (`GodForge-E2E`) | **PASSED** | Clean lockfile install; **0 vulnerabilities**. |
| `npm.cmd test` (`GodForge-E2E` controlled Chromium) | **PASSED** | **19 passed, 0 failed, 0 skipped** (`TC-AUTH-BROWSER-001`–`011` and `TC-WORKSPACE-BROWSER-001`–`008`). |
| `npm.cmd run test:workspace:live` (disposable live Docker Compose stack) | **PASSED** | **2 passed, 0 failed, 0 skipped** (`TC-WORKSPACE-LIVE-001` and `TC-WORKSPACE-LIVE-002`, `16.5s`) against real `.NET 10` API + Worker, PostgreSQL, Redis, RabbitMQ, and Mailpit. |
| `npm.cmd run test:backend:smoke` (`docker-compose.backend.yml`) | **PASSED** | **41 HTTP assertions passed** across registration OTP, outbox/SMTP, organization/project creation, invitation acceptance, tenant isolation, versioned ownership transfers, archive/restore, member revocation, and session/reset flows. |
| `node GodForge-FE/node_modules/typescript/bin/tsc -p GodForge-E2E/tsconfig.json --noEmit --ignoreDeprecations 6.0` | **PASSED** | 0 TypeScript errors across Playwright configs and specs. |
| `dotnet list GodForge-BE/GodForge.sln package --vulnerable --include-transitive` | **PASSED** | All 7 backend projects report **0 vulnerable packages**. |
| `git diff --check` | **PASSED** | 0 whitespace or merge-marker errors. |

---

## 6. Security, RBAC & Concurrency Cases Verified

1. **Complete Role × Action Matrix (`OrganizationManagementHttpTests.RbacMatrix_EnforcesEveryRoleAndHierarchyRule`)**:
   - `OrganizationOwner`: Full lifecycle, settings update, ownership transfer, deletion request, member role/status management, invitation creation (`organizationAdmin` and `organizationMember`), and revocation.
   - `OrganizationAdmin`: Can update general settings, invite/remove `organizationMember`, and suspend/activate `organizationMember`; forbidden (`403 SECURITY_FORBIDDEN`) from inviting `organizationAdmin`, promoting to `organizationOwner`/`organizationAdmin`, modifying/removing `organizationOwner` or another `organizationAdmin`, transferring ownership, or requesting organization deletion.
   - `OrganizationMember`: Can read organization overview, assigned projects, and member directory, and can self-leave (when not last owner); forbidden (`403 SECURITY_FORBIDDEN`) from reading/creating/revoking invitations, updating settings, or managing other members.
   - `Suspended` / `Removed` / `Non-member`: Masked with `404 ORGANIZATION_NOT_FOUND` on organization reads; denied on all mutations and project routes (`AC-FR-27-02`, `AC-FR-27.2-01`).
2. **Invitation Security (`OrganizationManagementHttpTests.InvitationSecurity_CoversAllTenSecurityInvariants`)**:
   - Raw token never returned in `POST` or `GET /organizations/{id}/invitations`; only SHA-256 hash persisted in `core.organization_invitations`.
   - Wrong authenticated email rejected (`400 INVITE_INVALID_OR_EXPIRED`).
   - Unverified or inactive user rejected.
   - Expired, revoked, and already-consumed tokens rejected (`400 INVITE_INVALID_OR_EXPIRED`).
   - Duplicate pending invitation for same `(organizationId, normalizedEmail)` rejected (`409 INVITE_ALREADY_PENDING`).
   - `Idempotency-Key` replay with identical payload returns existing invitation without duplicate email; key reuse with different payload returns `409 IDEMPOTENCY_KEY_REUSED`.
   - Accepting an invitation when already an active member with a higher role does not downgrade existing role.
3. **Concurrency & Multi-Owner / Large-Organization Scale (`OrganizationManagementHttpTests`)**:
   - Concurrent acceptance of the same invitation token (`OnlyOneSucceeds`), concurrent last-owner removal (`PreservesAtLeastOneOwner`), concurrent ownership transfer (`OnlyOneSucceeds`), stale version rejection on `PATCH /organizations/{id}` and `PATCH /organizations/{id}/members/{userId}`, and member removal racing with project creation.
   - `MultiOwnerAndLargeOrganization_SupportsCrossPageOwnerRemoval_AndBeyond100MemberOwnershipTransfer`: Seeds 105 active members across 6 pages (`pageSize = 20`), verifies multi-owner removal across pages while preventing removal of the final remaining owner (`409 LAST_OWNER_REQUIRED`), and verifies candidate search + ownership transfer to member #102.

---

## 7. CI, Runtime Image & Security Scan Status

- **Local Runtime Images**: Multi-stage `.NET 10` (`mcr.microsoft.com/dotnet/sdk:10.0`, `aspnet:10.0`, `runtime:10.0`) images for `GodForge.Api` and `GodForge.Worker` built and passed health checks in both `docker-compose.workspace-test.yml` and `docker-compose.backend.yml`.
- **OpenAPI Specification**: Validated during `GodForge.IntegrationTests` (`IdentityOpenApiTests`) via `ISwaggerProvider.GetSwagger("v1")`.
- **Dependency Vulnerability Scans**: `dotnet list GodForge-BE/GodForge.sln package --vulnerable --include-transitive` (0 vulnerabilities across 7 projects) and `npm.cmd audit --audit-level=high` in `GodForge-FE` and `GodForge-E2E` (0 vulnerabilities) were executed locally and passed.
- **External CI & Scanner Binaries Not Claimed**:
  - `trivy` and `gitleaks` CLI binaries are not installed on this local Windows workstation (`Get-Command trivy, gitleaks` returned not found) and were therefore not executed locally.
  - A clean-checkout GitHub Actions CI run on the exact reviewed commit snapshot (targeting CI Node 22 and running container/secret scanners in CI) remains an external pipeline gate and is not claimed as complete from a local workstation run.

---

## 8. Remaining Blocked / Deferred Requirements

- **`FR-27.2` / `AC-FR-27.2-02` (External Hosted-Provider Permission Reconciliation)**:
  - Removing or suspending an organization member atomically revokes all GodForge project memberships in that organization (`AC-FR-27.2-01` verified). However, durable outbox emission and worker consumption for external Forgejo repository permission reconciliation remain deferred until the M2 hosted-Git adapter milestone is implemented, per `docs/IMPLEMENTATION_PLAN.md` ("Implement durable reconciliation required by `FR-27.2` without emitting events that have no consumer; coordinate the provider adapter with the M2 dependency").
- **`FR-03.1` & `FR-17` (Project Membership, Project Ownership Transfer, Archive/Restore & Project Settings Frontend UI)**:
  - Backend endpoints are implemented and tested, but the browser UI for project-level member management, project ownership transfer, project archive/restore, and project settings remains the next M1 frontend slice.
- **Data Retention Purge Execution**:
  - `DELETE /api/v1/organizations/{organizationId}` transitions the organization to the retained `deleting` status and blocks mutations; asynchronous background retention purge jobs belong to the data-retention milestone.

---

## 9. Reproduction Instructions

From the repository root (`d:\Project\GodForge` on Windows PowerShell with Docker Desktop running):

```powershell
# 1. Full local verification gate (docs links, Postgres/Redis containers, BE restore/format/build/250 unit + 85 integration tests, FE ci/audit/lint/typecheck/96 unit tests/build, E2E ci/audit/19 controlled Chromium tests)
npm.cmd run verify

# 2. Live browser-to-API acceptance gate (builds .NET 10 API/Worker images, starts Postgres/Redis/RabbitMQ/Mailpit, runs TC-WORKSPACE-LIVE-001 and TC-WORKSPACE-LIVE-002, and tears down all containers/volumes)
npm.cmd run test:workspace:live

# 3. Backend runtime smoke gate (41 HTTP assertions against containerized API/Worker/Postgres/Redis/RabbitMQ/Mailpit)
docker compose -f docker-compose.backend.yml up -d --build --wait
try { npm.cmd run test:backend:smoke } finally { docker compose -f docker-compose.backend.yml down }
```
