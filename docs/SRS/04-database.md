# 4. Database Schema and Principles

## 4.1 Database Principles
- **Source of Truth**: PostgreSQL is the strict source of truth for business data, configurations, and job states.
- **Relational Integrity**: Use foreign keys for all relations.
- **No Hard Deletes**: Core business entities must use soft-delete (a nullable `deleted_at` column) to preserve history and relations.

## 4.2 PostgreSQL Schemas
- Default schema is `public`.

## 4.3 Naming Conventions
- All tables, columns, and indexes must use `snake_case`.
- Table names should be plural (e.g., `projects`, `users`).
- Foreign key columns must end in `_id` (e.g., `project_id`).

## 4.4 PostgreSQL Type Conventions
- **IDs**: Primary and foreign keys use `uuid`.
- **Timestamps**: All dates and times must use `timestamptz`.
- **JSON**: Arbitrary metadata payloads use `jsonb`.
- **Strings**: Use `text` or `varchar(n)`.
- **Booleans**: Use `boolean`.

## 4.5 Common Columns
- `id` (uuid, primary key)
- `created_at` (timestamptz, not null)
- `updated_at` (timestamptz, not null)
- `deleted_at` (timestamptz, null)

## 4.6 Soft-Delete Rules
- Entities that can be deleted by users must implement soft-delete.
- Queries must globally filter out rows where `deleted_at IS NOT NULL` unless explicitly requesting deleted items.

## 4.7 Timestamp Rules
- All timestamps (`created_at`, `updated_at`, `deleted_at`) must be stored in UTC.

## 4.8 Migration Rules
- Use EF Core code-first migrations.
- Migrations must not be edited once applied. Create new corrective migrations.

## 4.9 Delete Behavior Rules
- `Restrict` or `NoAction` is preferred for relationships that should not cascade.
- Cascade deletes should only be used for strict parent-child owned relationships (e.g., `scene_nodes` cascade when `scenes` is deleted).

## 4.10 Seed Data Rules
- Required system data (e.g., default system roles if any) should be seeded via EF Core's `HasData`.

## 4.11 Indexes and Constraint Naming Conventions
- Foreign Key Constraints: `fk_<table_name>_<referenced_table_name>_<column_name>`
- Primary Key Constraints: `pk_<table_name>`
- Unique Constraints: `uq_<table_name>_<column_name>`
- Indexes: `ix_<table_name>_<column_name>`

## 4.12 Core Business Tables

### `projects`
- **Purpose**: Top-level container for a project.
- **Columns**: `id`, `name`, `description`, `godot_version`, `visibility`, `created_at`, `updated_at`, `deleted_at`
- **PK**: `id`
- **Retention/Soft-Delete**: Soft-delete enabled.
- **Sensitive Data**: None.

### `project_members`
- **Purpose**: Maps users to projects with a role.
- **Columns**: `id`, `project_id`, `user_id`, `role`, `created_at`, `updated_at`
- **PK**: `id`
- **FKs**: `project_id` -> `projects.id`, `user_id` -> `users.id`
- **Indexes**: Unique on `(project_id, user_id)`

## 4.13 Auth/User/Session Tables

### `users`
- **Purpose**: System users.
- **Columns**: `id`, `email`, `password_hash`, `display_name`, `avatar_url`, `system_role`, `status`, `created_at`, `updated_at`, `deleted_at`
- **PK**: `id`
- **Indexes**: Unique on `email`
- **Sensitive Data**: `password_hash` must be bcrypt hashed. Never return `password_hash`.
- **Retention/Soft-Delete**: Soft-delete enabled.

### `refresh_tokens`
- **Purpose**: Token rotation.
- **Columns**: `id`, `user_id`, `token`, `expires_at`, `revoked_at`, `created_at`
- **PK**: `id`
- **FKs**: `user_id` -> `users.id`
- **Sensitive Data**: `token` should be treated as sensitive.

## 4.14 Project/Member/Role Tables
(See `projects` and `project_members` above).

## 4.15 Repository/Credential-Reference Tables

### `repositories`
- **Purpose**: Linked Git repositories for a project.
- **Columns**: `id`, `project_id`, `remote_url`, `default_branch`, `status`, `created_at`, `updated_at`, `deleted_at`
- **PK**: `id`
- **FKs**: `project_id` -> `projects.id`

### `repository_credentials`
- **Purpose**: Credentials for accessing remote Git repos.
- **Columns**: `id`, `repository_id`, `encrypted_token`, `created_at`, `updated_at`
- **PK**: `id`
- **FKs**: `repository_id` -> `repositories.id`
- **Sensitive Data**: `encrypted_token` must be encrypted using AES-256-GCM.

## 4.16 Job Tables

