# M1 API Contract - Organizations

Base path: `/api/v1`  
Requirements: `FR-27`, `FR-27.1`, `FR-27.2`, `FR-27.3`  
Acceptance criteria: `AC-FR-27-*` in `../03-functional/project.md`

## Common DTOs

```json
// OrganizationSummary
{
  "id": "uuid",
  "slug": "studio-name",
  "name": "Studio Name",
  "status": "active",
  "currentUserRole": "organizationOwner",
  "createdAt": "utc",
  "updatedAt": "utc",
  "version": 1
}
```

```json
// OrganizationMemberSummary
{
  "userId": "uuid",
  "email": "user@example.com",
  "displayName": "Nguyen Van A",
  "role": "organizationMember",
  "status": "active",
  "joinedAt": "utc",
  "version": 1
}
```

```json
// OrganizationInvitationSummary
{
  "id": "uuid",
  "email": "invitee@example.com",
  "role": "organizationMember",
  "status": "pending",
  "expiresAt": "utc",
  "invitedByUserId": "uuid",
  "createdAt": "utc",
  "version": 1
}
```

## GET `/organizations`

- **Actor:** authenticated user.
- **Permission:** active account/session.
- **Query:** `page`, `pageSize`, optional `status`.
- **Response:** paged `OrganizationSummary[]` for organizations where the actor has an active membership.
- **Authorization:** SystemAdmin break-glass listing uses a separate admin route, not this endpoint.
- **Errors:** `UNAUTHORIZED`, `VALIDATION_ERROR`.
- **Tests:** `TC-ORG-LIST-001` to `TC-ORG-LIST-004`.

## POST `/organizations`

- **Actor:** authenticated verified user.
- **Permission:** platform policy allows organization creation.
- **Headers:** optional `Idempotency-Key`.
- **Request:** `{ "name": "Studio Name", "slug": "studio-name" }`.
- **Validation:** name bounded; slug lower-case, normalized and reserved-word checked.
- **Workspace form bounds:** name 1–160 characters after trim; slug 1–80 lowercase letters/digits with single interior hyphens. The server remains authoritative for reserved slugs, quotas and membership.
- **Response:** `201 Created` with `OrganizationSummary` and `Location`.
- **Transaction:** create organization, creator OrganizationOwner membership, audit log and optional outbox event atomically.
- **Idempotency:** key scoped to actor + operation; unique slug remains authoritative.
- **Rate/quota:** account/plan organization-count quota.
- **Errors:** `ORGANIZATION_SLUG_EXISTS`, `ORGANIZATION_QUOTA_EXCEEDED`, `VALIDATION_ERROR`.
- **Audit:** `organization.created`.
- **Tests:** `TC-ORG-CREATE-001` to `TC-ORG-CREATE-006`.

## GET `/organizations/{organizationId}`

- **Actor:** authenticated user.
- **Permission:** `organizations.read` through active membership.
- **Response:** `200 OK` with organization detail and current actor role.
- **Errors:** masked `ORGANIZATION_NOT_FOUND`, `UNAUTHORIZED`.
- **Tests:** `TC-ORG-READ-001` to `TC-ORG-READ-004`.

## PATCH `/organizations/{organizationId}`

- **Actor:** authenticated user.
- **Permission:** `organizations.update`.
- **Request:** `{ "name": "New Name", "slug": "new-slug", "version": 3 }`; omitted fields unchanged; at least one of `name` or `slug` must be provided.
- **Validation:** name 1–160 characters after trim; slug 1–80 lowercase letters/digits with single interior hyphens, reserved-word checked, and globally unique; `version >= 1`; status is not changed through this endpoint.
- **Response:** `200 OK` with updated `OrganizationSummary`.
- **Concurrency:** `version` required; stale version returns conflict.
- **Errors:** `ORGANIZATION_NOT_FOUND`, `ORGANIZATION_SLUG_EXISTS`, `ORGANIZATION_SLUG_RESERVED`, `ORGANIZATION_NOT_ACTIVE`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Audit:** `organization.updated` with safe changed-field list.
- **Tests:** `TC-ORG-UPDATE-001` to `TC-ORG-UPDATE-006`.

## DELETE `/organizations/{organizationId}`

