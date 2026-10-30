# M1 Workspace & Organization Management evidence — 2026-10-30

Scope: user-facing organization/project workspace foundation plus end-to-end M1 Organization Management and Collaboration (`Overview`, `Projects`, `Members`, `Invitations`, `Invitation Acceptance`, and `Settings: General, Ownership, Danger Zone`). Requirements `FR-27`, `FR-27.1`, `FR-27.2` (internal revocation), `FR-27.3`, `FR-03`; acceptance criteria `AC-FR-27-01/02`, `AC-FR-27.1-01/02`, `AC-FR-27.2-01`, `AC-FR-27.3-01/02`, `AC-FR-03-01/02`, `AC-FR-03.1-03/04`. Server suites verify transaction atomicity, uniqueness, RBAC matrix, invitation security, concurrency, boundary validation, and >100-member scale/multi-owner removal. Detailed stabilization and defect remediation record: [ORGANIZATION_MANAGEMENT_ACCEPTANCE_EVIDENCE.md](ORGANIZATION_MANAGEMENT_ACCEPTANCE_EVIDENCE.md). This record does not close M0, full M1 (`FR-03.1`/`FR-17` project UI and `FR-27.2` external provider reconciliation remain open), or release acceptance.

## Delivered behavior and files

| Files | Behavior |
|---|---|
| `GodForge-FE/src/layouts/WorkspaceLayout.vue`, `features/workspace/OrganizationSwitcher.vue`, `components/workspace/OrganizationSectionNav.vue`, `router/index.ts`, `style.css` | Authenticated responsive shell, section-preserving organization switcher with reactive refresh on mutations, role-aware horizontal organization section navigation, user disclosure/logout, sessions navigation, focus/keyboard/reduced-motion behavior. |
| `GodForge-FE/src/api/workspace/workspace.models.ts`, `organizations.api.ts`, `projects.api.ts` | Executable DTO shapes, standard envelopes, paginated typed clients through `baseApi` for organizations, members, invitations, ownership transfer, deletion request, and projects. |
| `GodForge-FE/src/features/workspace/CreateWorkspaceForm.vue`, `workspaceState.ts`, `components/workspace/ResourceState.vue`, `PageControls.vue` | Validated forms, idempotent uncertain retries, permission helpers, stable error-code mapping, in-memory-only invitation token capture, loading/empty/error/permission/conflict states, pagination and stale-response suppression. |
| `GodForge-FE/src/views/DashboardView.vue`, `OrganizationOverviewView.vue`, `OrganizationProjectsView.vue`, `OrganizationMembersView.vue`, `OrganizationInvitationsView.vue`, `OrganizationInvitationAcceptView.vue`, `OrganizationSettingsView.vue`, `ProjectDetailView.vue` | Organization onboarding/list/create; organization overview; selected-organization assigned projects and separate administration catalog; server-paginated/filtered members directory with role/status management, removal and self-leave; invitation creation/listing/revocation; invitation acceptance with URL token scrubbing; organization settings (General, Ownership Transfer, Danger Zone deletion request); project create and metadata detail. |
| `GodForge-BE/src/GodForge.Application/Features/Organizations/**`, `Common/Security/OrganizationRolePermissions.cs`, `GodForge.Api/Controllers/OrganizationsController.cs`, `GodForge.Infrastructure/Persistence/Repositories/OrganizationMemberRepository.cs` | Granted `organizationMembers.read` to `OrganizationMember`; excluded `removed` members by default in `GET /organizations/{id}/members`; serialized `AcceptOrganizationInvitation` under membership lock with active-user check and role-downgrade protection; added `Idempotency-Key` support (`organization.invitation.create`) to `POST /organizations/{id}/invitations`; made `RevokeOrganizationInvitation` and `RequestOrganizationDeletion` idempotent under lock; locked affected projects during `RemoveOrganizationMember`; tightened version/slug/status validation in `UpdateOrganization`, `UpdateOrganizationMember`, and `TransferOrganizationOwnership`. |
| `GodForge-BE/tests/GodForge.UnitTests/Application/Organizations/OrganizationManagementUnitTests.cs`, `GodForge.IntegrationTests/Organizations/OrganizationManagementHttpTests.cs` | Unit and real-PostgreSQL HTTP integration tests covering the complete RBAC matrix, all 10 invitation security cases, and all 6 concurrency cases. |
| Workspace API/form/state/switcher/view specs, `views/OrganizationManagementViews.spec.ts`, and `GodForge-FE/src/router/index.spec.ts` | Transport payloads, form behavior, retry, loading, denied/masked detail, escaped text, tenant mismatch/race, pagination, protected deep links, URL token scrubbing, and organization members/invitations/acceptance/settings views. |
| `GodForge-E2E/tests/workspace.spec.ts`, `workspace.live.spec.ts`, `auth.spec.ts`, both Playwright configs | Eight controlled workspace cases (`TC-WORKSPACE-BROWSER-001`–`008`), two real local API workflows (`TC-WORKSPACE-LIVE-001`–`002`), existing auth regression compatibility (`TC-AUTH-BROWSER-001`–`011`), reduced-motion browser context. |
| `docker-compose.workspace-test.yml`, `scripts/workspace-live.mjs`, root `package.json` | Reproducible isolated local acceptance with dedicated ports and automatic fixture cleanup. |
| Functional/API/data/security/frontend/testing/traceability/status/plan/local-development/quality documentation | Scope and evidence synchronized. No database migration required. |

