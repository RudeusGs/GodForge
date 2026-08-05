# 4A. M1 Physical Database Design

## 1. Scope and authority

This document is the implementation-ready PostgreSQL design for M1 Identity and Tenancy. It refines the logical model in `04-database.md`.

Covered requirements:

- `FR-01.1` to `FR-01.6`
- `FR-27`, `FR-27.1`, `FR-27.2`, `FR-27.3`
- `FR-03`, `FR-03.1`
- M1 subset of `FR-17`

Covered schemas:

- `identity`
- `core`
- `audit`
- `ops`

The exact EF class/property names may differ, but SQL ownership, nullability, uniqueness, foreign keys, indexes and state invariants must remain equivalent.

## 2. Global physical conventions

- Primary keys: `uuid`, generated server-side/application-side; clients cannot choose IDs unless an import contract explicitly allows it.
- Time: `timestamptz` in UTC.
- Normalized email: `varchar(320)` lower-cased using one documented normalization function.
- Slug: lower-case `varchar(80)` with application validation and database check where practical.
- Optimistic concurrency: explicit `version bigint NOT NULL DEFAULT 1`; every successful mutable update increments it.
- Soft lifecycle: business status plus `deleted_at` where retention requires a tombstone.
- Foreign keys default to `ON DELETE RESTRICT`. Cascade is used only for disposable dependent rows documented below.
- Sensitive secrets are stored as cryptographic hashes or encrypted vault references, never raw values.
- Partial unique indexes implement “one active/pending” rules.
- All tenant queries start from an authenticated actor membership and include `organization_id`/`project_id` in predicates.

Recommended PostgreSQL schemas:

```sql
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS core;
CREATE SCHEMA IF NOT EXISTS audit;
CREATE SCHEMA IF NOT EXISTS ops;
```

## 3. State values

Use strongly typed application enums persisted as bounded strings or small integers with database check constraints. Names below are canonical API/domain values.

### UserStatus

`pending`, `active`, `locked`, `disabled`, `deleted`

### ChallengePurpose

`registration`, `passwordReset`, `emailChange`

### OrganizationStatus

`active`, `suspended`, `deleting`, `deleted`

### OrganizationRole

`organizationOwner`, `organizationAdmin`, `organizationMember`

### MembershipStatus

`active`, `suspended`, `removed`

### ProjectStatus

`active`, `archived`, `deleting`, `deleted`

### ProjectRole

`projectOwner`, `maintainer`, `developer`, `reviewer`, `viewer`

### InvitationStatus

Derived from timestamps: `pending`, `accepted`, `revoked`, `expired`. Do not store a second mutable status when timestamps can be authoritative.

## 4. Identity tables

### 4.1 `identity.users`

Purpose: durable user account and authentication state.

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK |
| `email` | varchar(320) | no | display/canonical email |
| `email_normalized` | varchar(320) | no | globally unique |
| `password_hash` | text | no | approved adaptive hash |
| `display_name` | varchar(120) | no | trimmed/bounded |
| `status` | varchar(24) | no | UserStatus check |
| `email_verified_at` | timestamptz | yes | required for active normal user |
| `failed_login_count` | integer | no | default 0, non-negative |
| `locked_until` | timestamptz | yes | temporary lock |
| `security_stamp` | uuid | no | changes on password/security reset |
| `last_login_at` | timestamptz | yes | informational |
| `version` | bigint | no | default 1 |
| `created_at` | timestamptz | no | |
| `updated_at` | timestamptz | no | |
| `deleted_at` | timestamptz | yes | retention/anonymization workflow |

Constraints/indexes:

```sql
UNIQUE (email_normalized)
CHECK (failed_login_count >= 0)
INDEX (status)
INDEX (locked_until) WHERE locked_until IS NOT NULL
```

Deletion: never hard-delete while referenced by audit/history. A purge process anonymizes permitted personal fields.

### 4.2 `identity.auth_challenges`

Purpose: hashed OTP and password-reset challenges.

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK |
| `purpose` | varchar(24) | no | ChallengePurpose check |
| `email_normalized` | varchar(320) | no | target identity |
| `user_id` | uuid | yes | FK users; null for pre-registration |
| `secret_hash` | varchar(128) | no | unique enough; never raw secret |
| `attempt_count` | integer | no | default 0 |
| `max_attempts` | integer | no | positive |
| `expires_at` | timestamptz | no | |
| `consumed_at` | timestamptz | yes | single use |
| `revoked_at` | timestamptz | yes | |
| `requested_ip_hash` | varchar(128) | yes | privacy-safe abuse signal |
| `created_at` | timestamptz | no | |

Constraints/indexes:

```sql
UNIQUE (secret_hash)
CHECK (attempt_count >= 0 AND max_attempts > 0)
INDEX (email_normalized, purpose, created_at DESC)
INDEX (expires_at) WHERE consumed_at IS NULL AND revoked_at IS NULL
```

Application invariant: at most one usable challenge per `(email_normalized, purpose)` within resend policy. Implement with transaction/advisory lock or a partial uniqueness strategy using an `active_key` if required.

Retention: purge expired/consumed challenge rows after security retention window; retain aggregate security events.

### 4.3 `identity.user_sessions`

Purpose: server-side session state independent of JWT lifetime.

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK |
| `user_id` | uuid | no | FK users, restrict |
| `device_name` | varchar(160) | yes | user-visible safe label |
| `user_agent_hash` | varchar(128) | yes | no raw long UA required |
| `ip_hash` | varchar(128) | yes | privacy-safe |
| `created_at` | timestamptz | no | |
| `last_seen_at` | timestamptz | no | |
| `expires_at` | timestamptz | no | absolute/session expiry |
| `revoked_at` | timestamptz | yes | |
| `revoke_reason` | varchar(64) | yes | bounded code |
| `version` | bigint | no | default 1 |

Indexes:

```sql
INDEX (user_id, revoked_at, expires_at)
INDEX (expires_at)
```

Retention: keep revoked session metadata for the configured security window, then purge.

### 4.4 `identity.refresh_tokens`

Purpose: one-time rotating refresh-token family.

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK |
| `user_id` | uuid | no | FK users, restrict |
| `session_id` | uuid | no | FK user_sessions, cascade allowed when session metadata purged |
| `family_id` | uuid | no | token-family identity |
| `token_hash` | varchar(128) | no | unique; never raw token |
| `issued_at` | timestamptz | no | |
| `expires_at` | timestamptz | no | |
| `revoked_at` | timestamptz | yes | |
| `revoke_reason` | varchar(64) | yes | |
| `replaced_by_token_id` | uuid | yes | self FK, restrict |
| `reuse_detected_at` | timestamptz | yes | |
| `created_ip_hash` | varchar(128) | yes | |

Constraints/indexes:

```sql
UNIQUE (token_hash)
INDEX (session_id, revoked_at, expires_at)
INDEX (family_id, issued_at)
INDEX (expires_at)
```

Rotation transaction locks the presented token row and active session. A token with `replaced_by_token_id` or `revoked_at` is never accepted.

### 4.5 `identity.login_events`

Purpose: bounded security history for login outcomes.

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK |
| `user_id` | uuid | yes | FK users, set null only after approved anonymization |
| `email_hash` | varchar(128) | yes | avoid raw unknown email |
| `outcome` | varchar(40) | no | safe code |
| `ip_hash` | varchar(128) | yes | |
| `user_agent_hash` | varchar(128) | yes | |
| `correlation_id` | varchar(100) | no | |
| `created_at` | timestamptz | no | |

Indexes: `(user_id, created_at DESC)`, `(outcome, created_at DESC)`, `(created_at)`.

### 4.6 `identity.security_events`

Purpose: append-oriented account security events such as refresh reuse, password reset and privileged session revocation.

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK |
| `user_id` | uuid | yes | subject |
| `actor_user_id` | uuid | yes | actor if authenticated |
| `event_type` | varchar(80) | no | stable code |
| `severity` | varchar(16) | no | info/warning/high/critical |
| `session_id` | uuid | yes | safe reference |
| `correlation_id` | varchar(100) | no | |
| `metadata` | jsonb | yes | safe schema; no secrets |
| `created_at` | timestamptz | no | append-only |

Indexes: `(user_id, created_at DESC)`, `(event_type, created_at DESC)`.

## 5. Organization tables

### 5.1 `core.organizations`

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK |
| `slug` | varchar(80) | no | normalized unique |
| `name` | varchar(160) | no | |
| `status` | varchar(24) | no | OrganizationStatus |
| `created_by_user_id` | uuid | no | FK users, restrict |
| `version` | bigint | no | default 1 |
| `created_at` | timestamptz | no | |
| `updated_at` | timestamptz | no | |
| `deleted_at` | timestamptz | yes | |

Constraints/indexes:

```sql
UNIQUE (slug)
INDEX (status)
```

Ownership source of truth is active `organization_members.role = organizationOwner`, not a duplicated owner column.

