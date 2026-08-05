# Organization, Project and Membership

## Purpose

Define the M1 tenant hierarchy, organization/project lifecycle, invitations, membership, ownership and authorization boundaries.

## Actors

OrganizationOwner, OrganizationAdmin, OrganizationMember, ProjectOwner, Maintainer, Developer, Reviewer, Viewer, SystemAdmin.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-27 | Organization tenancy and organization membership | Must |
| FR-27.1 | Active organization membership is required for active project membership | Must |
| FR-27.2 | Organization membership suspension/removal revokes project membership and schedules provider reconciliation | Must |
| FR-27.3 | Effective permission intersects platform, organization, project and resource policy | Must |
| FR-03 | Project create/read/update/archive/restore | Must |
| FR-03.1 | Project membership and role management | Must |
| FR-17 | Project and user settings are owned by their dedicated settings module | Must |


## Main flow

### Organization creation

1. A verified authenticated user creates an organization with a normalized unique slug.
2. The organization and creator's active `OrganizationOwner` membership are committed atomically.
3. An audit entry records organization creation.

### Organization invitation

1. OrganizationOwner/Admin invites an email with `OrganizationMember` or allowed administrative role.
2. Invitation stores only a token hash, has expiry and may be revoked.
3. The intended verified user accepts the invitation.
4. Acceptance creates or activates one organization membership and consumes the invitation atomically.

### Project creation

1. OrganizationOwner/Admin creates a project inside an active organization.
2. The creator must be an active organization member.
3. Project, default settings and creator's `ProjectOwner` membership are committed atomically.
4. Project slug is unique within the organization.

### Project membership

1. ProjectOwner/Maintainer selects an active member of the same organization.
2. The system validates role-grant boundaries from `docs/RBAC_MATRIX.md`.
3. Project membership is created or reactivated.
4. Removal, suspension or role change emits audit/activity intent and provider-reconciliation outbox events where repository access may be affected.

### Organization membership removal

1. Authorized organization administrator requests suspension/removal.
2. Last-owner and self-removal rules are validated.
3. Organization membership and all project memberships in that organization are revoked/suspended in one transaction.
4. Outbox events schedule Forgejo permission reconciliation and notification.
5. Historical attribution remains intact.

## Error and edge cases

- Duplicate organization slug or duplicate project slug within organization.
- Invitation expired, revoked, already consumed or accepted by a different verified email.
- Project member candidate is not an active member of the same organization.
- Organization/project archived, deleting or suspended.
- Quota exceeded.
- Operation would remove the last active OrganizationOwner or ProjectOwner.
- Maintainer attempts to manage ProjectOwner or grant a role above Maintainer.
- OrganizationAdmin attempts to promote/demote/remove OrganizationOwner.
- Concurrent ownership transfer or membership change.
- Provider reconciliation delayed: GodForge authorization is revoked immediately while provider state is marked pending.

## Authorization and security

- Organization is the tenant boundary; every project row and project membership resolves to one organization.
- Client-supplied organization/project IDs are lookup inputs, never authorization evidence.
- Every project query/mutation validates current active organization and project membership in Application.
- OrganizationOwner/Admin may access minimal administration metadata for all organization projects but do not automatically receive source, analysis or protected-asset access.
- Project membership requires an active organization membership and a matching organization ID.
- Effective permission follows `docs/RBAC_MATRIX.md` and can only be reduced by higher-level policy.
- Private resource existence is masked when disclosure would leak cross-tenant information.
- Invitations store token hashes; raw tokens appear only in the delivery channel/client submission.
- Ownership transfer, administrative role changes, membership removal and project archive/delete are audited.
- Historical audit/comment/commit attribution is preserved after membership removal.

## Async processing and idempotency

- Organization/project CRUD and membership state changes are synchronous database transactions.
- Email invitation delivery, notifications and Forgejo permission reconciliation use outbox-backed asynchronous work.
- Create endpoints support `Idempotency-Key` where documented in M1 API contracts.
- Invitation acceptance, ownership transfer and membership removal use optimistic concurrency/unique constraints to prevent duplicate effects.

## Acceptance criteria

- `AC-FR-27-01`: Creating an organization creates exactly one active OrganizationOwner membership for the creator in the same transaction.
- `AC-FR-27-02`: A user cannot read private organization data outside an active membership, except audited SystemAdmin break-glass behavior.
- `AC-FR-27.1-01`: Creating/reactivating a project membership fails unless the user has an active organization membership for the project's organization.
- `AC-FR-27.1-02`: Database tenant constraints prevent a project membership from referencing a user membership in a different organization.
- `AC-FR-27.2-01`: Suspending/removing an organization member revokes all active project memberships in that organization before the transaction commits.
- `AC-FR-27.2-02`: The same removal transaction writes deduplicated outbox events for external repository permission reconciliation.
- `AC-FR-27.3-01`: Organization policy can reduce but cannot silently increase permissions granted by a project role.
- `AC-FR-27.3-02`: OrganizationOwner/Admin without a project role cannot read repository source, analysis content or protected assets for that project.
- `AC-FR-03-01`: Creating a project creates default settings and one ProjectOwner membership for the creator atomically.
- `AC-FR-03-02`: Project slug uniqueness is enforced per organization, not globally.
- `AC-FR-03-03`: Archived project rejects repository, membership and analysis mutations while remaining readable to authorized roles.
- `AC-FR-03-04`: Project restore requires permission and returns the project to the allowed active state without restoring removed memberships.
- `AC-FR-03.1-01`: A Maintainer cannot add, remove or change a ProjectOwner and cannot grant a role above Maintainer.
- `AC-FR-03.1-02`: Ownership transfer is atomic and never leaves a project without an active ProjectOwner.
- `AC-FR-03.1-03`: Membership removal takes effect for GodForge authorization immediately and preserves historical attribution.
- `AC-FR-17-01`: Project/user settings are read or changed only through the settings-policy contract and do not bypass membership or platform minimums.

## Related API

Detailed contracts:

- `../05-api-contracts/organizations.md`
- `../05-api-contracts/projects.md`

Primary routes:

- `/api/v1/organizations`
- `/api/v1/organizations/{organizationId}`
- `/api/v1/organizations/{organizationId}/members`
- `/api/v1/organizations/{organizationId}/invitations`
- `/api/v1/organization-invitations/accept`
- `/api/v1/projects`
- `/api/v1/projects/{projectId}`
- `/api/v1/projects/{projectId}/members`
- `/api/v1/projects/{projectId}/restore`

## Related data

- `core.organizations`
- `core.organization_members`
- `core.organization_invitations`
- `core.projects`
- `core.project_members`
- `core.project_settings`
- `audit.audit_logs`
- `ops.outbox_messages`

Physical design: `../04-database-m1-physical.md`.

## Tests and observability

- Test suites: `TC-TENANT-*`, `TC-ORG-*`, `TC-PROJ-*`, `TC-RBAC-*`, `TC-INVITE-*`.
- Every project route requires cross-tenant negative integration tests.
- Required concurrency tests cover last-owner protection, duplicate invitation acceptance and ownership transfer.
- Metrics: organization/project creation, invite lifecycle, membership changes, authorization denials and pending provider reconciliation.
- Audit/log fields: correlation ID, actor ID, organization ID, project ID, action, target and safe outcome; never raw invitation token.