- **Actor:** OrganizationOwner.
- **Permission:** `organizations.delete`.
- **Request:** `{ "version": 3, "confirmationSlug": "studio-name" }`.
- **Response:** `202 Accepted` if deletion/purge workflow is asynchronous; returns durable job summary or updated `OrganizationSummary` in `deleting` status. The M1 implementation transitions the organization to the retained `deleting` lifecycle state and blocks further mutations while retention/purge workflows remain deferred.
- **Transaction:** validate last-owner/hold/state and `version >= 1` inside serialized membership lock, mark organization `deleting`, block mutations, and write audit atomically.
- **Idempotency:** repeated request by an authorized owner with matching `confirmationSlug` when already `deleting` returns the existing `deleting` state without writing duplicate audit entries.
- **Errors:** `ORGANIZATION_NOT_FOUND`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Audit:** `organization.deletionRequested` with explicit actor confirmation.
- **Tests:** `TC-ORG-DELETE-001` to `TC-ORG-DELETE-005`.

## POST `/organizations/{organizationId}/transfer-ownership`

- **Actor:** OrganizationOwner.
- **Permission:** `organizations.transferOwnership`.
- **Request:** `{ "newOwnerUserId": "uuid", "retainCurrentOwnerAs": "organizationAdmin", "version": 3 }`.
- **Validation:** target is an active organization member with an active user account; retained role is `organizationAdmin` or `organizationMember`; `version >= 1`.
- **Response:** `200 OK` with updated current/target membership summaries (`previousOwner`, `newOwner`, `organizationVersion`).
- **Transaction:** promote target and demote actor atomically inside serialized membership lock; at least one Owner remains; organization aggregate version is incremented.
- **Errors:** `ORGANIZATION_NOT_FOUND`, `ORGANIZATION_NOT_ACTIVE`, `MEMBERSHIP_NOT_FOUND`, `LAST_OWNER_REQUIRED`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Audit:** high-value `organization.ownershipTransferred`.
- **Tests:** `TC-ORG-OWNER-001` to `TC-ORG-OWNER-006`.

## GET `/organizations/{organizationId}/members`

- **Actor:** active organization member (`OrganizationOwner`, `OrganizationAdmin`, `OrganizationMember`).
- **Permission:** `organizationMembers.read`.
- **Query:** `page`, `pageSize`, optional `role`, `status`, `search`. When `status` is omitted, `removed` memberships are excluded by default (`status=removed` returns removed rows explicitly).
- **Response:** paged `OrganizationMemberSummary[]`.
- **Security:** email/display fields follow organization-directory policy; no session/security data.
- **Errors:** masked `ORGANIZATION_NOT_FOUND`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Tests:** `TC-ORG-MEMBER-LIST-001` to `TC-ORG-MEMBER-LIST-004`.

## PATCH `/organizations/{organizationId}/members/{userId}`

