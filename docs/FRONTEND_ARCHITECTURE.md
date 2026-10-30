# Frontend Architecture

## Stack

- Vue 3 Composition API.
- TypeScript with strict mode.
- Vite.
- Vue Router.
- Pinia.
- Axios or equivalent typed API client.
- Cytoscape.js or an approved graph library for dependency visualization.

## Structure target

```text
src/
├── api/                 # typed transport models and clients
├── components/          # reusable presentational components
├── composables/         # reusable stateful behavior
├── features/            # module-specific views/components/store
├── router/
├── stores/              # session and global UI state only
├── types/
├── utils/
└── views/
```

## State rules

- Server state is re-fetchable and must not exist only in Pinia.
- Job progress may use polling and optional SignalR, but REST job state is authoritative.
- Tokens are handled according to the chosen session strategy; never log or expose them in URLs.
- Project context includes organization ID, project ID and current revision.

## Authentication coordination

- Access tokens remain tab/runtime memory only; refresh tokens remain in the server-managed HttpOnly cookie.
- All silent refresh entry points use one coordinator. Same-tab calls share one promise.
- Cross-tab refresh prefers Web Locks and propagates authentication/invalidation with BroadcastChannel.
- If Web Locks are unavailable, the coordinator may use an atomic IndexedDB lease containing only owner/expiry metadata. A non-atomic localStorage lease is not an acceptable mutual-exclusion primitive for refresh rotation.
- Lease ownership is bounded and renewed while a leader is active so a crashed leader cannot deadlock other tabs indefinitely.
- A leader refresh failure is propagated to tabs already waiting on that attempt; waiters do not immediately create a sequential refresh storm.
- Logout/password reset/current-session revocation invalidation must win over an in-flight stale refresh result.
- Logout failure clears local authentication and blocks silent restoration until explicit sign-in, while explaining that server revocation was not confirmed. Bootstrap retry must re-evaluate both initialization and authentication state before protected content is rendered.

## Typed transport boundary

The Axios instance retains `AxiosResponse<T>` through response and refresh-retry interceptors. `baseApi` unwraps `.data` once and exposes `Promise<T>` to typed feature modules. Do not declare an interceptor-mutated generic Axios response as the payload; keeping the boundary explicit preserves compile-time checks when Axios types change. `npm run typecheck` checks both referenced application and Vite/Node projects explicitly.

## M1 workspace and organization management

Authenticated routes `/dashboard`, `/organizations/:organizationId`, `/organizations/:organizationId/projects`, `/organizations/:organizationId/projects/:projectId`, `/organizations/:organizationId/members`, `/organizations/:organizationId/invitations`, `/organizations/:organizationId/settings`, and `/organization-invitations/accept` share `layouts/WorkspaceLayout.vue`. The shell provides responsive navigation, a paginated organization switcher (preserving the active organization section on tenant switch and automatically re-fetching on organization mutations via `workspaceRevision`), horizontal organization section navigation (`components/workspace/OrganizationSectionNav.vue`), and a native keyboard-accessible account disclosure. Account sessions retain their existing protected route and behavior. Landing, auth bootstrap, refresh coordination and transport interceptors are unchanged.

`api/workspace/` types mirror executable organization, member, invitation, ownership-transfer, and project DTOs and standard `{ data, meta }` envelopes. Lists use server pagination at 20 rows. `features/workspace/` owns forms, permission helpers, error mapping, and re-fetchable request state; selections live in routes rather than persisted browser tenant data. Invitation acceptance (`/organization-invitations/accept`) captures `?token=` into transient in-memory module state and immediately scrubs the query parameter via `router.replace`, never writing raw invitation tokens to `localStorage`, `sessionStorage`, or logs, and preserving `returnTo=/organization-invitations/accept` across `/login` and `/register`.

Your projects uses the membership-scoped `/projects?organizationId=…` endpoint. Organization catalog uses `/organizations/{id}/projects` and renders only administration metadata, without content links. Creation and invitation forms retain an `Idempotency-Key` for identical retries, disable duplicate submissions, and surface safe stable errors. Shared workspace validation rules in `features/workspace/workspaceState.ts` align organization name (`1–160` characters), project name (`1–80` characters), and slug (`1–80` characters, `^[a-z0-9]+(?:-[a-z0-9]+)*$`) bounds with the backend domain model. Ownership-transfer candidate selection uses server-paginated (`pageSize = 20`) and searchable active-member queries (`GET /organizations/{id}/members?status=active&search=…`) while preserving selected candidates across page navigation. Member removal/self-leave delegates last-owner enforcement to the authoritative backend (`409 LAST_OWNER_REQUIRED`) rather than speculating from a single 20-row page. Versioned mutations (`PATCH /organizations/{id}`, `PATCH /organizations/{id}/members/{userId}`, `POST /organizations/{id}/transfer-ownership`, `DELETE /organizations/{id}`) submit the current server version, guard async responses against stale request generations and tenant switches (`useResource` `onSuccess` + `activeTenantEpoch`), and handle `409 CONCURRENCY_CONFLICT` by preserving user context and offering a clean state reload.

Loading, empty, network/retry, forbidden, masked-not-found and conflict messages are implemented. Text uses Vue escaping, controls have labels and visible focus, navigation focuses main content, mobile navigation exposes expanded state, and the loading pulse respects reduced motion. Project membership/settings UI is implemented in the addendum below. Repository/analysis/asset navigation awaits its respective milestone.

Evidence and exact reproduction: [ORGANIZATION_MANAGEMENT_ACCEPTANCE_EVIDENCE.md](ORGANIZATION_MANAGEMENT_ACCEPTANCE_EVIDENCE.md) and [WORKSPACE_FOUNDATION_EVIDENCE.md](WORKSPACE_FOUNDATION_EVIDENCE.md).

## Required UI states

Every screen must implement:

- Loading skeleton.
- Empty state.
- Permission-denied or masked-not-found state.
- Recoverable error state.
- Degraded AI state when deterministic output remains available.
- Stale/retry state for jobs.

## Performance

- Lazy-load feature routes.
- Virtualize large trees and tables.
- Never render an unbounded dependency graph; apply server/client limits and filters.
- Paginate commit, activity, finding and asset lists.
- Escape repository text and sanitize rendered Markdown/HTML.

## Accessibility

- Keyboard navigation for major workflows.
- Visible focus state.
- Semantic labels for graph controls, forms and status indicators.
- Do not encode severity by color alone.


## Project workspace management — 2026-10-30

ProjectSectionNav connects /organizations/:organizationId/projects/:projectId to /members and /settings. ProjectMembersView provides server-paginated search/filtering, assignment, confirmed role changes/removal and self-leave. ProjectSettingsView provides General, Analysis & Preferences, Ownership, Lifecycle and Danger Zone. Organization catalog authority never enables project controls. Tenant mismatch is rejected before dependent requests.

projectWorkspace.ts shares resource/mutation state with identity/epoch/unmount guards. ProjectMemberPicker pages organization or project members on the server, excludes inactive accounts and checks active duplicates. WorkspaceActionPanel provides non-modal accessible dialogs, focuses on open and restores the previous connected control. Existing focus tokens/responsive shell are reused. General edits survive conflicts while current project versions reload; member conflicts require selecting the refreshed row. Settings use their independent version and preserve supported key/notification fields. All requests use existing baseApi/Axios infrastructure. [Evidence](PROJECT_MANAGEMENT_ACCEPTANCE_EVIDENCE.md).
