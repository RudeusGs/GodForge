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