### 5.2 `core.organization_members`

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `organization_id` | uuid | no | FK organizations, restrict |
| `user_id` | uuid | no | FK users, restrict |
| `role` | varchar(32) | no | OrganizationRole |
| `status` | varchar(24) | no | MembershipStatus |
| `joined_at` | timestamptz | no | |
| `suspended_at` | timestamptz | yes | |
| `removed_at` | timestamptz | yes | |
| `changed_by_user_id` | uuid | yes | FK users |
| `version` | bigint | no | default 1 |
| `created_at` | timestamptz | no | |
| `updated_at` | timestamptz | no | |

Primary/constraints/indexes:

```sql
PRIMARY KEY (organization_id, user_id)
INDEX (user_id, status)
INDEX (organization_id, role, status)
```

Business invariant: every active organization has at least one active Owner. Enforce in transactional domain/application logic with row locking; database constraints alone cannot express “last owner”.

### 5.3 `core.organization_invitations`

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK |
| `organization_id` | uuid | no | FK organizations, restrict |
| `email_normalized` | varchar(320) | no | intended identity |
| `role` | varchar(32) | no | Owner invitation prohibited; Admin grant restricted |
| `token_hash` | varchar(128) | no | unique |
| `invited_by_user_id` | uuid | no | FK users, restrict |
| `expires_at` | timestamptz | no | |
| `accepted_at` | timestamptz | yes | |
| `accepted_by_user_id` | uuid | yes | FK users |
| `revoked_at` | timestamptz | yes | |
| `version` | bigint | no | default 1 |
| `created_at` | timestamptz | no | |

Constraints/indexes:

```sql
UNIQUE (token_hash)
INDEX (organization_id, email_normalized, created_at DESC)
INDEX (expires_at) WHERE accepted_at IS NULL AND revoked_at IS NULL
```

Implement one active invitation per organization/email with a partial unique index if the chosen status representation supports it, or transactionally revoke the prior pending invitation before insert.

## 6. Project tables

### 6.1 `core.projects`

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK |
| `organization_id` | uuid | no | FK organizations, restrict |
| `slug` | varchar(80) | no | unique within organization |
| `name` | varchar(160) | no | |
| `description` | varchar(2000) | yes | |
| `visibility` | varchar(24) | no | private/internal/public policy |
| `status` | varchar(24) | no | ProjectStatus |
| `created_by_user_id` | uuid | no | FK users |
| `archived_at` | timestamptz | yes | |
| `version` | bigint | no | default 1 |
| `created_at` | timestamptz | no | |
| `updated_at` | timestamptz | no | |
| `deleted_at` | timestamptz | yes | |

Constraints/indexes:

```sql
UNIQUE (organization_id, slug)
UNIQUE (id, organization_id) -- composite tenant FK target
INDEX (organization_id, status, created_at DESC)
```

### 6.2 `core.project_members`

`organization_id` is deliberately duplicated to enforce same-tenant composite foreign keys and simplify safe scoped queries.

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `project_id` | uuid | no | part of PK |
| `organization_id` | uuid | no | same as project organization |
| `user_id` | uuid | no | active organization member required |
| `role` | varchar(24) | no | ProjectRole |
| `status` | varchar(24) | no | MembershipStatus |
| `joined_at` | timestamptz | no | |
| `suspended_at` | timestamptz | yes | |
| `removed_at` | timestamptz | yes | |
| `changed_by_user_id` | uuid | yes | FK users |
| `version` | bigint | no | default 1 |
| `created_at` | timestamptz | no | |
| `updated_at` | timestamptz | no | |

Constraints/indexes:

```sql
PRIMARY KEY (project_id, user_id)
FOREIGN KEY (project_id, organization_id)
  REFERENCES core.projects (id, organization_id)
  ON DELETE RESTRICT
FOREIGN KEY (organization_id, user_id)
  REFERENCES core.organization_members (organization_id, user_id)
  ON DELETE RESTRICT
INDEX (organization_id, user_id, status)
INDEX (project_id, role, status)
INDEX (user_id, status)
```

The composite FK proves same-organization membership exists. The application transaction additionally requires `organization_members.status = active` before creating/reactivating an active project membership.

Business invariant: every active project has at least one active ProjectOwner.

### 6.3 `core.project_settings`

M1 creates a typed default settings row with every project. Later milestones may add fields through migrations.

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `project_id` | uuid | no | PK/FK projects, restrict |
| `analysis_profile_key` | varchar(80) | no | default profile key |
| `ai_advisory_enabled` | boolean | no | default false/organization policy |
| `default_asset_visibility` | varchar(32) | no | bounded value |
| `notification_policy_version` | integer | no | default 1 |
| `version` | bigint | no | default 1 |
| `created_at` | timestamptz | no | |
| `updated_at` | timestamptz | no | |

No provider secrets are stored in this table.

### 6.4 Project invitations

