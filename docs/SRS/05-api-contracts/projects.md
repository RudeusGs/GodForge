# M1 API Contract - Projects and Project Membership

Base path: `/api/v1`  
Requirements: `FR-03`, `FR-03.1`, `FR-27.1`, `FR-27.2`, `FR-27.3`  
Acceptance criteria: `AC-FR-03-*` and related `AC-FR-27.*` in `../03-functional/project.md`

## Common DTOs

```json
// ProjectSummary
{
  "id": "uuid",
  "organizationId": "uuid",
  "slug": "game-project",
  "name": "Game Project",
  "description": "Godot project",
  "visibility": "private",
  "status": "active",
  "currentUserRole": "projectOwner",
  "createdAt": "utc",
  "updatedAt": "utc",
  "version": 1
}
```

```json
// ProjectAdministrationSummary - no source/analysis content
{
  "id": "uuid",
  "organizationId": "uuid",
  "slug": "game-project",
  "name": "Game Project",
  "status": "active",
  "ownerCount": 1,
  "memberCount": 5,
  "createdAt": "utc",
  "version": 1
}
```

```json
// ProjectMemberSummary
{
  "userId": "uuid",
  "email": "user@example.com",
  "displayName": "Nguyen Van A",
  "role": "developer",
  "status": "active",
  "joinedAt": "utc",
  "version": 1
}
```

## GET `/projects`

- **Actor:** authenticated user.
- **Permission:** active project membership.
- **Query:** `page`, `pageSize`, optional `organizationId`, `status`, `search`.
- **Response:** paged `ProjectSummary[]` for projects where actor has an active project role.
- **Security:** does not include projects visible only through organization administration.
- **Errors:** `UNAUTHORIZED`, `VALIDATION_ERROR`.
- **Tests:** `TC-PROJ-LIST-001` to `TC-PROJ-LIST-005`.

## GET `/organizations/{organizationId}/projects`

- **Actor:** active organization member.
- **Permission:** OrganizationOwner/Admin receives all `ProjectAdministrationSummary`; OrganizationMember receives assigned projects only.
- **Response:** paged administration summaries without repository/source/analysis/protected-asset fields.
- **Errors:** masked `ORGANIZATION_NOT_FOUND`, `SECURITY_FORBIDDEN`.
- **Tests:** `TC-PROJ-ADMIN-LIST-001` to `TC-PROJ-ADMIN-LIST-006`.

## POST `/organizations/{organizationId}/projects`

- **Actor:** OrganizationOwner/Admin.
- **Permission:** `organizationProjects.create` and active organization membership.
- **Headers:** optional `Idempotency-Key`.
- **Request:**

```json
{
  "name": "Game Project",
  "slug": "game-project",
  "description": "Godot project",
  "visibility": "private"
}
```

- **Validation:** organization active; name/slug bounds; slug unique per organization; quota.
- **Response:** `201 Created` with `ProjectSummary` and `Location`.
- **Transaction:** create project, default settings, creator ProjectOwner membership, audit and optional outbox atomically.
- **Idempotency:** key scoped to actor/organization; unique `(organizationId, slug)` authoritative.
- **Errors:** `ORGANIZATION_NOT_FOUND`, `PROJECT_NAME_EXISTS`, `ORGANIZATION_QUOTA_EXCEEDED`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Audit:** `project.created`.
- **Tests:** `TC-PROJ-CREATE-001` to `TC-PROJ-CREATE-008`.

## GET `/projects/{projectId}`

- **Actor:** authenticated user.
- **Permission:** `projects.read` through active project membership.
- **Response:** `200 OK` with `ProjectSummary`/detail DTO.
- **Errors:** masked `PROJECT_NOT_FOUND`, `UNAUTHORIZED`.
- **Tests:** `TC-PROJ-READ-001` to `TC-PROJ-READ-005`.

## PATCH `/projects/{projectId}`

- **Actor:** ProjectOwner/Maintainer.
- **Permission:** `projects.update`.
- **Request:** `{ "name": "New Name", "slug": "new-slug", "description": "...", "visibility": "private", "version": 2 }`.
- **Validation:** mutable field allow-list; slug unique within organization; repository/asset policy consequences validated separately.
- **Response:** `200 OK` with updated `ProjectSummary`.
- **Concurrency:** required version.
- **Errors:** `PROJECT_NOT_FOUND`, `PROJECT_ARCHIVED`, `PROJECT_NAME_EXISTS`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Audit:** `project.updated`.
- **Tests:** `TC-PROJ-UPDATE-001` to `TC-PROJ-UPDATE-007`.

## DELETE `/projects/{projectId}`

- **Actor:** ProjectOwner; OrganizationOwner/Admin may use organization-administration authority where policy allows.
- **Permission:** `projects.delete` or documented organization administrative permission.
- **Request:** `{ "version": 2, "confirmationSlug": "game-project" }`.
- **Response:** `202 Accepted` for deletion/retention workflow, or `204` only when implementation explicitly performs archive rather than purge. Contracted target is `202` with job summary.
- **Transaction:** mark deleting, block mutations, write audit and outbox/job atomically.
- **Idempotency:** repeated request returns existing deletion state/job.
- **Errors:** `PROJECT_NOT_FOUND`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Audit:** `project.deletionRequested`.
- **Tests:** `TC-PROJ-DELETE-001` to `TC-PROJ-DELETE-006`.

## POST `/projects/{projectId}/restore`