- **Actor:** OrganizationOwner/Admin.
- **Permission:** `organizationMembers.updateRole`.
- **Request:** `{ "role": "organizationAdmin", "status": "active", "version": 2 }`.
- **Rules:** Admin cannot manage Owner; only Owner can grant/revoke Admin; last-owner invariant applies.
- **Response:** `200 OK` with `OrganizationMemberSummary`.
- **Transaction:** membership change, affected project revocation when suspending, audit and reconciliation outbox events atomically.
- **Errors:** `MEMBERSHIP_NOT_FOUND`, `LAST_OWNER_REQUIRED`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`.
- **Audit:** `organization.memberChanged`.
- **Tests:** `TC-ORG-MEMBER-UPDATE-001` to `TC-ORG-MEMBER-UPDATE-009`.

## DELETE `/organizations/{organizationId}/members/{userId}`

- **Actor:** OrganizationOwner/Admin or self when allowed.
- **Permission:** `organizationMembers.remove`; self-leave follows last-owner rule.
- **Headers:** optional `Idempotency-Key`.
- **Response:** `204 No Content`.
- **Transaction:** set organization membership removed, remove/suspend all project memberships in the organization, append audit and create deduplicated provider-reconciliation outbox messages atomically.
- **Errors:** `MEMBERSHIP_NOT_FOUND`, `LAST_OWNER_REQUIRED`, `SECURITY_FORBIDDEN`, `CONCURRENCY_CONFLICT`.
- **Audit:** `organization.memberRemoved` with affected-project count.
- **Tests:** `TC-ORG-MEMBER-REMOVE-001` to `TC-ORG-MEMBER-REMOVE-010`.

## GET `/organizations/{organizationId}/invitations`

- **Actor:** OrganizationOwner/Admin.
- **Permission:** `organizationMembers.invite`.
- **Query:** `page`, `pageSize`, optional `status`, `email`.
- **Response:** paged pending/recent `OrganizationInvitationSummary[]`; never returns token hash.
- **Errors:** masked `ORGANIZATION_NOT_FOUND`, `SECURITY_FORBIDDEN`.
- **Tests:** `TC-ORG-INVITE-LIST-001` to `TC-ORG-INVITE-LIST-003`.

## POST `/organizations/{organizationId}/invitations`

- **Actor:** OrganizationOwner/Admin.
- **Permission:** `organizationMembers.invite`; only Owner may invite an Admin (`organizationAdmins.manage`).
- **Headers:** optional `Idempotency-Key` (scope `organization.invitation.create`).
- **Request:** `{ "email": "invitee@example.com", "role": "organizationMember" }`.
- **Validation:** normalized email; role grant boundary; organization active; target not already an active member; pending-invitation quota.
- **Response:** `201 Created` with `OrganizationInvitationSummary`.
- **Transaction:** create hashed-token invitation, audit and encrypted email outbox atomically inside serialized invitation lock.
- **Idempotency:** same `Idempotency-Key` and identical payload returns the existing `OrganizationInvitationSummary` without sending a duplicate email; same key with different payload returns `409 IDEMPOTENCY_KEY_REUSED`; unique active pending invite per organization/email returns `409 INVITE_ALREADY_PENDING`.
- **Errors:** `INVITE_ALREADY_PENDING`, `IDEMPOTENCY_KEY_REUSED`, `ORGANIZATION_INVITATION_QUOTA_EXCEEDED`, `ORGANIZATION_NOT_ACTIVE`, `SECURITY_FORBIDDEN`, `VALIDATION_ERROR`, `CONCURRENCY_CONFLICT`.
- **Audit:** `organization.invitationCreated` without raw token.
- **Tests:** `TC-ORG-INVITE-CREATE-001` to `TC-ORG-INVITE-CREATE-007`.

## DELETE `/organizations/{organizationId}/invitations/{invitationId}`

- **Actor:** OrganizationOwner/Admin.
- **Permission:** `organizationMembers.invite`.
- **Response:** `204 No Content`.
- **Behavior:** revoke pending invitation inside serialized invitation lock; repeated revocation on a non-pending invitation is idempotent and does not write duplicate audit logs.
- **Errors:** masked `RESOURCE_NOT_FOUND`, `ORGANIZATION_NOT_ACTIVE`, `SECURITY_FORBIDDEN`.
- **Audit:** `organization.invitationRevoked`.
- **Tests:** `TC-ORG-INVITE-REVOKE-001` to `TC-ORG-INVITE-REVOKE-004`.

## POST `/organization-invitations/accept`

- **Actor:** authenticated verified user (`Active` status, verified email).
- **Permission:** invitation email matches current normalized verified email.
- **Request:** `{ "token": "one-time-secret" }`.
- **Response:** `200 OK` with `OrganizationSummary` and current `OrganizationMemberSummary`.
- **Transaction:** acquire serialized organization-membership lock, re-read invitation, verify token hash/email/expiry/active organization, create or reactivate membership without downgrading an existing active higher role, and consume invitation atomically.
- **Idempotency:** repeated or concurrent consumed token returns `INVITE_INVALID_OR_EXPIRED`; it never creates a duplicate membership or race corruption.
- **Errors:** `INVITE_INVALID_OR_EXPIRED`, `ORGANIZATION_NOT_ACTIVE`, `CONCURRENCY_CONFLICT`, `SECURITY_FORBIDDEN`.
- **Audit:** `organization.invitationAccepted`.
- **Tests:** `TC-ORG-INVITE-ACCEPT-001` to `TC-ORG-INVITE-ACCEPT-008`.


## Project candidate compatibility — 2026-10-30

Organization member summaries additionally expose accountStatus derived from the account. Existing role/status/joinedAt/version are retained. Authorized readers can exclude inactive accounts from project candidate pages; assignment rechecks eligibility server-side. Pagination now ends with a unique ID tiebreaker. [Project evidence](../../PROJECT_MANAGEMENT_ACCEPTANCE_EVIDENCE.md).