### `jobs`
- **Purpose**: Tracks async background work.
- **Columns**: `id`, `project_id`, `type`, `status`, `progress`, `correlation_id`, `error_code`, `error_message`, `created_at`, `updated_at`
- **PK**: `id`
- **FKs**: `project_id` -> `projects.id`

### `job_events`
- **Purpose**: Log of state changes for a job.
- **Columns**: `id`, `job_id`, `status`, `message`, `created_at`
- **PK**: `id`
- **FKs**: `job_id` -> `jobs.id`

## 4.17 Activity/Audit Tables

### `activities`
- **Purpose**: Audit logging of significant user actions.
- **Columns**: `id`, `project_id`, `user_id`, `action`, `entity_type`, `entity_id`, `metadata` (jsonb), `created_at`
- **PK**: `id`

## 4.18 Notification Tables

### `notifications`
- **Purpose**: System notifications for users.
- **Columns**: `id`, `user_id`, `type`, `title`, `message`, `is_read`, `created_at`
- **PK**: `id`
- **FKs**: `user_id` -> `users.id`

## 4.19 Metadata/Versioning Tables

### `metadata_versions`
- **Purpose**: Tracks parsed metadata generation versions.
- **Columns**: `id`, `project_id`, `commit_hash`, `status`, `created_at`
- **PK**: `id`
- **FKs**: `project_id` -> `projects.id`

## 4.20 Scene/Node/Resource/Script/Asset/Dependency Tables

### `scenes`
- **Purpose**: Parsed Godot scenes.
- **Columns**: `id`, `project_id`, `file_path`, `version_id`, `created_at`
- **PK**: `id`
- **FKs**: `project_id` -> `projects.id`, `version_id` -> `metadata_versions.id`

### `scene_nodes`
- **Purpose**: Hierarchy of nodes within a scene.
- **Columns**: `id`, `scene_id`, `node_name`, `node_type`, `parent_id`, `created_at`
- **PK**: `id`
- **FKs**: `scene_id` -> `scenes.id`

### `resources`
- **Purpose**: Internal or external Godot resources.
- **Columns**: `id`, `project_id`, `resource_path`, `type`, `version_id`, `created_at`
- **PK**: `id`
- **FKs**: `project_id` -> `projects.id`

### `scripts`
- **Purpose**: Godot scripts.
- **Columns**: `id`, `project_id`, `file_path`, `language`, `version_id`, `created_at`
- **PK**: `id`
- **FKs**: `project_id` -> `projects.id`

### `assets`
- **Purpose**: General assets (textures, audio).
- **Columns**: `id`, `project_id`, `file_path`, `asset_type`, `version_id`, `created_at`
- **PK**: `id`
- **FKs**: `project_id` -> `projects.id`

### `dependencies`
- **Purpose**: Relationship graph between scenes, nodes, resources, scripts, assets.
- **Columns**: `id`, `project_id`, `source_id`, `source_type`, `target_id`, `target_type`, `version_id`
- **PK**: `id`

## 4.21 Health/Analyzer Tables

### `health_reports`
- **Purpose**: Result of an analysis job.
- **Columns**: `id`, `project_id`, `commit_hash`, `score`, `created_at`
- **PK**: `id`

### `health_issues`
- **Purpose**: Individual issues found in a health report.
- **Columns**: `id`, `report_id`, `severity`, `type`, `message`, `file_path`, `created_at`
- **PK**: `id`
- **FKs**: `report_id` -> `health_reports.id`

## 4.22 Object Artifact Metadata Tables

### `object_artifacts`
- **Purpose**: Metadata for MinIO blobs (diffs, thumbnails).
- **Columns**: `id`, `project_id`, `job_id`, `bucket_name`, `object_key`, `content_type`, `checksum`, `created_at`
- **PK**: `id`

## 4.23 Redis Usage Boundaries
- Redis is strictly for distributed locks, rate limiting, and short-lived caching (e.g., dashboards). It must not store primary relational data.

## 4.24 RabbitMQ Job-State Boundary
- RabbitMQ acts as the transport layer. PostgreSQL is the single source of truth for `jobs` state.

## 4.25 MinIO Artifact Metadata Rules
- MinIO stores the actual file blobs (diffs, thumbnails). The `object_artifacts` table stores their relational metadata in PostgreSQL.

## 4.26 Credential Encryption/Storage Rules
- Personal Access Tokens (PATs) must be encrypted before database storage via AES-256-GCM. Plaintext credentials must never be logged or stored.

## 4.27 Retention Rules
- Deleted projects and their relational data are soft-deleted and can be hard-deleted via a system cleanup job after a specified retention period (e.g., 30 days).

## 4.28 Backup/Restore Considerations
- PostgreSQL must have daily automated snapshots. MinIO buckets should have lifecycle policies. Soft-deleted entities allow for immediate logical restoration.

## 4.29 Open Decisions
- (None left open for implementation phase).