- **Actor:** ProjectOwner/Maintainer or allowed organization administrator.
- **Permission:** `projects.restore`.
- **Request:** `{ "version": 3 }`.
- **Response:** `200 OK` with restored `ProjectSummary`.
- **Rules:** restore from archived state only; removed memberships are not restored automatically.
- **Errors:** `PROJECT_NOT_FOUND`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`.
- **Audit:** `project.restored`.
- **Tests:** `TC-PROJ-RESTORE-001` to `TC-PROJ-RESTORE-005`.

## POST `/projects/{projectId}/transfer-ownership`

- **Actor:** ProjectOwner.
- **Permission:** `projectMembers.transferOwnership`.
- **Request:** `{ "newOwnerUserId": "uuid", "retainCurrentOwnerAs": "maintainer", "version": 3 }`.
- **Validation:** target has active organization membership and active project membership; retained role allowed.
- **Response:** `200 OK` with updated memberships.
- **Transaction:** promote target and optionally demote actor atomically; at least one ProjectOwner remains.
- **Errors:** `PROJECT_NOT_FOUND`, `MEMBERSHIP_NOT_FOUND`, `LAST_OWNER_REQUIRED`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`.
- **Audit:** `project.ownershipTransferred`.
- **Tests:** `TC-PROJ-OWNER-001` to `TC-PROJ-OWNER-006`.

## GET `/projects/{projectId}/members`

- **Actor:** project member.
- **Permission:** `projectMembers.read`.
- **Query:** `page`, `pageSize`, optional `role`, `status`, `search`.
- **Response:** paged `ProjectMemberSummary[]`.
- **Errors:** masked `PROJECT_NOT_FOUND`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Tests:** `TC-PROJ-MEMBER-LIST-001` to `TC-PROJ-MEMBER-LIST-004`.

## POST `/projects/{projectId}/members`

- **Actor:** ProjectOwner/Maintainer.
- **Permission:** `projectMembers.add`.
- **Headers:** optional `Idempotency-Key`.
- **Request:** `{ "userId": "uuid", "role": "developer" }`.
- **Validation:** target is an active member of the project's organization; grantor may grant target role; project active.
- **Response:** `201 Created` with `ProjectMemberSummary`.
- **Transaction:** create/reactivate membership, audit and Forgejo reconciliation outbox event atomically.
- **Idempotency:** key and unique `(projectId, userId)`.
- **Errors:** `PROJECT_NOT_FOUND`, `MEMBERSHIP_NOT_FOUND`, `PROJECT_ARCHIVED`, `SECURITY_FORBIDDEN`, `CONCURRENCY_CONFLICT`, `VALIDATION_ERROR`.
- **Audit:** `project.memberAdded`.
- **Tests:** `TC-PROJ-MEMBER-ADD-001` to `TC-PROJ-MEMBER-ADD-009`.

## PATCH `/projects/{projectId}/members/{userId}`

- **Actor:** ProjectOwner/Maintainer.
- **Permission:** `projectMembers.updateRole`.
- **Request:** `{ "role": "reviewer", "version": 2 }`.
- **Rules:** Maintainer cannot manage ProjectOwner or grant above Maintainer; last-owner invariant applies.
- **Response:** `200 OK` with `ProjectMemberSummary`.
- **Transaction:** role update, audit and reconciliation outbox event atomically.
- **Errors:** `PROJECT_NOT_FOUND`, `MEMBERSHIP_NOT_FOUND`, `LAST_OWNER_REQUIRED`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Audit:** `project.memberRoleChanged`.
- **Tests:** `TC-PROJ-MEMBER-UPDATE-001` to `TC-PROJ-MEMBER-UPDATE-009`.

## DELETE `/projects/{projectId}/members/{userId}`

- **Actor:** ProjectOwner/Maintainer or self when allowed.
- **Permission:** `projectMembers.remove`.
- **Response:** `204 No Content`.
- **Transaction:** remove membership, audit and provider-reconciliation outbox event atomically.
- **Rules:** Maintainer cannot remove ProjectOwner; last ProjectOwner cannot be removed; project removal does not remove organization membership.
- **Errors:** `PROJECT_NOT_FOUND`, `MEMBERSHIP_NOT_FOUND`, `LAST_OWNER_REQUIRED`, `SECURITY_FORBIDDEN`, `CONCURRENCY_CONFLICT`.
- **Audit:** `project.memberRemoved`.
- **Tests:** `TC-PROJ-MEMBER-REMOVE-001` to `TC-PROJ-MEMBER-REMOVE-009`.

## GET `/projects/{projectId}/settings`

- **Actor:** project member.
- **Permission:** `projects.read`; sensitive configuration is filtered.
- **Response:** effective settings DTO with source scope and version.
- **Contract owner:** `FR-17.*` in `settings-policy.md`.
- **Tests:** `TC-SET-PROJ-READ-*`.

## PUT `/projects/{projectId}/settings`

- **Actor:** ProjectOwner/Maintainer.
- **Permission:** `analysis.configure` or relevant settings permission.
- **Request:** versioned typed settings DTO; no provider secrets.
- **Response:** updated effective settings DTO.
- **Concurrency:** required version.
- **Errors:** `PROJECT_NOT_FOUND`, `PROJECT_ARCHIVED`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Audit:** `project.settingsUpdated`.
- **Tests:** `TC-SET-PROJ-WRITE-*`.