## Exact workspace and organization endpoints

All paths below are relative to `/api/v1` and require existing authenticated sessions:

| Method/path | Purpose |
|---|---|
| `GET /organizations?page={page}&pageSize=20` | Membership-scoped organization list and switcher. |
| `POST /organizations` | `{name, slug}` plus `Idempotency-Key`; creator receives OrganizationOwner. |
| `GET /organizations/{organizationId}` | Current organization metadata, status, role and version. |
| `PATCH /organizations/{organizationId}` | `{name, slug, version}`; updates general settings with optimistic concurrency. |
| `DELETE /organizations/{organizationId}` | `{version, confirmationSlug}`; transitions organization to retained `deleting` status (`202 Accepted`). |
| `POST /organizations/{organizationId}/transfer-ownership` | `{newOwnerUserId, retainCurrentOwnerAs, version}`; atomic ownership transfer. |
| `GET /organizations/{organizationId}/members?page={page}&pageSize=20&role=…&status=…&search=…` | Server-paginated member list for active organization members (`removed` excluded unless `status=removed`). |
| `PATCH /organizations/{organizationId}/members/{userId}` | `{role, status, version}`; role/status update with last-owner and role-hierarchy enforcement. |
| `DELETE /organizations/{organizationId}/members/{userId}` | Removes member (or self-leave) and revokes all project memberships in the organization atomically. |
| `GET /organizations/{organizationId}/invitations?page={page}&pageSize=20&status=…&email=…` | Server-paginated invitation list for OrganizationOwner/OrganizationAdmin; never returns token hashes. |
| `POST /organizations/{organizationId}/invitations` | `{email, role}` plus optional `Idempotency-Key`; creates hashed-token invitation and encrypted email outbox entry. |
| `DELETE /organizations/{organizationId}/invitations/{invitationId}` | Idempotently revokes pending invitation. |
| `POST /organization-invitations/accept` | `{token}`; serialized one-time token acceptance for matching active verified email. |
| `GET /projects?organizationId={organizationId}&page={page}&pageSize=20` | Assigned projects with actual current project role. |
| `GET /organizations/{organizationId}/projects?page={page}&pageSize=20` | Administration catalog, metadata only. |
| `POST /organizations/{organizationId}/projects` | `{name, slug, description, visibility}` plus `Idempotency-Key`; creator receives ProjectOwner. |
| `GET /projects/{projectId}` | Authorized project metadata detail; browser also checks organization context. |

Existing login/refresh/logout and `/users/me/sessions` workflows are preserved. The live fixture additionally uses real `POST /auth/send-otp` and `/auth/register`, and polls only its local Mailpit mailbox. Creates do not take an aggregate version; returned versions are preserved and detail displays the server version. No update/archive/ownership/settings mutation is added in this slice.

## Security and data decisions

- Backend Application authorization remains authoritative; role-based UI controls grant no permission. Catalog rows never link to content or infer project membership.
- Tenant selection stays in route context, never a persisted credential or authorization snapshot. Loading clears old data; request generations suppress late success/failure after navigation/unmount. Mismatched project/route tenants render the same safe unavailable state.
- Names/descriptions use Vue escaping; no raw HTML, fake analytics, production fixtures, provider calls or new token storage. Existing auth refresh coordinator/guards remain in use.
- Create retries reuse their key while the payload/form remains unchanged and disable duplicate submission. No stale version is invented. Later versioned mutations must consume fresh server versions.
- CRUD uses existing synchronous transactions/audit; no schema, new asynchronous job, Forgejo, Gemini or Asset Vault behavior. Test-only SMTP delivery uses the already existing Worker/outbox.

