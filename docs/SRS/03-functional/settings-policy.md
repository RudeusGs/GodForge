# Settings and Policies

## Purpose

Centralize organization, project and user configuration without allowing lower scopes to weaken platform minimums.

## Actors

User, ProjectOwner, Maintainer, OrganizationOwner, OrganizationAdmin.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-17.1 | User preferences | Must |
| FR-17.2 | Project analysis, AI and notification settings | Must |
| FR-17.3 | Organization quotas, provider and asset policies | Should |
| FR-17.4 | Versioned analysis profile selection | Must |

`FR-17.3` is owned only by this document.

## Main flow

1. Client requests effective settings.
2. Service resolves platform minimum, organization override, project override and user preference where applicable.
3. Response identifies effective value and source scope.
4. Authorized update validates value, scope, version and security floor.
5. Change commits with optimistic concurrency and audit intent.
6. Relevant caches and future analysis identities are invalidated/versioned.

## Error and edge cases

- Invalid or unsafe value.
- Version conflict.
- Lower scope attempts to weaken mandatory security/quota control.
- Provider disabled by organization.
- Referenced analysis profile is inactive or incompatible.
- Secret material submitted through a generic settings endpoint.

## Authorization and security

- Platform minimum security and hard quotas cannot be bypassed.
- Organization policy can reduce project capabilities but cannot grant a permission absent from the project role.
- Secrets use dedicated write-only configuration/storage and are never returned through generic settings DTOs.
- Sensitive organization/project policy changes are audited.
- Effective settings queries remain tenant scoped.

## Async processing and idempotency

- Settings reads and writes are synchronous.
- Updates use optimistic concurrency with explicit version/ETag.
- Cache invalidation and external-provider reconciliation may be emitted through outbox events.

## Acceptance criteria

- `AC-FR-17.1-01`: A user can update only their own supported preferences and receives the new concurrency version.
- `AC-FR-17.2-01`: Project settings updates require `analysis.configure` or the documented project permission.
- `AC-FR-17.2-02`: Project settings cannot enable a provider or capability disabled by organization policy.
- `AC-FR-17.3-01`: Organization policy updates require OrganizationOwner/Admin permission and cannot reduce platform security minimums.
- `AC-FR-17.3-02`: Quota/policy conflicts return a stable validation or conflict error without partial update.
- `AC-FR-17.4-01`: A new analysis after profile change uses a new versioned analysis identity.
- `AC-FR-17.4-02`: Concurrent updates with a stale version return `409 CONCURRENCY_CONFLICT`.

## Related API

- User preference endpoints under `/api/v1/users/me/settings`.
- Organization policy endpoints under `/api/v1/organizations/{organizationId}/settings`.
- Project settings endpoints under `/api/v1/projects/{projectId}/settings`.

## Related data

- `identity.user_settings`
- `core.organization_settings`
- `core.project_settings`
- Analysis profile/rule version tables
- `audit.audit_logs`
- `ops.outbox_messages`

## Tests and observability

- Test suite: `TC-SET-*` and `TC-RBAC-SET-*`.
- Test effective-value precedence, stale-version conflict and platform-minimum rejection.
- Metrics: settings updates by scope/outcome and cache-invalidation failures.
- Audit logs record safe before/after summaries without secrets.


## Executable M1 settings boundary — 2026-10-30

Final stabilization adds explicit policy error classification and authoritative reconciliation before deliberate versioned retry after uncertain saves. Edits survive failed reconciliation/stale versions; tenant changes invalidate prior requests. See the [recovery contract](../05-api-contracts/settings.md). Deterministic keys and optional AI advisory modes use separate validators without changing existing wire names. Full immutable execution acceptance remains OPEN.

FR-17.2 project reads/updates are executable through projects.read/settings.update. ProjectOwner/Maintainer persist the existing key, AI preference, private/internal default asset visibility and positive notification-policy version with independent concurrency and audit. The UI preserves the profile key/notification version without inventing choices or notification delivery. Saving a Boolean does not activate Gemini.

The [typed settings contract](../05-api-contracts/settings.md) defines executable user preferences, organization AI restrictions, platform precedence and the single existing versioned profile. User preferences use the authenticated actor, required fields, strict enum strings, unknown-field rejection, independent versions and atomic audit. The account screen applies shared workspace/account theme tokens, supports reload/revert/conflict, and describes notification delivery as unavailable. Public/auth editorial palettes remain independent.

Gemini.Enabled is the platform capability ceiling. Organization allowance defaults true but cannot override that ceiling; project enablement under either restriction fails atomically. Effective responses expose actual source, requested/effective AI and role/lifecycle editability. Other persisted project values retain project scope. No effective-policy cache exists. Queued AI is checked against fresh database projections before provider/cache use and after an in-flight provider response; denied output is discarded. This is point-in-time revalidation, not cancellation of an already transmitted provider request.

The registry exposes only current-default-v1/version 1, the existing inventory/resource-check mode. Keys are immutable definitions; unknown versions and tenant-shaped keys cannot be selected. No distinct executable alternate, disabled or engine-specific profile is fabricated. FR-17.4/AC-FR-17.4-01 remains OPEN: the existing AnalysisRun lacks parser/rule/profile/input identity and the pipeline persists per-job output. M4–M6 must add authoritative version propagation, immutable identity uniqueness and historical/duplicate execution evidence. Selection alone does not satisfy full acceptance.

AC-FR-17.1-01, AC-FR-17.2-02 and AC-FR-17.4-02 have local executable evidence in [settings acceptance evidence](../../M1_SETTINGS_POLICY_ACCEPTANCE_EVIDENCE.md). Organization quota/provider/asset policy expansion remains outside this narrowly scoped FR-17.3 implementation. FR-27.2 Forgejo reconciliation remains an M2 dependency. Full M1 stays open.
