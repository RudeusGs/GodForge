# GodForge Database Design

> **Version:** 1.0  
> **Status:** Production-grade design draft  
> **Target:** SaaS website for individual users, teams, and enterprises  
> **Primary database:** PostgreSQL  
> **Supporting stores:** Redis, RabbitMQ, MinIO/S3  

---

## Table of Contents

1. [Purpose](#40-purpose)
2. [Architecture Overview](#41-architecture-overview)
3. [PostgreSQL Schema Map](#42-postgresql-schema-map)
4. [Identity Schema](#43-identity-schema)
5. [Tenant / Organization Schema](#44-tenant--organization-schema)
6. [Billing / License Schema](#45-billing--license-schema)
7. [Core Project Schema](#46-core-project-schema)
8. [Repository / Git Schema](#47-repository--git-schema)
9. [Ops / Worker Schema](#48-ops--worker-schema)
10. [Metadata Schema](#49-metadata-schema)
11. [Analysis / Health / Graph Schema](#410-analysis--health--graph-schema)
12. [Storage / Artifact Schema](#411-storage--artifact-schema)
13. [Collaboration Schema](#412-collaboration-schema)
14. [Audit Schema](#413-audit-schema)
15. [Search Schema](#414-search-schema)
16. [Governance Schema](#415-governance-schema)
17. [Admin / Operations Schema](#416-admin--operations-schema)
18. [Redis Design](#417-redis-design)
19. [MinIO / S3 Bucket Design](#418-minio--s3-bucket-design)
20. [High-volume Tables and Partitioning](#419-high-volume-tables-and-partitioning)
21. [Data Classification](#420-data-classification)
22. [Backup, Restore, and PITR](#421-backup-restore-and-pitr)
23. [Migration Rollout Plan](#422-migration-rollout-plan)
24. [Production Acceptance Checklist](#423-production-acceptance-checklist)
25. [Implementation Phasing](#424-implementation-phasing)

---

## 4.0 Purpose

GodForge is a web platform for managing, analyzing, reviewing, and visualizing Godot projects. Its database must support both:

1. **Individual users and small teams** working on Godot projects.
2. **Organizations, studios, and enterprises** that need multi-tenancy, strict RBAC, auditability, credential security, scale, retention, billing, and operational reliability.

The database design must support:

- Authentication and user security.
- Organizations and enterprise membership.
- Project-level RBAC.
- Git repository connection and workspace tracking.
- Async worker jobs with retry, lease, idempotency, and DLQ.
- Godot metadata parsing from `.tscn`, `.tres`, `.gd`, and asset files.
- Scene Explorer, Scene Flow Table, Asset Explorer, Dependency Graph, Scene Diff, Search, and Project Health.
- Audit logs, security events, retention, purge, legal hold, backup, restore, and production migration governance.

---

## 4.1 Architecture Overview

| Component | Responsibility |
| --- | --- |
| **PostgreSQL** | Source of truth for business data, security state, project data, metadata, job state, audit, search index, and governance records. |
| **Redis** | Ephemeral cache, rate limit counters, distributed locks, short-lived job progress cache. |
| **RabbitMQ** | Async message transport for clone, fetch, parse, analyze, diff, preview, notification, and background jobs. |
| **MinIO / S3** | Large binary/object storage for diff artifacts, thumbnails, previews, reports, archives, and audit exports. |
| **Prometheus / Grafana / Loki / OpenTelemetry** | Metrics, logs, traces, and observability outside PostgreSQL. |

### Core Principles

- PostgreSQL is the source of truth for durable job state and all business data.
- RabbitMQ is transport only, not authoritative job storage.
- Redis is cache/rate limit/distributed lock only, not primary data storage.
- Redis repository locks require TTL and owner token.
- MinIO stores artifacts such as diff outputs, thumbnails, reports, previews, snapshots, and archives.
- All tables use `uuid` primary keys unless explicitly noted.
- Table and column names use `snake_case`.
- Time columns use `timestamptz` and UTC.
- No plaintext passwords, refresh tokens, invite tokens, reset tokens, API keys, or secrets are stored.
- Tokens are stored as hashes only.
- Git credentials must never be stored in plaintext; they are encrypted and versioned.
- API responses/logs/activity logs must never expose credentials, internal paths, stack traces, or raw unsafe Git output.
- Metadata must be snapshot-aware and parse-run-aware.
- Worker jobs must support lease, retry, attempts, timeout, cancellation, DLQ, and idempotency.
- Audit logs are append-only from the application perspective.
- High-volume tables must have partitioning and retention strategy.

---

## 4.2 PostgreSQL Schema Map

Recommended logical PostgreSQL schemas:

```text
identity     -- users, sessions, login security, invites, tokens
 tenant      -- organizations, organization members, teams, tenant settings
billing      -- plans, subscriptions, usage, invoices
core         -- projects, project settings, project members, project invites
repo         -- repositories, credentials, snapshots, file inventory, Git cache
ops          -- jobs, attempts, leases, outbox, inbox, DLQ, worker heartbeats
metadata     -- parsed Godot metadata from repository snapshots
analysis     -- health reports, health issues, dependency graph snapshots
storage      -- artifact records for MinIO/S3 objects
collab       -- notifications, activities, comments, review threads
audit        -- immutable audit/security/data-access logs
search       -- materialized search documents and indexing runs
governance   -- retention, purge, legal hold, data classification
admin        -- app versions, migration runs, backfills, seeds, system checks
```

> For early EF Core implementation, tables may initially live in `public`, but schema boundaries should remain reflected in code folders, documentation, migrations, and naming conventions.

---

# 4.3 Identity Schema

The `identity` schema owns authentication, account security, sessions, login tracking, invite tokens, password reset, and user preferences.

---

## 4.3.1 `identity.users`

Stores user identity and account status.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `email` | `varchar(255)` | No | Original email address. |
| `normalized_email` | `varchar(255)` | No | Lowercase/normalized email for unique lookup. |
| `display_name` | `varchar(120)` | No | User display name. |
| `password_hash` | `varchar(255)` | No | Password hash. Never plaintext. |
| `system_role` | `varchar(30)` | No | `system_admin`, `support_admin`, `user`. |
| `status` | `varchar(30)` | No | `pending_activation`, `active`, `locked`, `disabled`, `deleted`. |
| `email_verified_at` | `timestamptz` | Yes | When the email was verified. |
| `last_login_at` | `timestamptz` | Yes | Last successful login. |
| `password_changed_at` | `timestamptz` | Yes | Used to revoke old sessions. |
| `failed_login_count` | `int` | No | Failed login count. Default `0`. |
| `locked_until` | `timestamptz` | Yes | Lock expiration. |
| `avatar_url` | `varchar(500)` | Yes | Avatar URL. |
| `security_stamp` | `varchar(100)` | No | Changes after security-sensitive updates. |
| `concurrency_stamp` | `varchar(100)` | No | Optimistic concurrency token. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |
| `deleted_at` | `timestamptz` | Yes | Soft delete timestamp. |

Constraints:

```sql
check (system_role in ('system_admin', 'support_admin', 'user'));
check (status in ('pending_activation', 'active', 'locked', 'disabled', 'deleted'));
```

Indexes:

```sql
create unique index ux_users_normalized_email
on identity.users(normalized_email);

create index ix_users_status
on identity.users(status);

create index ix_users_created_at
on identity.users(created_at desc);
```

Data classification:

| Column | Classification |
| --- | --- |
| `email`, `normalized_email` | Confidential |
| `password_hash` | Secret |
| `security_stamp` | Secret/Internal |

---

## 4.3.2 `identity.user_profiles`

Stores non-auth profile information.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | No | FK to `identity.users`, unique. |
| `bio` | `text` | Yes | User biography. |
| `timezone` | `varchar(80)` | Yes | User timezone. |
| `locale` | `varchar(20)` | Yes | UI locale. |
| `company_name` | `varchar(200)` | Yes | Optional company name. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |

Indexes:

```sql
create unique index ux_user_profiles_user_id
on identity.user_profiles(user_id);
```

---

## 4.3.3 `identity.user_settings`

Stores user preferences.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | No | FK to `identity.users`, unique. |
| `theme` | `varchar(20)` | No | `dark`, `light`, `system`. |
| `notification_in_app` | `boolean` | No | In-app notifications enabled. |
| `notification_email` | `boolean` | No | Email notifications enabled. |
| `notification_digest` | `varchar(20)` | No | `off`, `daily`, `weekly`. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |

Constraints:

```sql
check (theme in ('dark', 'light', 'system'));
check (notification_digest in ('off', 'daily', 'weekly'));
```

---

## 4.3.4 `identity.refresh_tokens`

Stores refresh token hashes and rotation metadata.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | No | FK to `identity.users`. |
| `session_id` | `uuid` | No | FK to `identity.user_sessions`. |
| `token_hash` | `varchar(255)` | No | Hash of refresh token. |
| `token_family_id` | `uuid` | No | Token family for reuse detection. |
| `expires_at` | `timestamptz` | No | Expiration time. |
| `revoked_at` | `timestamptz` | Yes | Revocation time. |
| `replaced_by_token_id` | `uuid` | Yes | FK to rotated token. |
| `reuse_detected_at` | `timestamptz` | Yes | Token reuse detection time. |
| `created_at` | `timestamptz` | No | Creation time. |
| `ip_address` | `varchar(45)` | Yes | Request IP address. |
| `user_agent` | `varchar(500)` | Yes | Request user agent. |

Indexes:

```sql
create unique index ux_refresh_tokens_hash
on identity.refresh_tokens(token_hash);

create index ix_refresh_tokens_user_active
on identity.refresh_tokens(user_id, expires_at)
where revoked_at is null;

create index ix_refresh_tokens_family
on identity.refresh_tokens(token_family_id);
```

Data classification:

| Column | Classification |
| --- | --- |
| `token_hash` | Secret |

---

## 4.3.5 `identity.user_sessions`

Represents active and historical login sessions.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | No | FK to `identity.users`. |
| `device_name` | `varchar(200)` | Yes | Human-readable device name. |
| `ip_address` | `varchar(45)` | Yes | IP address. |
| `user_agent` | `varchar(500)` | Yes | Browser/device user agent. |
| `created_at` | `timestamptz` | No | Session creation. |
| `last_seen_at` | `timestamptz` | Yes | Last activity. |
| `expires_at` | `timestamptz` | No | Session expiration. |
| `revoked_at` | `timestamptz` | Yes | Revocation time. |
| `revoked_reason` | `varchar(100)` | Yes | `logout`, `password_changed`, `token_reuse`, `admin_revoked`. |

Indexes:

```sql
create index ix_user_sessions_user_active
on identity.user_sessions(user_id, expires_at)
where revoked_at is null;
```

---

## 4.3.6 `identity.user_invites`

Stores account creation invites.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `email` | `varchar(255)` | No | Invited email. |
| `normalized_email` | `varchar(255)` | No | Normalized invited email. |
| `invited_user_id` | `uuid` | Yes | FK to `identity.users`. |
| `invited_by` | `uuid` | Yes | FK to inviter user. |
| `token_hash` | `varchar(255)` | No | Invite token hash. |
| `status` | `varchar(30)` | No | `pending`, `accepted`, `expired`, `revoked`. |
| `expires_at` | `timestamptz` | No | Expiration time. |
| `accepted_at` | `timestamptz` | Yes | Acceptance time. |
| `revoked_at` | `timestamptz` | Yes | Revocation time. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create unique index ux_user_invites_token_hash
on identity.user_invites(token_hash);

create index ix_user_invites_email_status
on identity.user_invites(normalized_email, status);
```

---

## 4.3.7 `identity.password_reset_tokens`

Stores password reset token hashes.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | No | FK to `identity.users`. |
| `token_hash` | `varchar(255)` | No | Reset token hash. |
| `expires_at` | `timestamptz` | No | Expiration time. |
| `used_at` | `timestamptz` | Yes | When used. |
| `revoked_at` | `timestamptz` | Yes | When revoked. |
| `created_at` | `timestamptz` | No | Creation time. |
| `ip_address` | `varchar(45)` | Yes | Request IP address. |

Indexes:

```sql
create unique index ux_password_reset_tokens_hash
on identity.password_reset_tokens(token_hash);
```

---

## 4.3.8 `identity.email_change_requests`

Tracks email change confirmations.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | No | FK to `identity.users`. |
| `old_email` | `varchar(255)` | No | Current email. |
| `new_email` | `varchar(255)` | No | New requested email. |
| `normalized_new_email` | `varchar(255)` | No | Normalized new email. |
| `token_hash` | `varchar(255)` | No | Confirmation token hash. |
| `status` | `varchar(30)` | No | `pending`, `confirmed`, `expired`, `cancelled`. |
| `expires_at` | `timestamptz` | No | Expiration. |
| `confirmed_at` | `timestamptz` | Yes | Confirmation time. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.3.9 `identity.login_events`

Tracks all login attempts for security monitoring.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | Yes | Nullable to avoid user enumeration. |
| `normalized_email` | `varchar(255)` | No | Attempted email. |
| `outcome` | `varchar(30)` | No | `success`, `failed`, `locked`, `disabled`, `rate_limited`. |
| `failure_reason` | `varchar(100)` | Yes | Reason for failed attempt. |
| `ip_address` | `varchar(45)` | Yes | IP address. |
| `user_agent` | `varchar(500)` | Yes | User agent. |
| `correlation_id` | `varchar(80)` | No | Request correlation ID. |
| `created_at` | `timestamptz` | No | Event time. |

Indexes:

```sql
create index ix_login_events_email_created
on identity.login_events(normalized_email, created_at desc);

create index ix_login_events_ip_created
on identity.login_events(ip_address, created_at desc);
```

Partitioning: monthly.

---

## 4.3.10 `identity.security_events`

Tracks user/account security events.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | Yes | Related user. |
| `event_type` | `varchar(80)` | No | `password_changed`, `token_reuse`, `account_locked`, etc. |
| `severity` | `varchar(20)` | No | `info`, `warning`, `critical`. |
| `details` | `jsonb` | Yes | Sanitized event details. |
| `ip_address` | `varchar(45)` | Yes | IP address. |
| `user_agent` | `varchar(500)` | Yes | User agent. |
| `correlation_id` | `varchar(80)` | No | Correlation ID. |
| `created_at` | `timestamptz` | No | Event time. |

Partitioning: monthly.

---

# 4.4 Tenant / Organization Schema

The `tenant` schema supports individual workspaces, teams, and enterprise organizations.

---

## 4.6.1 `core.projects`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `name` | `varchar(80)` | No | Project name. |
| `slug` | `varchar(100)` | No | Unique inside organization. |
| `description` | `text` | Yes | Project description. |
| `godot_version` | `varchar(30)` | No | Target Godot version. |
| `visibility` | `varchar(30)` | No | `private`, `internal`. |
| `status` | `varchar(30)` | No | `active`, `archived`, `deleted`. |
| `health_score` | `int` | Yes | Latest health score, 0-100. |
| `created_by` | `uuid` | No | FK to user. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |
| `archived_at` | `timestamptz` | Yes | Archive time. |
| `deleted_at` | `timestamptz` | Yes | Soft delete time. |

Constraints:

```sql
check (visibility in ('private', 'internal'));
check (status in ('active', 'archived', 'deleted'));
check (health_score is null or health_score between 0 and 100);
```

Indexes:

```sql
create unique index ux_projects_org_slug_active
on core.projects(organization_id, slug)
where deleted_at is null;

create index ix_projects_org_status
on core.projects(organization_id, status);

create index ix_projects_created_by
on core.projects(created_by);
```

---

## 4.6.2 `core.project_members`

Stores project-level RBAC.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to projects. |
| `user_id` | `uuid` | No | FK to users. |
| `role` | `varchar(30)` | No | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer`. |
| `source` | `varchar(30)` | No | `direct`, `team`, `inherited`. |
| `created_by` | `uuid` | Yes | User who added this member. |
| `joined_at` | `timestamptz` | No | Join time. |
| `removed_at` | `timestamptz` | Yes | Removal time. |

Indexes:

```sql
create unique index ux_project_members_active
on core.project_members(project_id, user_id)
where removed_at is null;

create index ix_project_members_user
on core.project_members(user_id);

create index ix_project_members_project_role
on core.project_members(project_id, role);
```

---

## 4.6.3 `core.project_member_history`

Stores membership change history.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to projects. |
| `user_id` | `uuid` | No | FK to users. |
| `action` | `varchar(30)` | No | `added`, `removed`, `role_changed`, `restored`. |
| `old_role` | `varchar(30)` | Yes | Previous role. |
| `new_role` | `varchar(30)` | Yes | New role. |
| `changed_by` | `uuid` | No | User who changed membership. |
| `reason` | `text` | Yes | Optional reason. |
| `created_at` | `timestamptz` | No | Change time. |

Partitioning: yearly or monthly depending volume.

---

## 4.6.4 `core.project_settings`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to projects, unique. |
| `auto_parse_on_push` | `boolean` | No | Trigger parse after sync/push. |
| `auto_analyze_on_parse` | `boolean` | No | Trigger analyze after parse. |
| `health_check_schedule` | `varchar(30)` | No | `manual`, `daily`, `weekly`. |
| `notification_email_enabled` | `boolean` | No | Project email notifications. |
| `metadata_retention_days` | `int` | No | Metadata retention. |
| `diff_artifact_retention_days` | `int` | No | Diff artifact retention. |
| `max_repo_size_bytes` | `bigint` | Yes | Optional project limit. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |

---

## 4.6.5 `core.project_invites`

Stores project invitations.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to projects. |
| `email` | `varchar(255)` | No | Invited email. |
| `normalized_email` | `varchar(255)` | No | Normalized email. |
| `role` | `varchar(30)` | No | Project role. |
| `token_hash` | `varchar(255)` | No | Invite token hash. |
| `status` | `varchar(30)` | No | `pending`, `accepted`, `expired`, `revoked`. |
| `invited_by` | `uuid` | No | User who invited. |
| `expires_at` | `timestamptz` | No | Expiration time. |
| `accepted_at` | `timestamptz` | Yes | Acceptance time. |
| `created_at` | `timestamptz` | No | Creation time. |

---

# 4.7 Repository / Git Schema

The `repo` schema owns repository configuration, credentials, workspace state, snapshots, file inventory, and Git history cache.

---

## 4.7.1 `repo.repositories`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to projects. One repository per project in MVP. |
| `remote_url` | `varchar(800)` | No | Sanitized remote URL without credentials. |
| `provider` | `varchar(50)` | No | `github`, `gitlab`, `bitbucket`, `generic`. |
| `default_branch` | `varchar(150)` | No | Default branch. |
| `workspace_key` | `varchar(200)` | Yes | Internal workspace key, not raw path. |
| `clone_status` | `varchar(30)` | No | `pending`, `cloning`, `ready`, `error`, `disabled`. |
| `last_synced_at` | `timestamptz` | Yes | Last successful sync. |
| `repo_size_bytes` | `bigint` | Yes | Repository size. |
| `current_commit_hash` | `varchar(80)` | Yes | Current commit. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |
| `deleted_at` | `timestamptz` | Yes | Soft delete. |

Indexes:

```sql
create unique index ux_repositories_project
on repo.repositories(project_id)
where deleted_at is null;

create index ix_repositories_org_status
on repo.repositories(organization_id, clone_status);
```

---

## 4.7.2 `repo.repository_credentials`

Logical credential record.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repositories. |
| `credential_type` | `varchar(30)` | No | `pat`, `basic`, `oauth`, `deploy_token`. |
| `status` | `varchar(30)` | No | `active`, `rotated`, `revoked`, `expired`. |
| `created_by` | `uuid` | Yes | User who created credential. |
| `created_at` | `timestamptz` | No | Creation time. |
| `rotated_at` | `timestamptz` | Yes | Rotation time. |
| `revoked_at` | `timestamptz` | Yes | Revocation time. |

---

## 4.7.3 `repo.repository_credential_versions`

Stores encrypted credential versions.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `credential_id` | `uuid` | No | FK to repository credentials. |
| `encrypted_secret` | `text` | No | Encrypted PAT/secret. |
| `encryption_key_id` | `varchar(120)` | No | KMS/envelope key reference. |
| `secret_fingerprint` | `varchar(120)` | Yes | Non-reversible fingerprint. |
| `status` | `varchar(30)` | No | `active`, `rotated`, `revoked`. |
| `created_by` | `uuid` | Yes | User who created this version. |
| `created_at` | `timestamptz` | No | Creation time. |
| `revoked_at` | `timestamptz` | Yes | Revocation time. |

Indexes:

```sql
create unique index ux_repo_credential_active_version
on repo.repository_credential_versions(credential_id)
where status = 'active';
```

Data classification: Secret.

---

## 4.7.4 `repo.repository_snapshots`

Represents repository state at a branch/commit used by parser/analyzer.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repositories. |
| `branch_name` | `varchar(150)` | No | Branch name. |
| `commit_hash` | `varchar(80)` | No | Commit hash. |
| `commit_author` | `varchar(255)` | Yes | Commit author. |
| `commit_message` | `text` | Yes | Commit message. |
| `snapshot_status` | `varchar(30)` | No | `created`, `parsed`, `analyzed`, `stale`, `failed`. |
| `created_by_job_id` | `uuid` | Yes | FK to `ops.jobs`. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create unique index ux_repo_snapshots_commit
on repo.repository_snapshots(repository_id, commit_hash);

create index ix_repo_snapshots_repo_created
on repo.repository_snapshots(repository_id, created_at desc);
```

---

## 4.7.5 `repo.repository_files`

Snapshot file inventory.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repositories. |
| `snapshot_id` | `uuid` | No | FK to repository snapshots. |
| `file_path` | `varchar(800)` | No | Path in repository. |
| `file_name` | `varchar(255)` | No | File name. |
| `extension` | `varchar(30)` | Yes | Extension. |
| `file_kind` | `varchar(30)` | No | `scene`, `script`, `resource`, `asset`, `import`, `config`, `other`. |
| `file_size_bytes` | `bigint` | No | File size. |
| `file_hash` | `varchar(80)` | No | SHA-256 hash. |
| `is_deleted` | `boolean` | No | Whether file is deleted in this snapshot. |
| `last_seen_at` | `timestamptz` | No | Last seen time. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create unique index ux_repository_files_snapshot_path
on repo.repository_files(snapshot_id, file_path);

create index ix_repository_files_kind
on repo.repository_files(snapshot_id, file_kind);

create index ix_repository_files_hash
on repo.repository_files(repository_id, file_hash);
```

Partitioning: by repository or snapshot for large installations.

---

## 4.7.6 `repo.file_versions`

Tracks file versions across commits/snapshots.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_file_id` | `uuid` | No | FK to repository files. |
| `snapshot_id` | `uuid` | No | FK to repository snapshots. |
| `commit_hash` | `varchar(80)` | No | Commit hash. |
| `file_hash` | `varchar(80)` | No | File hash. |
| `change_type` | `varchar(30)` | No | `added`, `modified`, `deleted`, `renamed`, `unchanged`. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.7.7 `repo.git_refs`

Caches branches and tags.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repositories. |
| `ref_type` | `varchar(30)` | No | `branch`, `tag`, `remote_branch`. |
| `name` | `varchar(200)` | No | Ref name. |
| `commit_hash` | `varchar(80)` | Yes | Commit hash. |
| `is_default` | `boolean` | No | Is default branch. |
| `last_seen_at` | `timestamptz` | No | Last seen time. |

Indexes:

```sql
create unique index ux_git_refs_repo_type_name
on repo.git_refs(repository_id, ref_type, name);
```

---

## 4.7.8 `repo.git_commits`

Caches commit history.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repositories. |
| `commit_hash` | `varchar(80)` | No | Commit hash. |
| `parent_hashes` | `text[]` | Yes | Parent commits. |
| `author_name` | `varchar(255)` | Yes | Author name. |
| `author_email` | `varchar(255)` | Yes | Author email. |
| `committer_name` | `varchar(255)` | Yes | Committer name. |
| `committer_email` | `varchar(255)` | Yes | Committer email. |
| `committed_at` | `timestamptz` | No | Commit time. |
| `message` | `text` | No | Commit message. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create unique index ux_git_commits_repo_hash
on repo.git_commits(repository_id, commit_hash);

create index ix_git_commits_repo_time
on repo.git_commits(repository_id, committed_at desc);
```

---

## 4.7.9 `repo.git_commit_files`

Stores changed files per commit.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `commit_id` | `uuid` | No | FK to Git commit. |
| `file_path` | `varchar(800)` | No | File path. |
| `old_file_path` | `varchar(800)` | Yes | Previous path for rename. |
| `change_type` | `varchar(30)` | No | `added`, `modified`, `deleted`, `renamed`, `copied`. |
| `additions` | `int` | Yes | Added lines. |
| `deletions` | `int` | Yes | Deleted lines. |

Indexes:

```sql
create index ix_git_commit_files_commit
on repo.git_commit_files(commit_id);

create index ix_git_commit_files_path
on repo.git_commit_files(file_path);
```

---

## 4.7.10 `repo.workspace_states`

Tracks internal workspace state. Raw server path must not be exposed to users.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repository, unique. |
| `workspace_key` | `varchar(200)` | No | Internal workspace identifier. |
| `current_branch` | `varchar(150)` | Yes | Current branch. |
| `current_commit_hash` | `varchar(80)` | Yes | Current commit. |
| `dirty_state` | `varchar(30)` | No | `clean`, `dirty`, `conflict`, `unknown`. |
| `last_git_status_at` | `timestamptz` | Yes | Last Git status time. |
| `updated_at` | `timestamptz` | No | Update time. |

---

## 4.7.11 `repo.repository_sync_runs`

Tracks repository clone/fetch/pull runs.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repository. |
| `job_id` | `uuid` | Yes | FK to job. |
| `sync_type` | `varchar(30)` | No | `clone`, `fetch`, `pull`, `manual`. |
| `status` | `varchar(30)` | No | `running`, `completed`, `failed`, `cancelled`. |
| `started_at` | `timestamptz` | No | Start time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |
| `error_code` | `varchar(100)` | Yes | Error code. |
| `error_message` | `text` | Yes | Sanitized error message. |

---

## 4.7.12 `repo.webhook_events`

Stores future Git provider webhooks.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | Yes | FK to repository. |
| `provider` | `varchar(50)` | No | Git provider. |
| `event_type` | `varchar(80)` | No | Provider event type. |
| `delivery_id` | `varchar(200)` | No | Provider delivery ID. |
| `payload` | `jsonb` | No | Sanitized payload. |
| `signature_verified` | `boolean` | No | Signature verification result. |
| `status` | `varchar(30)` | No | `received`, `processed`, `ignored`, `failed`. |
| `created_at` | `timestamptz` | No | Receive time. |
| `processed_at` | `timestamptz` | Yes | Processing time. |

Indexes:

```sql
create unique index ux_webhook_events_provider_delivery
on repo.webhook_events(provider, delivery_id);
```

---

# 4.8 Ops / Worker Schema

The `ops` schema tracks async jobs, attempts, leasing, worker health, transactional outbox, idempotent inbox, and dead-letter records.

---

## 4.8.1 `ops.jobs`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `repository_id` | `uuid` | Yes | FK to repository. |
| `type` | `varchar(40)` | No | `clone`, `fetch`, `parse`, `analyze`, `diff`, `preview`, `notification`. |
| `status` | `varchar(30)` | No | `queued`, `running`, `retrying`, `completed`, `failed`, `cancelled`, `timeout`, `dead_lettered`. |
| `queue_name` | `varchar(100)` | No | Target queue. |
| `priority` | `int` | No | Higher value = higher priority. |
| `progress` | `int` | No | 0-100. |
| `payload` | `jsonb` | Yes | Sanitized input. |
| `result` | `jsonb` | Yes | Sanitized output. |
| `metadata` | `jsonb` | Yes | Additional metadata. |
| `idempotency_key` | `varchar(160)` | Yes | Idempotent request key. |
| `max_attempts` | `int` | No | Default `3`. |
| `attempt_count` | `int` | No | Attempt count. |
| `available_at` | `timestamptz` | No | For retry delay. |
| `started_at` | `timestamptz` | Yes | Start time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |
| `cancelled_at` | `timestamptz` | Yes | Cancellation time. |
| `timeout_at` | `timestamptz` | Yes | Timeout time. |
| `last_heartbeat_at` | `timestamptz` | Yes | Worker heartbeat time. |
| `cancellation_requested_at` | `timestamptz` | Yes | Cancellation intent time. |
| `error_code` | `varchar(100)` | Yes | Error code. |
| `error_message` | `text` | Yes | Sanitized error message. |
| `triggered_by` | `uuid` | Yes | FK to user. |
| `correlation_id` | `varchar(80)` | No | Correlation ID. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |

Indexes:

```sql
create index ix_jobs_queue_poll
on ops.jobs(status, available_at, priority desc, created_at);

create index ix_jobs_project_created
on ops.jobs(project_id, created_at desc);

create index ix_jobs_type_status
on ops.jobs(type, status);

create unique index ux_jobs_idempotency
on ops.jobs(project_id, type, idempotency_key)
where idempotency_key is not null;
```

Partitioning: monthly after scale.

---

## 4.8.2 `ops.job_attempts`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `job_id` | `uuid` | No | FK to jobs. |
| `attempt_number` | `int` | No | Attempt number. |
| `worker_name` | `varchar(100)` | Yes | Worker logical name. |
| `worker_instance_id` | `varchar(120)` | Yes | Worker instance ID. |
| `status` | `varchar(30)` | No | `running`, `completed`, `failed`, `timeout`, `cancelled`. |
| `started_at` | `timestamptz` | No | Start time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |
| `duration_ms` | `int` | Yes | Duration. |
| `error_code` | `varchar(100)` | Yes | Error code. |
| `error_message` | `text` | Yes | Sanitized error. |
| `stack_trace_hash` | `varchar(100)` | Yes | Hash of stack trace, not raw trace by default. |

Indexes:

```sql
create unique index ux_job_attempts_job_number
on ops.job_attempts(job_id, attempt_number);
```

Partitioning: monthly.

---

## 4.8.3 `ops.job_events`

Tracks job lifecycle events and progress timeline.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `job_id` | `uuid` | No | FK to jobs. |
| `event_type` | `varchar(80)` | No | `created`, `started`, `progress`, `retry_scheduled`, `completed`, etc. |
| `progress` | `int` | Yes | Progress if relevant. |
| `message` | `text` | Yes | Sanitized message. |
| `data` | `jsonb` | Yes | Sanitized event data. |
| `created_at` | `timestamptz` | No | Event time. |

Indexes:

```sql
create index ix_job_events_job_created
on ops.job_events(job_id, created_at);
```

Partitioning: monthly.

---

## 4.8.4 `ops.job_leases`

Prevents multiple workers from processing the same job.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `job_id` | `uuid` | No | FK to jobs. |
| `worker_instance_id` | `varchar(120)` | No | Worker instance. |
| `lease_token` | `varchar(120)` | No | Lease ownership token. |
| `leased_until` | `timestamptz` | No | Lease expiration. |
| `acquired_at` | `timestamptz` | No | Acquire time. |
| `renewed_at` | `timestamptz` | Yes | Last renewal. |
| `released_at` | `timestamptz` | Yes | Release time. |

Indexes:

```sql
create index ix_job_leases_expired
on ops.job_leases(leased_until);

create unique index ux_job_leases_active
on ops.job_leases(job_id)
where released_at is null;
```

---

## 4.8.5 `ops.job_dependencies`

Represents chained jobs, such as clone → parse → analyze.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `job_id` | `uuid` | No | Job waiting on dependency. |
| `depends_on_job_id` | `uuid` | No | Required prior job. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.8.6 `ops.job_cancellations`

Stores cancellation requests.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `job_id` | `uuid` | No | FK to jobs. |
| `requested_by` | `uuid` | No | FK to users. |
| `reason` | `text` | Yes | Cancellation reason. |
| `created_at` | `timestamptz` | No | Request time. |

---

## 4.8.7 `ops.worker_heartbeats`

Tracks worker health.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `worker_name` | `varchar(100)` | No | Logical worker name. |
| `worker_instance_id` | `varchar(120)` | No | Unique worker instance. |
| `queues` | `text[]` | No | Queues handled. |
| `status` | `varchar(30)` | No | `starting`, `running`, `draining`, `stopped`, `unhealthy`. |
| `started_at` | `timestamptz` | No | Start time. |
| `last_seen_at` | `timestamptz` | No | Last heartbeat. |
| `metadata` | `jsonb` | Yes | Runtime metadata. |

Indexes:

```sql
create unique index ux_worker_heartbeats_instance
on ops.worker_heartbeats(worker_instance_id);
```

---

## 4.8.8 `ops.outbox_messages`

Transactional outbox for reliable event publishing.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `aggregate_type` | `varchar(100)` | No | Aggregate type. |
| `aggregate_id` | `uuid` | Yes | Aggregate ID. |
| `event_type` | `varchar(120)` | No | Event type. |
| `payload` | `jsonb` | No | Event payload. |
| `headers` | `jsonb` | Yes | Message headers. |
| `correlation_id` | `varchar(80)` | No | Correlation ID. |
| `status` | `varchar(30)` | No | `pending`, `processing`, `processed`, `failed`. |
| `attempts` | `int` | No | Publish attempts. |
| `available_at` | `timestamptz` | No | Next attempt time. |
| `processed_at` | `timestamptz` | Yes | Processed time. |
| `error_message` | `text` | Yes | Error message. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create index ix_outbox_status_available
on ops.outbox_messages(status, available_at);
```

---

## 4.8.9 `ops.inbox_messages`

Consumer-side idempotency table.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `message_id` | `varchar(160)` | No | Message ID. |
| `consumer_name` | `varchar(120)` | No | Consumer name. |
| `status` | `varchar(30)` | No | `received`, `processed`, `failed`. |
| `received_at` | `timestamptz` | No | Receive time. |
| `processed_at` | `timestamptz` | Yes | Process time. |
| `error_message` | `text` | Yes | Error message. |

Indexes:

```sql
create unique index ux_inbox_message_consumer
on ops.inbox_messages(message_id, consumer_name);
```

---

## 4.8.10 `ops.dead_letter_messages`

Tracks DLQ messages in PostgreSQL for support, replay, and audit.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `original_queue` | `varchar(120)` | No | Original queue. |
| `message_id` | `varchar(160)` | Yes | Message ID. |
| `job_id` | `uuid` | Yes | FK to jobs. |
| `payload` | `jsonb` | Yes | Sanitized payload. |
| `error_code` | `varchar(100)` | Yes | Error code. |
| `error_message` | `text` | Yes | Error message. |
| `failed_attempts` | `int` | No | Failed attempts. |
| `moved_at` | `timestamptz` | No | Moved to DLQ time. |
| `replayed_at` | `timestamptz` | Yes | Replay time. |
| `replayed_by` | `uuid` | Yes | User who replayed. |

---

# 4.9 Metadata Schema

The `metadata` schema stores parsed Godot metadata. All parsed metadata must be tied to a repository snapshot and metadata run.

---

## 4.9.1 `metadata.metadata_runs`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `repository_id` | `uuid` | No | FK to repository. |
| `snapshot_id` | `uuid` | No | FK to repository snapshot. |
| `job_id` | `uuid` | Yes | FK to job. |
| `run_type` | `varchar(40)` | No | `full_parse`, `incremental_parse`, `reparse`. |
| `status` | `varchar(30)` | No | `running`, `completed`, `failed`, `cancelled`. |
| `file_count` | `int` | No | File count. |
| `scene_count` | `int` | No | Scene count. |
| `asset_count` | `int` | No | Asset count. |
| `script_count` | `int` | No | Script count. |
| `resource_count` | `int` | No | Resource count. |
| `dependency_count` | `int` | No | Dependency count. |
| `started_at` | `timestamptz` | No | Start time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create index ix_metadata_runs_repo_created
on metadata.metadata_runs(repository_id, created_at desc);

create unique index ux_metadata_runs_snapshot_completed
on metadata.metadata_runs(snapshot_id)
where status = 'completed';
```

---

## 4.9.2 `metadata.scenes`

Represents parsed `.tscn` scene files.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repository. |
| `snapshot_id` | `uuid` | No | FK to repository snapshot. |
| `metadata_run_id` | `uuid` | No | FK to metadata run. |
| `file_path` | `varchar(800)` | No | Scene file path. |
| `scene_name` | `varchar(255)` | No | Scene name. |
| `format_version` | `int` | Yes | Godot format version. |
| `uid` | `varchar(120)` | Yes | Godot UID. |
| `load_steps` | `int` | Yes | Godot load steps. |
| `root_node_name` | `varchar(255)` | Yes | Root node name. |
| `root_node_type` | `varchar(120)` | Yes | Root node type. |
| `node_count` | `int` | No | Node count. |
| `file_hash` | `varchar(80)` | No | File hash. |
| `parsed_at` | `timestamptz` | No | Parse time. |

Indexes:

```sql
create unique index ux_scenes_snapshot_path
on metadata.scenes(snapshot_id, file_path);

create index ix_scenes_repo_path
on metadata.scenes(repository_id, file_path);
```

---

## 4.9.3 `metadata.scene_nodes`

Stores scene node tree and supports Scene Flow Table.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `scene_id` | `uuid` | No | FK to scene. |
| `node_path` | `varchar(1000)` | No | Unique node path within scene. |
| `node_name` | `varchar(255)` | No | Node name. |
| `node_type` | `varchar(120)` | No | Godot node type. |
| `parent_path` | `varchar(1000)` | No | Parent node path. |
| `depth` | `int` | No | Tree depth for UI. |
| `node_order` | `int` | No | Preorder index for flow table. |
| `script_path` | `varchar(800)` | Yes | Attached script path. |
| `groups` | `text[]` | Yes | Node groups. |
| `important_properties` | `jsonb` | Yes | UI-friendly property subset. |
| `properties` | `jsonb` | Yes | Full parsed properties. |
| `warning_count` | `int` | No | Parser/health warnings for node. |

Indexes:

```sql
create unique index ux_scene_nodes_scene_path
on metadata.scene_nodes(scene_id, node_path);

create index ix_scene_nodes_type
on metadata.scene_nodes(scene_id, node_type);

create index ix_scene_nodes_order
on metadata.scene_nodes(scene_id, node_order);
```

---

## 4.9.4 `metadata.scene_node_properties`

Optional normalized property table for querying and diffing.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `scene_node_id` | `uuid` | No | FK to scene node. |
| `property_name` | `varchar(255)` | No | Property name. |
| `property_type` | `varchar(80)` | Yes | Parsed property type. |
| `property_value_json` | `jsonb` | Yes | Property value. |
| `property_value_hash` | `varchar(80)` | Yes | Hash for diffing. |

Indexes:

```sql
create index ix_scene_node_properties_node
on metadata.scene_node_properties(scene_node_id);
```

---

## 4.9.5 `metadata.scene_node_references`

Node-level references for Flow Table and diagnostics.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `scene_node_id` | `uuid` | No | FK to scene node. |
| `property_name` | `varchar(255)` | No | e.g. `texture`, `script`. |
| `reference_type` | `varchar(40)` | No | `script`, `texture`, `resource`, `packed_scene`, `audio`, `other`. |
| `target_path` | `varchar(800)` | Yes | Target path. |
| `target_uid` | `varchar(120)` | Yes | Godot UID. |
| `target_exists` | `boolean` | No | Whether target exists. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create index ix_scene_node_references_node
on metadata.scene_node_references(scene_node_id);

create index ix_scene_node_references_target
on metadata.scene_node_references(target_path);
```

---

## 4.9.6 `metadata.scene_connections`

Stores Godot signal connections.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `scene_id` | `uuid` | No | FK to scene. |
| `signal_name` | `varchar(255)` | No | Signal name. |
| `from_node_path` | `varchar(1000)` | No | Signal source node path. |
| `to_node_path` | `varchar(1000)` | No | Signal target node path. |
| `method_name` | `varchar(255)` | No | Connected method. |
| `flags` | `int` | Yes | Godot connection flags. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.9.7 `metadata.assets`

Stores parsed asset metadata.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repository. |
| `snapshot_id` | `uuid` | No | FK to snapshot. |
| `metadata_run_id` | `uuid` | No | FK to metadata run. |
| `file_path` | `varchar(800)` | No | Asset path. |
| `file_name` | `varchar(255)` | No | File name. |
| `asset_type` | `varchar(40)` | No | `image`, `audio`, `font`, `shader`, `model`, `video`, `other`. |
| `file_size_bytes` | `bigint` | No | File size. |
| `mime_type` | `varchar(120)` | Yes | MIME type. |
| `dimensions` | `jsonb` | Yes | Width/height etc. |
| `thumbnail_artifact_id` | `uuid` | Yes | FK to storage artifact. |
| `file_hash` | `varchar(80)` | No | File hash. |
| `imported_metadata` | `jsonb` | Yes | Godot import data. |

Indexes:

```sql
create unique index ux_assets_snapshot_path
on metadata.assets(snapshot_id, file_path);

create index ix_assets_snapshot_type
on metadata.assets(snapshot_id, asset_type);
```

---

## 4.9.8 `metadata.asset_imports`

Stores Godot `.import` metadata.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `asset_id` | `uuid` | No | FK to asset. |
| `importer` | `varchar(120)` | Yes | Godot importer. |
| `import_type` | `varchar(120)` | Yes | Import type. |
| `uid` | `varchar(120)` | Yes | Godot UID. |
| `source_file` | `varchar(800)` | Yes | Source file. |
| `dest_files` | `text[]` | Yes | Generated destination files. |
| `import_options` | `jsonb` | Yes | Import options. |

---

## 4.9.9 `metadata.scripts`

Stores `.gd` script metadata.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repository. |
| `snapshot_id` | `uuid` | No | FK to snapshot. |
| `metadata_run_id` | `uuid` | No | FK to metadata run. |
| `file_path` | `varchar(800)` | No | Script path. |
| `class_name` | `varchar(255)` | Yes | GDScript class name. |
| `extends_type` | `varchar(255)` | Yes | Extended type. |
| `line_count` | `int` | No | Line count. |
| `public_method_count` | `int` | No | Public method count. |
| `signal_count` | `int` | No | Signal count. |
| `exported_property_count` | `int` | No | Exported property count. |
| `file_hash` | `varchar(80)` | No | File hash. |
| `parse_summary` | `jsonb` | Yes | Parse summary. |

Indexes:

```sql
create unique index ux_scripts_snapshot_path
on metadata.scripts(snapshot_id, file_path);

create index ix_scripts_class_name
on metadata.scripts(snapshot_id, class_name);
```

---

## 4.9.10 `metadata.script_symbols`

Stores symbols extracted from scripts.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `script_id` | `uuid` | No | FK to script. |
| `symbol_type` | `varchar(40)` | No | `class`, `method`, `signal`, `exported_property`, `const`, `enum`. |
| `name` | `varchar(255)` | No | Symbol name. |
| `signature` | `text` | Yes | Method/property signature. |
| `line_number` | `int` | Yes | Source line. |
| `metadata` | `jsonb` | Yes | Additional metadata. |

---

## 4.9.11 `metadata.resources`

Stores `.tres` and `.res` resource metadata.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repository. |
| `snapshot_id` | `uuid` | No | FK to snapshot. |
| `metadata_run_id` | `uuid` | No | FK to metadata run. |
| `file_path` | `varchar(800)` | No | Resource path. |
| `resource_type` | `varchar(120)` | No | Godot resource type. |
| `uid` | `varchar(120)` | Yes | Godot UID. |
| `file_hash` | `varchar(80)` | No | File hash. |
| `properties` | `jsonb` | Yes | Resource properties. |

Indexes:

```sql
create unique index ux_resources_snapshot_path
on metadata.resources(snapshot_id, file_path);
```

---

## 4.9.12 `metadata.dependencies`

Stores raw dependency graph edges.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `repository_id` | `uuid` | No | FK to repository. |
| `snapshot_id` | `uuid` | No | FK to snapshot. |
| `metadata_run_id` | `uuid` | No | FK to metadata run. |
| `source_type` | `varchar(40)` | No | `scene`, `script`, `resource`, `asset`. |
| `source_path` | `varchar(800)` | No | Source path. |
| `target_type` | `varchar(40)` | No | `scene`, `script`, `resource`, `asset`, `external`. |
| `target_path` | `varchar(800)` | No | Target path. |
| `relation` | `varchar(40)` | No | `instances`, `attaches`, `uses`, `references`, `extends`, `preload`, `load`, `imports`. |
| `is_missing` | `boolean` | No | Target missing. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create unique index ux_dependencies_snapshot_edge
on metadata.dependencies(snapshot_id, source_path, target_path, relation);

create index ix_dependencies_source
on metadata.dependencies(snapshot_id, source_type, source_path);

create index ix_dependencies_target
on metadata.dependencies(snapshot_id, target_type, target_path);

create index ix_dependencies_missing
on metadata.dependencies(snapshot_id, is_missing);
```

---

## 4.9.13 `metadata.parser_diagnostics`

Stores parser warnings and errors per file.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `metadata_run_id` | `uuid` | No | FK to metadata run. |
| `snapshot_id` | `uuid` | No | FK to snapshot. |
| `file_path` | `varchar(800)` | No | File path. |
| `severity` | `varchar(20)` | No | `info`, `warning`, `error`. |
| `code` | `varchar(100)` | No | Diagnostic code. |
| `message` | `text` | No | Message. |
| `line` | `int` | Yes | Line number. |
| `column` | `int` | Yes | Column number. |
| `details` | `jsonb` | Yes | Additional details. |
| `created_at` | `timestamptz` | No | Creation time. |

---

# 4.10 Analysis / Health / Graph Schema

The `analysis` schema stores health rules, reports, issues, suppressions, and materialized dependency graphs.

---

## 4.10.1 `analysis.analysis_runs`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `repository_id` | `uuid` | No | FK to repository. |
| `snapshot_id` | `uuid` | No | FK to snapshot. |
| `metadata_run_id` | `uuid` | Yes | FK to metadata run. |
| `job_id` | `uuid` | Yes | FK to job. |
| `status` | `varchar(30)` | No | `running`, `completed`, `failed`, `cancelled`. |
| `started_at` | `timestamptz` | No | Start time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |

---

## 4.10.2 `analysis.health_rules`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `code` | `varchar(100)` | No | Unique rule code. |
| `name` | `varchar(200)` | No | Rule name. |
| `description` | `text` | Yes | Rule description. |
| `default_severity` | `varchar(20)` | No | `critical`, `warning`, `info`. |
| `is_enabled` | `boolean` | No | Enabled by default. |
| `config` | `jsonb` | Yes | Rule config. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |

Indexes:

```sql
create unique index ux_health_rules_code
on analysis.health_rules(code);
```

---

## 4.10.3 `analysis.health_rule_versions`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `rule_id` | `uuid` | No | FK to health rule. |
| `version` | `int` | No | Rule version. |
| `config` | `jsonb` | Yes | Versioned config. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.10.4 `analysis.health_reports`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `repository_id` | `uuid` | No | FK to repository. |
| `snapshot_id` | `uuid` | No | FK to snapshot. |
| `analysis_run_id` | `uuid` | No | FK to analysis run. |
| `job_id` | `uuid` | Yes | FK to job. |
| `score` | `int` | No | 0-100 health score. |
| `total_issues` | `int` | No | Total issues. |
| `critical_count` | `int` | No | Critical count. |
| `warning_count` | `int` | No | Warning count. |
| `info_count` | `int` | No | Info count. |
| `summary` | `jsonb` | Yes | Summary. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create index ix_health_reports_project_created
on analysis.health_reports(project_id, created_at desc);

create unique index ux_health_reports_snapshot
on analysis.health_reports(snapshot_id);
```

---

## 4.10.5 `analysis.health_issues`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `report_id` | `uuid` | No | FK to health report. |
| `rule_id` | `uuid` | Yes | FK to health rule. |
| `issue_type` | `varchar(80)` | No | Issue type. |
| `severity` | `varchar(20)` | No | `critical`, `warning`, `info`. |
| `file_path` | `varchar(800)` | Yes | Related file. |
| `node_path` | `varchar(1000)` | Yes | Related node. |
| `message` | `text` | No | Issue message. |
| `details` | `jsonb` | Yes | Issue details. |
| `is_suppressed` | `boolean` | No | Suppression state. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create index ix_health_issues_report_severity
on analysis.health_issues(report_id, severity);

create index ix_health_issues_file
on analysis.health_issues(file_path);
```

---

## 4.10.6 `analysis.health_issue_suppressions`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `rule_code` | `varchar(100)` | No | Health rule code. |
| `file_path` | `varchar(800)` | Yes | Optional file scope. |
| `node_path` | `varchar(1000)` | Yes | Optional node scope. |
| `reason` | `text` | No | Suppression reason. |
| `suppressed_by` | `uuid` | No | User who suppressed. |
| `suppressed_until` | `timestamptz` | Yes | Optional expiration. |
| `created_at` | `timestamptz` | No | Creation time. |
| `revoked_at` | `timestamptz` | Yes | Revocation time. |

---

## 4.10.7 `analysis.dependency_graph_snapshots`

Stores materialized graph summaries for UI.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `repository_id` | `uuid` | No | FK to repository. |
| `snapshot_id` | `uuid` | No | FK to snapshot. |
| `analysis_run_id` | `uuid` | No | FK to analysis run. |
| `graph_hash` | `varchar(80)` | No | Graph hash. |
| `node_count` | `int` | No | Node count. |
| `edge_count` | `int` | No | Edge count. |
| `cycle_count` | `int` | No | Cycle count. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.10.8 `analysis.dependency_graph_nodes`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `graph_snapshot_id` | `uuid` | No | FK to graph snapshot. |
| `node_key` | `varchar(900)` | No | Stable graph node key. |
| `node_type` | `varchar(40)` | No | Node type. |
| `file_path` | `varchar(800)` | Yes | File path. |
| `label` | `varchar(255)` | No | UI label. |
| `metrics` | `jsonb` | Yes | Node metrics. |

---

## 4.10.9 `analysis.dependency_graph_edges`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `graph_snapshot_id` | `uuid` | No | FK to graph snapshot. |
| `source_node_key` | `varchar(900)` | No | Source node key. |
| `target_node_key` | `varchar(900)` | No | Target node key. |
| `relation` | `varchar(40)` | No | Relation type. |
| `weight` | `numeric(12,4)` | No | Edge weight. |

---

# 4.11 Storage / Artifact Schema

The `storage` schema tracks objects stored in MinIO/S3.

---

## 4.11.1 `storage.artifacts`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | Yes | FK to project. |
| `repository_id` | `uuid` | Yes | FK to repository. |
| `job_id` | `uuid` | Yes | FK to job. |
| `artifact_type` | `varchar(40)` | No | `diff`, `thumbnail`, `preview`, `report_export`, `archive`, `audit_export`. |
| `bucket_name` | `varchar(120)` | No | Bucket. |
| `object_key` | `varchar(1000)` | No | Object key. |
| `content_type` | `varchar(120)` | Yes | MIME type. |
| `size_bytes` | `bigint` | Yes | Object size. |
| `checksum` | `varchar(120)` | Yes | Object checksum. |
| `retention_until` | `timestamptz` | Yes | Retention deadline. |
| `created_at` | `timestamptz` | No | Creation time. |
| `deleted_at` | `timestamptz` | Yes | Soft delete. |

Indexes:

```sql
create unique index ux_artifacts_bucket_key
on storage.artifacts(bucket_name, object_key);

create index ix_artifacts_project_type
on storage.artifacts(project_id, artifact_type);
```

---

## 4.11.2 `storage.artifact_versions`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `artifact_id` | `uuid` | No | FK to artifact. |
| `version_number` | `int` | No | Version number. |
| `object_key` | `varchar(1000)` | No | Object key. |
| `checksum` | `varchar(120)` | Yes | Checksum. |
| `size_bytes` | `bigint` | Yes | Size. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.11.3 `storage.artifact_access_logs`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `artifact_id` | `uuid` | No | FK to artifact. |
| `user_id` | `uuid` | Yes | User who accessed. |
| `action` | `varchar(30)` | No | `view`, `download`, `delete`. |
| `ip_address` | `varchar(45)` | Yes | IP address. |
| `correlation_id` | `varchar(80)` | No | Correlation ID. |
| `created_at` | `timestamptz` | No | Access time. |

Partitioning: monthly.

---

## 4.11.4 `storage.scene_diffs`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `repository_id` | `uuid` | No | FK to repository. |
| `scene_path` | `varchar(800)` | No | Scene path. |
| `base_revision` | `varchar(120)` | No | Base commit/branch. |
| `target_revision` | `varchar(120)` | No | Target commit/branch. |
| `base_snapshot_id` | `uuid` | Yes | FK to base snapshot. |
| `target_snapshot_id` | `uuid` | Yes | FK to target snapshot. |
| `job_id` | `uuid` | Yes | FK to job. |
| `artifact_id` | `uuid` | Yes | FK to artifact. |
| `status` | `varchar(30)` | No | `pending`, `completed`, `failed`. |
| `summary` | `jsonb` | Yes | Diff summary. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.11.5 `storage.preview_requests`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `repository_id` | `uuid` | No | FK to repository. |
| `target_type` | `varchar(40)` | No | `asset`, `scene`, `resource`. |
| `target_path` | `varchar(800)` | No | Target path. |
| `job_id` | `uuid` | Yes | FK to job. |
| `artifact_id` | `uuid` | Yes | FK to artifact. |
| `status` | `varchar(30)` | No | Request status. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.11.6 `storage.report_exports`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `report_type` | `varchar(60)` | No | `health`, `audit`, `dependency`. |
| `format` | `varchar(20)` | No | `pdf`, `json`, `csv`. |
| `requested_by` | `uuid` | No | FK to user. |
| `artifact_id` | `uuid` | Yes | FK to artifact. |
| `status` | `varchar(30)` | No | Export status. |
| `created_at` | `timestamptz` | No | Creation time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |

---

# 4.12 Collaboration Schema

The `collab` schema stores notifications, timeline activities, comments, and review threads.

---

## 4.12.1 `collab.notifications`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | No | Recipient user. |
| `project_id` | `uuid` | Yes | Related project. |
| `job_id` | `uuid` | Yes | Related job. |
| `type` | `varchar(60)` | No | Notification type. |
| `title` | `varchar(200)` | No | Title. |
| `message` | `text` | No | Message. |
| `data` | `jsonb` | Yes | Additional data. |
| `is_read` | `boolean` | No | Read status. |
| `read_at` | `timestamptz` | Yes | Read time. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create index ix_notifications_user_read_created
on collab.notifications(user_id, is_read, created_at desc);
```

Partitioning: monthly.

---

## 4.12.2 `collab.notification_preferences`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | No | FK to user. |
| `event_type` | `varchar(80)` | No | Event type. |
| `in_app_enabled` | `boolean` | No | In-app enabled. |
| `email_enabled` | `boolean` | No | Email enabled. |
| `digest_enabled` | `boolean` | No | Digest enabled. |
| `updated_at` | `timestamptz` | No | Update time. |

---

## 4.12.3 `collab.activities`

User-facing activity timeline.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | Yes | FK to project. |
| `user_id` | `uuid` | Yes | Actor user. |
| `action` | `varchar(100)` | No | Action type. |
| `target_type` | `varchar(80)` | Yes | Target type. |
| `target_id` | `uuid` | Yes | Target ID. |
| `metadata` | `jsonb` | Yes | Sanitized metadata. |
| `ip_address` | `varchar(45)` | Yes | IP address. |
| `correlation_id` | `varchar(80)` | No | Correlation ID. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create index ix_activities_project_created
on collab.activities(project_id, created_at desc);

create index ix_activities_user_created
on collab.activities(user_id, created_at desc);
```

Partitioning: monthly.

---

## 4.12.4 `collab.comments`

Generic comments for files, nodes, health issues, and diffs.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `author_id` | `uuid` | No | FK to user. |
| `target_type` | `varchar(80)` | No | `scene_diff`, `health_issue`, `file`, `node`. |
| `target_id` | `uuid` | Yes | Target ID. |
| `body` | `text` | No | Comment body. |
| `status` | `varchar(30)` | No | `active`, `deleted`, `resolved`. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |
| `deleted_at` | `timestamptz` | Yes | Deletion time. |

---

## 4.12.5 `collab.review_threads`

Stores review discussion threads.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | FK to project. |
| `target_type` | `varchar(80)` | No | Review target type. |
| `target_id` | `uuid` | No | Review target ID. |
| `status` | `varchar(30)` | No | `open`, `resolved`, `archived`. |
| `created_by` | `uuid` | No | Creator. |
| `created_at` | `timestamptz` | No | Creation time. |
| `resolved_at` | `timestamptz` | Yes | Resolution time. |

---

## 4.12.6 `collab.review_thread_comments`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `thread_id` | `uuid` | No | FK to review thread. |
| `author_id` | `uuid` | No | FK to user. |
| `body` | `text` | No | Comment body. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |
| `deleted_at` | `timestamptz` | Yes | Soft delete. |

---

# 4.13 Audit Schema

The `audit` schema stores immutable security, admin, and data-access logs.

---

## 4.13.1 `audit.audit_logs`

Append-only audit log for sensitive business events.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | Yes | FK to project. |
| `actor_user_id` | `uuid` | Yes | Actor user. |
| `event_type` | `varchar(120)` | No | Event type. |
| `resource_type` | `varchar(100)` | Yes | Resource type. |
| `resource_id` | `uuid` | Yes | Resource ID. |
| `outcome` | `varchar(30)` | No | `success`, `failure`, `denied`. |
| `ip_address` | `varchar(45)` | Yes | IP address. |
| `user_agent` | `varchar(500)` | Yes | User agent. |
| `correlation_id` | `varchar(80)` | No | Correlation ID. |
| `details` | `jsonb` | Yes | Sanitized details. |
| `created_at` | `timestamptz` | No | Event time. |

Indexes:

```sql
create index ix_audit_logs_org_created
on audit.audit_logs(organization_id, created_at desc);

create index ix_audit_logs_actor_created
on audit.audit_logs(actor_user_id, created_at desc);

create index ix_audit_logs_correlation
on audit.audit_logs(correlation_id);
```

Partitioning: monthly.

---

## 4.13.2 `audit.audit_log_hashes`

Optional tamper-evidence hash chain.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `audit_log_id` | `uuid` | No | PK, FK to audit log. |
| `previous_hash` | `varchar(128)` | Yes | Previous log hash. |
| `current_hash` | `varchar(128)` | No | Current log hash. |
| `algorithm` | `varchar(30)` | No | e.g. `sha256`. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.13.3 `audit.security_audit_events`

Security-specific immutable audit events.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | Yes | User scope. |
| `event_type` | `varchar(120)` | No | Security event. |
| `severity` | `varchar(30)` | No | `info`, `warning`, `critical`. |
| `details` | `jsonb` | Yes | Sanitized details. |
| `correlation_id` | `varchar(80)` | No | Correlation ID. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.13.4 `audit.data_access_logs`

Tracks access to sensitive resources and artifacts.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | Yes | Actor user. |
| `resource_type` | `varchar(100)` | No | Resource type. |
| `resource_id` | `uuid` | Yes | Resource ID. |
| `action` | `varchar(50)` | No | `view`, `download`, `export`, `decrypt`, `admin_access`. |
| `outcome` | `varchar(30)` | No | `success`, `failure`, `denied`. |
| `ip_address` | `varchar(45)` | Yes | IP address. |
| `correlation_id` | `varchar(80)` | No | Correlation ID. |
| `created_at` | `timestamptz` | No | Event time. |

Partitioning: monthly.

---

## 4.13.5 `audit.admin_actions`

Tracks system/support admin actions.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `admin_user_id` | `uuid` | No | Admin user. |
| `target_type` | `varchar(100)` | No | Target type. |
| `target_id` | `uuid` | Yes | Target ID. |
| `action` | `varchar(120)` | No | Admin action. |
| `reason` | `text` | No | Reason required for sensitive actions. |
| `outcome` | `varchar(30)` | No | Result. |
| `correlation_id` | `varchar(80)` | No | Correlation ID. |
| `created_at` | `timestamptz` | No | Creation time. |

---

# 4.14 Search Schema

The `search` schema stores materialized search documents and indexing runs.

---

## 4.14.1 `search.search_documents`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | Project scope. |
| `repository_id` | `uuid` | Yes | Repository scope. |
| `snapshot_id` | `uuid` | Yes | Snapshot scope. |
| `entity_type` | `varchar(50)` | No | `project`, `scene`, `asset`, `script`, `resource`, `node`, `health_issue`. |
| `entity_id` | `uuid` | No | Source entity ID. |
| `title` | `varchar(300)` | No | Search title. |
| `subtitle` | `varchar(500)` | Yes | Subtitle. |
| `content` | `text` | Yes | Searchable content. |
| `path` | `varchar(800)` | Yes | Path. |
| `search_vector` | `tsvector` | Yes | PostgreSQL full-text vector. |
| `metadata` | `jsonb` | Yes | Additional data. |
| `updated_at` | `timestamptz` | No | Update time. |

Indexes:

```sql
create index ix_search_documents_project_type
on search.search_documents(project_id, entity_type);

create index ix_search_documents_vector
on search.search_documents using gin(search_vector);
```

---

## 4.14.2 `search.search_index_runs`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `project_id` | `uuid` | No | Project scope. |
| `snapshot_id` | `uuid` | Yes | Snapshot indexed. |
| `job_id` | `uuid` | Yes | Indexing job. |
| `status` | `varchar(30)` | No | Index status. |
| `document_count` | `int` | No | Indexed document count. |
| `started_at` | `timestamptz` | No | Start time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |

---

## 4.14.3 `search.saved_searches`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `user_id` | `uuid` | No | Owner user. |
| `name` | `varchar(120)` | No | Search name. |
| `query` | `text` | No | Query text. |
| `filters` | `jsonb` | Yes | Saved filters. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |

---

# 4.15 Governance Schema

The `governance` schema stores retention, purge, legal hold, archive, and data classification records.

---

## 4.15.1 `governance.retention_policies`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `scope` | `varchar(40)` | No | `global`, `organization`, `project`, `repository`. |
| `target` | `varchar(120)` | No | Table/data class target. |
| `retention_days` | `int` | No | Retention days. |
| `action` | `varchar(30)` | No | `delete`, `archive`, `anonymize`. |
| `is_enabled` | `boolean` | No | Enabled flag. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |

---

## 4.15.2 `governance.retention_runs`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `policy_id` | `uuid` | No | FK to retention policy. |
| `status` | `varchar(30)` | No | `running`, `completed`, `failed`. |
| `affected_count` | `int` | No | Rows/items affected. |
| `started_at` | `timestamptz` | No | Start time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |

---

## 4.15.3 `governance.retention_run_items`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `retention_run_id` | `uuid` | No | FK to retention run. |
| `target_table` | `varchar(120)` | No | Target table. |
| `target_id` | `uuid` | Yes | Target row/entity ID. |
| `action` | `varchar(30)` | No | `delete`, `archive`, `anonymize`. |
| `status` | `varchar(30)` | No | Item status. |
| `error_message` | `text` | Yes | Error if failed. |

---

## 4.15.4 `governance.archive_records`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `source_table` | `varchar(120)` | No | Original table. |
| `source_id` | `uuid` | Yes | Source row/entity ID. |
| `artifact_id` | `uuid` | Yes | Archive artifact. |
| `archived_at` | `timestamptz` | No | Archive time. |
| `expires_at` | `timestamptz` | Yes | Expiration time. |

---

## 4.15.5 `governance.purge_requests`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `target_type` | `varchar(80)` | No | `user`, `project`, `repository`, `organization`. |
| `target_id` | `uuid` | No | Target entity ID. |
| `reason` | `text` | No | Purge reason. |
| `status` | `varchar(30)` | No | `pending`, `approved`, `running`, `completed`, `failed`, `cancelled`. |
| `requested_by` | `uuid` | No | Requesting user. |
| `approved_by` | `uuid` | Yes | Approving user. |
| `created_at` | `timestamptz` | No | Request time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |

---

## 4.15.6 `governance.legal_holds`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `target_type` | `varchar(80)` | No | Target type. |
| `target_id` | `uuid` | No | Target ID. |
| `reason` | `text` | No | Legal hold reason. |
| `created_by` | `uuid` | No | User who created hold. |
| `created_at` | `timestamptz` | No | Creation time. |
| `released_at` | `timestamptz` | Yes | Release time. |

---

## 4.15.7 `governance.data_classifications`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `schema_name` | `varchar(80)` | No | Schema name. |
| `table_name` | `varchar(120)` | No | Table name. |
| `column_name` | `varchar(120)` | Yes | Column name, null for table-level. |
| `classification` | `varchar(40)` | No | `public`, `internal`, `confidential`, `secret`. |
| `notes` | `text` | Yes | Notes. |
| `created_at` | `timestamptz` | No | Creation time. |

---

# 4.16 Admin / Operations Schema

The `admin` schema supports migrations, deployment history, seed tracking, backfills, system checks, and database maintenance.

---

## 4.16.1 `admin.app_versions`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `service_name` | `varchar(100)` | No | `api`, `worker`, `frontend`. |
| `version` | `varchar(80)` | No | App version. |
| `git_sha` | `varchar(80)` | Yes | Git SHA. |
| `deployed_at` | `timestamptz` | No | Deployment time. |
| `deployed_by` | `varchar(120)` | Yes | CI/CD actor. |

---

## 4.16.2 `admin.migration_runs`

EF Core still uses `__EFMigrationsHistory`, but this table provides production deployment visibility.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `migration_name` | `varchar(200)` | No | Migration name. |
| `migration_version` | `varchar(100)` | No | Migration version. |
| `checksum` | `varchar(120)` | Yes | Migration checksum. |
| `status` | `varchar(30)` | No | `running`, `completed`, `failed`, `rolled_back`. |
| `started_at` | `timestamptz` | No | Start time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |
| `executed_by` | `varchar(120)` | Yes | CI/CD actor. |
| `error_message` | `text` | Yes | Error if failed. |

---

## 4.16.3 `admin.data_backfill_runs`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `name` | `varchar(200)` | No | Backfill name. |
| `status` | `varchar(30)` | No | Backfill status. |
| `processed_count` | `int` | No | Processed rows/items. |
| `failed_count` | `int` | No | Failed rows/items. |
| `started_at` | `timestamptz` | No | Start time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |
| `metadata` | `jsonb` | Yes | Metadata. |

---

## 4.16.4 `admin.seed_history`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `seed_name` | `varchar(200)` | No | Seed name. |
| `checksum` | `varchar(120)` | No | Seed checksum. |
| `applied_at` | `timestamptz` | No | Applied time. |

---

## 4.16.5 `admin.system_health_checks`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `component` | `varchar(100)` | No | `postgres`, `redis`, `rabbitmq`, `minio`, `worker`. |
| `status` | `varchar(30)` | No | `healthy`, `degraded`, `unhealthy`. |
| `details` | `jsonb` | Yes | Details. |
| `checked_at` | `timestamptz` | No | Check time. |

---

## 4.16.6 `admin.db_maintenance_runs`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `maintenance_type` | `varchar(100)` | No | `partition_create`, `partition_drop`, `vacuum`, `reindex`, `analyze`. |
| `target` | `varchar(200)` | Yes | Target table/partition. |
| `status` | `varchar(30)` | No | `running`, `completed`, `failed`. |
| `started_at` | `timestamptz` | No | Start time. |
| `completed_at` | `timestamptz` | Yes | Completion time. |
| `details` | `jsonb` | Yes | Details. |

---

# 4.17 Redis Design

Redis is used only for ephemeral data.

| Key Pattern | Purpose | TTL |
| --- | --- | --- |
| `cache:dashboard:{project_id}` | Dashboard summary | 5 minutes |
| `cache:health:{project_id}:latest` | Latest health score/top issues | 5 minutes |
| `cache:project_members:{project_id}` | RBAC cache | 5 minutes |
| `lock:repo:{repository_id}` | Git workspace lock | 5 minutes |
| `rate:user:{user_id}:{endpoint}` | User rate limit | 1 minute |
| `rate:ip:{ip}:{action}` | Login/password reset rate limit | 1–15 minutes |
| `job:{job_id}:progress` | Realtime progress cache | 1 hour |
| `session:user:{user_id}` | Active session summary | Access token TTL |

Distributed lock pattern:

```text
SET lock:repo:{repository_id} {owner}:{correlation_id} NX EX 300
```

---

# 4.18 MinIO / S3 Bucket Design

| Bucket | Content | Retention |
| --- | --- | --- |
| `diff-artifacts` | Scene diff JSON | 7–30 days |
| `thumbnails` | Asset thumbnails | Repository lifecycle |
| `previews` | Preview artifacts | Repository lifecycle |
| `reports` | Exported health/dependency reports | 30–90 days |
| `archives` | Repository/data archives | Manual/legal policy |
| `audit-exports` | Audit exports | Enterprise policy |

PostgreSQL stores object metadata in `storage.artifacts`; binary content stays in MinIO/S3.

---

# 4.19 High-volume Tables and Partitioning

## Monthly Partitioning

Use monthly partitions for:

```text
identity.login_events
identity.security_events
ops.job_attempts
ops.job_events
collab.notifications
collab.activities
audit.audit_logs
audit.data_access_logs
storage.artifact_access_logs
```

## Repository/Snapshot-oriented Partitioning

Consider partitioning by repository, project, or snapshot for:

```text
repo.repository_files
repo.file_versions
metadata.scene_nodes
metadata.scene_node_properties
metadata.dependencies
analysis.health_issues
```

## Partition Rules

- Every partitioned table must have a retention policy.
- Partition creation/drop must be tracked in `admin.db_maintenance_runs`.
- Partition indexes must match query access patterns.
- Queries must include tenant/project/time/snapshot filters whenever possible.

---

# 4.20 Data Classification

| Classification | Examples | Rules |
| --- | --- | --- |
| `public` | Plan names, app version | Safe for public display. |
| `internal` | Project name, scene name | Limited logs, authenticated access. |
| `confidential` | Repository metadata, commit history, health reports | No public exposure. |
| `secret` | Password hash, token hash, encrypted PAT | Never log, never return in API. |

Secret columns:

```text
identity.users.password_hash
identity.refresh_tokens.token_hash
identity.user_invites.token_hash
identity.password_reset_tokens.token_hash
identity.email_change_requests.token_hash
repo.repository_credential_versions.encrypted_secret
```

Confidential examples:

```text
repo.repositories.remote_url
repo.git_commits.author_email
metadata.scenes.file_path
metadata.scripts.file_path
storage.artifacts.object_key
audit.audit_logs.details
```

---

# 4.21 Backup, Restore, and PITR

| Data Area | Backup Requirement | Restore Priority |
| --- | --- | --- |
| `identity`, `tenant`, `billing`, `core` | PITR + frequent encrypted backups | Highest |
| `repo.repository_credentials` | PITR + encrypted backup | Highest |
| `ops` recent job state | Backup recent state | High |
| `metadata`, `analysis` | Periodic backup; can regenerate from repository | Medium |
| `audit`, `governance` | Long retention archive | High |
| `storage` artifacts | MinIO/S3 bucket backup/lifecycle | Medium/High |
| `search` | Rebuildable | Low |

Production requirements:

- PostgreSQL WAL archiving enabled.
- PITR tested regularly.
- Backup encryption enabled.
- Restore drills documented.
- Secrets and database backups protected separately.
- Audit/security archives must not be silently deleted outside governance policy.

---

# 4.22 Migration Rollout Plan

Do not create one massive migration. Roll out by bounded context.

```text
001_identity_foundation
002_tenant_organization
003_billing_foundation
004_core_projects_rbac
005_repository_git_foundation
006_ops_jobs_messaging
007_metadata_parser_foundation
008_analysis_health_graph
009_storage_artifacts
010_collaboration_notifications
011_audit_security
012_search_index
013_governance_retention
014_admin_operations
```

Each migration must include:

- Forward migration.
- Rollback plan, or explicit reason rollback is not possible.
- Data migration/backfill plan if needed.
- Index creation strategy.
- Downtime expectation.
- Validation query.
- Required seed changes.
- Compatibility note with current application version.

---

# 4.23 Production Acceptance Checklist

```text
[ ] Every table has a clear owner schema and purpose.
[ ] Every table has a primary key.
[ ] Every foreign key is intentional.
[ ] Every critical access pattern has an index.
[ ] Token and secret columns are hash-only or encrypted.
[ ] Git credentials are versioned and rotation-ready.
[ ] Tenant isolation exists through organization_id.
[ ] Organization and project RBAC are both supported.
[ ] Metadata is snapshot-aware and run-aware.
[ ] Scene Flow Table is supported by scene_nodes, scene_node_references, and scene_connections.
[ ] Worker jobs support lease, retry, attempts, timeout, cancellation, inbox/outbox, and DLQ.
[ ] Audit logs are append-only from the application perspective.
[ ] High-volume tables have partitioning and retention strategy.
[ ] Search is materialized, not ad-hoc across all metadata tables.
[ ] MinIO artifact metadata is tracked in PostgreSQL.
[ ] Retention, purge, legal hold, and archive models exist.
[ ] Migration rollout is phased and documented.
[ ] Backup, restore, and PITR requirements are documented.
[ ] Data classification is defined for sensitive tables/columns.
```

---

# 4.24 Implementation Phasing

This design is production-grade, but it should be implemented in phases.

## Phase 1 — Production Foundation

Implement first:

```text
identity.users
identity.user_profiles
identity.user_settings
identity.refresh_tokens
identity.user_sessions
identity.user_invites
identity.login_events
identity.security_events


core.projects
core.project_settings
core.project_members
core.project_member_history

repo.repositories
repo.repository_credentials
repo.repository_credential_versions
repo.repository_snapshots

ops.jobs
ops.job_attempts
ops.job_events
ops.job_leases
ops.outbox_messages
ops.inbox_messages
ops.dead_letter_messages
ops.worker_heartbeats

collab.notifications
collab.activities

audit.audit_logs
audit.security_audit_events
```

## Phase 2 — Repository and Parser Foundation

```text
repo.repository_files
repo.file_versions
repo.git_refs
repo.git_commits
repo.git_commit_files
repo.workspace_states
repo.repository_sync_runs

metadata.metadata_runs
metadata.scenes
metadata.scene_nodes
metadata.scene_node_properties
metadata.scene_node_references
metadata.scene_connections
metadata.assets
metadata.asset_imports
metadata.scripts
metadata.script_symbols
metadata.resources
metadata.dependencies
metadata.parser_diagnostics
```

## Phase 3 — Analysis, Storage, and Search

```text
analysis.analysis_runs
analysis.health_rules
analysis.health_rule_versions
analysis.health_reports
analysis.health_issues
analysis.health_issue_suppressions
analysis.dependency_graph_snapshots
analysis.dependency_graph_nodes
analysis.dependency_graph_edges

storage.artifacts
storage.artifact_versions
storage.artifact_access_logs
storage.scene_diffs
storage.preview_requests
storage.report_exports

search.search_documents
search.search_index_runs
search.saved_searches
```

## Phase 4 — Enterprise Governance and Operations

```text

governance.retention_policies
governance.retention_runs
governance.retention_run_items
governance.archive_records
governance.purge_requests
governance.legal_holds
governance.data_classifications

admin.app_versions
admin.migration_runs
admin.data_backfill_runs
admin.seed_history
admin.system_health_checks
admin.db_maintenance_runs

audit.audit_log_hashes
audit.data_access_logs
audit.admin_actions
```

---

## 4.25 Final Notes

This design intentionally goes beyond the original MVP database document. The original version contained the essential seed tables such as `users`, `refresh_tokens`, `projects`, `project_members`, `repositories`, `jobs`, `activities`, `notifications`, `scenes`, `scene_nodes`, `assets`, `scripts`, `resources`, `dependencies`, `health_reports`, and `health_issues`, plus Redis and MinIO basics. This production-grade version expands that foundation for real SaaS and enterprise usage: tenancy, billing, credential rotation, worker reliability, metadata versioning, auditability, retention, backup, search, and operational governance.

For implementation, do not build everything at once. Start with Phase 1, then proceed to parser/metadata, analysis/storage/search, and finally enterprise governance.

## Deferred / Future Scope — Not for MVP Implementation
> [!WARNING]
> Implementation agents must not scaffold, migrate, or implement deferred tenant.* or billing.* tables for the MVP.

## 4.4.1 `tenant.organizations`

Stores organizations and personal workspaces.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `name` | `varchar(150)` | No | Organization name. |
| `slug` | `varchar(100)` | No | URL-safe organization slug. |
| `type` | `varchar(30)` | No | `personal`, `team`, `enterprise`. |
| `status` | `varchar(30)` | No | `active`, `suspended`, `deleted`. |
| `created_by` | `uuid` | No | FK to `identity.users`. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |
| `deleted_at` | `timestamptz` | Yes | Soft delete. |

Indexes:

```sql
create unique index ux_organizations_slug
on tenant.organizations(slug);

create index ix_organizations_status
on tenant.organizations(status);
```

---

## 4.4.2 `tenant.organization_members`

Stores organization-level RBAC.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `organization_id` | `uuid` | No | FK to `tenant.organizations`. |
| `user_id` | `uuid` | No | FK to `identity.users`. |
| `role` | `varchar(30)` | No | `owner`, `admin`, `billing_admin`, `member`, `viewer`. |
| `status` | `varchar(30)` | No | `active`, `invited`, `removed`. |
| `joined_at` | `timestamptz` | No | Join time. |
| `removed_at` | `timestamptz` | Yes | Removal time. |

Indexes:

```sql
create unique index ux_org_members_active
on tenant.organization_members(organization_id, user_id)
where removed_at is null;

create index ix_org_members_user
on tenant.organization_members(user_id);
```

---

## 4.4.3 `tenant.organization_invites`

Stores organization invitations.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `organization_id` | `uuid` | No | FK to `tenant.organizations`. |
| `email` | `varchar(255)` | No | Invited email. |
| `normalized_email` | `varchar(255)` | No | Normalized email. |
| `role` | `varchar(30)` | No | Requested org role. |
| `token_hash` | `varchar(255)` | No | Invite token hash. |
| `status` | `varchar(30)` | No | `pending`, `accepted`, `expired`, `revoked`. |
| `invited_by` | `uuid` | No | FK to inviter user. |
| `expires_at` | `timestamptz` | No | Expiration time. |
| `accepted_at` | `timestamptz` | Yes | Acceptance time. |
| `created_at` | `timestamptz` | No | Creation time. |

---

## 4.4.4 `tenant.organization_settings`

Stores enterprise policies and defaults.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `organization_id` | `uuid` | No | FK to organizations, unique. |
| `default_project_visibility` | `varchar(30)` | No | `private`, `internal`. |
| `require_mfa` | `boolean` | No | MFA requirement. |
| `allowed_email_domains` | `text[]` | Yes | Allowed domains for enterprise invites. |
| `git_provider_policy` | `jsonb` | Yes | Allowed providers and credential rules. |
| `data_retention_policy_id` | `uuid` | Yes | FK to governance retention policy. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |

---

## 4.4.5 `tenant.teams`

Groups users inside organizations.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `organization_id` | `uuid` | No | FK to organizations. |
| `name` | `varchar(120)` | No | Team name. |
| `slug` | `varchar(120)` | No | Team slug. |
| `description` | `text` | Yes | Description. |
| `created_at` | `timestamptz` | No | Creation time. |
| `updated_at` | `timestamptz` | No | Update time. |

Indexes:

```sql
create unique index ux_teams_org_slug
on tenant.teams(organization_id, slug);
```

---

## 4.4.6 `tenant.team_members`

Stores team membership.

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `team_id` | `uuid` | No | FK to `tenant.teams`. |
| `user_id` | `uuid` | No | FK to `identity.users`. |
| `role` | `varchar(30)` | No | `maintainer`, `member`. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create unique index ux_team_members_team_user
on tenant.team_members(team_id, user_id);
```

---

# 4.5 Billing / License Schema

The `billing` schema supports plans, subscriptions, usage tracking, invoices, and future SaaS monetization.

---

## 4.5.1 `billing.plans`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `code` | `varchar(50)` | No | `free`, `pro`, `team`, `enterprise`. |
| `name` | `varchar(100)` | No | Plan name. |
| `price_monthly` | `numeric(12,2)` | Yes | Monthly price. |
| `currency` | `varchar(10)` | Yes | Currency. |
| `limits` | `jsonb` | No | Project/member/storage/worker limits. |
| `features` | `jsonb` | No | Feature flags. |
| `is_active` | `boolean` | No | Whether plan is available. |
| `created_at` | `timestamptz` | No | Creation time. |

Indexes:

```sql
create unique index ux_plans_code
on billing.plans(code);
```

---

## 4.5.2 `billing.subscriptions`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `organization_id` | `uuid` | No | FK to organizations. |
| `plan_id` | `uuid` | No | FK to plans. |
| `status` | `varchar(30)` | No | `trialing`, `active`, `past_due`, `cancelled`, `expired`. |
| `started_at` | `timestamptz` | No | Start time. |
| `trial_ends_at` | `timestamptz` | Yes | Trial end. |
| `current_period_start` | `timestamptz` | Yes | Billing period start. |
| `current_period_end` | `timestamptz` | Yes | Billing period end. |
| `cancelled_at` | `timestamptz` | Yes | Cancellation time. |
| `provider` | `varchar(50)` | Yes | Payment provider. |
| `provider_subscription_id` | `varchar(200)` | Yes | External provider ID. |

---

## 4.5.3 `billing.usage_counters`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `organization_id` | `uuid` | No | FK to organizations. |
| `period_start` | `date` | No | Usage period start. |
| `period_end` | `date` | No | Usage period end. |
| `metric` | `varchar(80)` | No | `projects`, `members`, `repo_bytes`, `storage_bytes`, `worker_minutes`. |
| `value` | `numeric(20,4)` | No | Usage value. |
| `updated_at` | `timestamptz` | No | Update time. |

Indexes:

```sql
create unique index ux_usage_counters_org_period_metric
on billing.usage_counters(organization_id, period_start, period_end, metric);
```

---

## 4.5.4 `billing.invoices`

| Column | Type | Nullable | Description |
| --- | --- | --- | --- |
| `id` | `uuid` | No | Primary key. |
| `organization_id` | `uuid` | No | FK to organizations. |
| `provider_invoice_id` | `varchar(200)` | Yes | External invoice ID. |
| `status` | `varchar(30)` | No | `draft`, `open`, `paid`, `void`, `uncollectible`. |
| `amount_due` | `numeric(12,2)` | No | Amount due. |
| `currency` | `varchar(10)` | No | Currency. |
| `issued_at` | `timestamptz` | Yes | Issue time. |
| `paid_at` | `timestamptz` | Yes | Payment time. |

---

# 4.6 Core Project Schema

The `core` schema owns projects, project settings, project membership, and project invitations.

---