## Execution record

Environment: Windows PowerShell, Node `v26.5.0`, .NET SDK `10.0.400`, .NET 10 runtime images, Chromium, disposable PostgreSQL `16-alpine`, Redis `7-alpine`, RabbitMQ and Mailpit. This is a dirty working tree containing prior work; a clean-checkout CI run of the reviewed snapshot is still missing. CI targets Node 22, which is not this local Node version.

| Command | Executed result |
|---|---|
| `npm.cmd run verify` (root), first run | Failed after restore/format/build and 220 unit passes; 67 integration passes, 5 auth failures. Logs identified an unintended `localhost:6379` cache dependency. Frontend/browser stages were not reached by this first run. |
| `dotnet test GodForge-BE/tests/GodForge.IntegrationTests/GodForge.IntegrationTests.csproj --artifacts-path artifacts/workspace-diagnostic --filter FullyQualifiedName~AuthPostgresHttpTests` (root), with `GODFORGE_TEST_POSTGRES` targeting isolated `godforge_workspace_diagnostic` on port 15433 and `GODFORGE_TEST_REDIS=localhost:16379` | After fixing cache isolation, 7 passed and reset-versus-refresh returned 400. After bounded reset retry, all 8 passed, zero skipped. |
| Final combined backend restore/format/build/test through `npm.cmd run verify` | Restore and `dotnet format --verify-no-changes --no-restore` passed. Build passed with zero warnings/errors; 250 unit and 85 integration tests passed, zero skipped, using `--artifacts-path artifacts/verification-backend`. |
| Final root `npm.cmd run verify` | Passed end to end (exit 0): documentation links, isolated service startup, backend gates above, both npm clean installs/audits, FE lint/typecheck/96 unit tests/build and 19 controlled Chromium cases (`TC-AUTH-BROWSER-001`–`011`, `TC-WORKSPACE-BROWSER-001`–`008`). Cleanup completed; zero mandatory tests skipped. |
| `npm.cmd ci --no-audit --no-fund`, `npm.cmd audit --audit-level=high` in FE and E2E | Both clean installs passed; both audits reported zero vulnerabilities. |
| FE `npm.cmd run lint`, `npm.cmd run typecheck`, `npm.cmd run test:unit`, `npm.cmd run build` | Passed; 96 tests in 15 files. |
| E2E `npm.cmd test` | 19 passed, zero skipped: 11 auth and 8 workspace cases. |
| Root `npm.cmd run test:workspace:live` | Passed (exit 0), 2 live Chromium workflows (`TC-WORKSPACE-LIVE-001` and `TC-WORKSPACE-LIVE-002`), zero skipped; API/Worker images built from current source, fixture resources removed automatically. |
| `npm.cmd run test:backend:smoke` | Passed (41 HTTP assertions across registration OTP, outbox/SMTP, organization/project creation, invitation acceptance, tenant isolation, versioned ownership transfers, archive/restore, member revocation, and session/reset flows). |
| `node ../GodForge-FE/node_modules/typescript/bin/tsc --noEmit --ignoreDeprecations 6.0` in E2E | Passed for both Playwright configs and browser tests using the installed FE TypeScript 6 compiler. |
| `node scripts/check-doc-links.mjs`, `git diff --check` | Passed after documentation synchronization; link checker validates local targets, excluding anchors/remote URLs. |

## Remaining blockers and next slice

- M0 exact reviewed clean-checkout CI (including Node 22), scans, generated OpenAPI/release artifact evidence and release configuration remain unclaimed. Local runtime images do not establish release readiness.
- M1 project membership/ownership/settings browser workflows (`FR-03.1`, `FR-17`) and staging acceptance remain open.
- `FR-27.2` (`AC-FR-27.2-02`) durable external provider reconciliation and external revocation evidence remain outstanding; no provider integration was introduced.
- Next slice: project membership, project ownership transfer, project archive/restore, and project settings browser workflows (`FR-03.1`, `FR-03`, `FR-17`) with versioned conflict handling and live acceptance. Respect M0/dependency gates before provider work.


## Subsequent project management slice — 2026-10-30

The results above remain historical foundation evidence. Members/Settings routes and membership/ownership/lifecycle/settings workflows now have local live acceptance. Current commands/results and remaining M0/M1 prerequisites are in [project management acceptance evidence](PROJECT_MANAGEMENT_ACCEPTANCE_EVIDENCE.md). The organization acceptance record is preserved.