Project invitations are **not an M1 table**. M1 adds only active organization members directly to projects. External users must first accept an organization invitation. A later project-invitation feature requires a separate requirement/API contract and cannot weaken `FR-27.1`.

## 7. Audit and outbox

### 7.1 `audit.audit_logs`

Purpose: append-oriented business/security administration audit.

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK |
| `organization_id` | uuid | yes | tenant scope |
| `project_id` | uuid | yes | optional project scope |
| `actor_user_id` | uuid | yes | null for provider/system actor |
| `actor_type` | varchar(24) | no | user/system/provider |
| `action` | varchar(120) | no | stable event code |
| `target_type` | varchar(80) | no | |
| `target_id` | uuid | yes | |
| `outcome` | varchar(24) | no | success/denied/failed |
| `reason` | varchar(500) | yes | required for break-glass where applicable |
| `correlation_id` | varchar(100) | no | |
| `metadata` | jsonb | yes | versioned safe schema |
| `created_at` | timestamptz | no | append-only |

Indexes:

```sql
INDEX (organization_id, created_at DESC)
INDEX (project_id, created_at DESC)
INDEX (actor_user_id, created_at DESC)
INDEX (action, created_at DESC)
INDEX (correlation_id)
```

Product APIs do not update/delete audit rows. Retention/legal hold controls archival/purge.

### 7.2 `ops.outbox_messages`

Purpose: atomically record asynchronous side effects with business state.

| Column | Type | Null | Rules |
|---|---|:---:|---|
| `id` | uuid | no | PK/message ID |
| `schema_version` | integer | no | positive |
| `message_type` | varchar(160) | no | stable contract name |
| `aggregate_type` | varchar(80) | yes | |
| `aggregate_id` | uuid | yes | |
| `organization_id` | uuid | yes | |
| `project_id` | uuid | yes | |
| `correlation_id` | varchar(100) | no | |
| `deduplication_key` | varchar(200) | yes | unique when present |
| `payload` | jsonb | no | identifiers/safe data only |
| `occurred_at` | timestamptz | no | business event time |
| `available_at` | timestamptz | no | dispatch schedule |
| `published_at` | timestamptz | yes | |
| `attempt_count` | integer | no | default 0 |
| `last_error_code` | varchar(100) | yes | safe code |
| `locked_by` | varchar(120) | yes | dispatcher owner |
| `locked_until` | timestamptz | yes | lease |

Constraints/indexes:

```sql
UNIQUE (deduplication_key) WHERE deduplication_key IS NOT NULL
INDEX (published_at, available_at) WHERE published_at IS NULL
INDEX (locked_until) WHERE published_at IS NULL
INDEX (organization_id, project_id, occurred_at DESC)
```

Payloads never contain raw invitation tokens, refresh tokens, credentials or unrestricted personal/source data.

## 8. Required M1 transaction boundaries

### Register user

`auth_challenge consume + user insert + security/audit event`.

### Refresh token

`presented token revoke + replacement insert + session update + security event on reuse`.

### Create organization

`organization insert + owner membership insert + audit/outbox`.

### Accept organization invitation

`invitation consume + organization membership insert/reactivate + audit/outbox`.

### Create project

`project insert + default project_settings + creator ProjectOwner membership + audit/outbox`.

### Remove/suspend organization member

`organization_members update + all project_members update for same organization/user + audit + deduplicated Forgejo reconciliation outbox messages`.

### Transfer ownership

Target promotion and source optional demotion commit in one transaction with row locks and last-owner validation.

## 9. Required M1 query/index tests

- User lookup by normalized email uses unique index.
- Session listing filters by `user_id` without loading refresh-token rows.
- Organization list starts from `(user_id, status)` membership index.
- Project list starts from `(user_id, status)` project-membership index.
- Project member listing uses `(project_id, role, status)`.
- Removing an organization member updates project memberships with one bounded set-based statement, not N+1 commands.
- Every project read/mutation query includes project/organization scope before projection.
- Migration tests run on empty database and prior M0 schema.

## 10. Migration and rollout

1. Create schemas/types/check constraints.
2. Create identity/core tables in FK-safe order.
3. Create audit/outbox.
4. Add indexes and partial uniqueness.
5. Seed no user/organization/project business data automatically.
6. Run integration tests against PostgreSQL.
7. Apply production migration through a dedicated release step.
8. API startup verifies schema compatibility but does not perform uncontrolled multi-instance migration.

The current foundation migration does not infer organization ownership for legacy
projects. If `core.projects` already contains rows, release preparation must first
provide and review an explicit project-to-organization mapping/backfill; otherwise
the M1 tenant migration fails before changing the schema. Assigning an empty,
synthetic or creator-derived organization silently is prohibited because it is not
authorization evidence.
