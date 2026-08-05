# 4. Database Model

PostgreSQL is the durable source of GodForge business state. This document defines the target logical model; exact EF names may differ only with documented mapping. The implementation-ready M1 physical schema is defined in `04-database-m1-physical.md`.

## 4.1 Global rules

- UUID primary keys generated server-side.
- UTC timestamps: `created_at`, `updated_at`; soft-deletable rows include `deleted_at`.
- Tenant-owned rows carry `organization_id` and/or `project_id` where appropriate.
- Every project belongs to one organization. Every active project member must have an active organization membership in the same organization. `project_members.organization_id` is retained to support composite tenant foreign keys and safe query scope.
- Organization roles and project roles are separate. Organization administrative membership does not imply project-content membership.
- Foreign keys use restrictive deletion by default; explicit cascade only for safe dependent data.
- Sensitive credentials are encrypted or reference a secret vault; never stored in clear text.
- Large binaries/source payloads are not stored in PostgreSQL.
- JSONB is allowed for versioned provider/evidence payloads with documented schema and size limits, not as a substitute for core relational constraints.

## 4.2 Schemas and tables

### `identity`

| Table | Key fields and constraints | Important indexes/retention |
|---|---|---|
| `users` | email normalized unique, password hash, status, verified timestamp | normalized email unique; retain account/audit references after deactivation |
| `refresh_tokens` | user, token hash, family ID, expiry, revoked/replaced | token hash unique; expire/purge by policy |
| `user_sessions` | user, device metadata, last seen, revoked | user + active; short retention after revocation |
| `login_events` | user/email hash, outcome, IP hash, user agent | time/user; security retention |
| `security_events` | actor, subject, type, safe metadata | append-oriented |
| `user_settings` | user unique, preferences/version | user unique |

### `core`

| Table | Key fields and constraints | Important indexes |
|---|---|---|
| `organizations` | slug unique, owner, status, plan/quota ref | slug unique, status |
| `organization_members` | organization + user unique, role, status | active membership by user/org |
| `organization_invites` | org, email, token hash unique, expiry | active email/org |
| `organization_settings` | organization unique, effective-policy version | organization unique |
| `projects` | organization, slug unique per org, name, visibility, status | org+slug unique, status, owner |
| `project_members` | project + user unique, role, removed_at | active project membership |
| `project_member_history` | project, user, role/action/actor | project+time |
| `project_invites` | optional later-milestone project invitation; M1 adds only active organization members directly | project+email active |
| `project_settings` | project unique, analysis/AI/asset policy refs | project unique |

### `repo`

| Table | Key fields and constraints | Important indexes |
|---|---|---|
| `repositories` | project unique active, mode, provider, sanitized URL, provider ID, default branch, status | project unique active, provider ID |
| `repository_credentials` | repository, encrypted payload/vault ref, key version | repository active version |
| `repository_credential_versions` | credential, version, rotated/revoked | credential+version unique |
| `git_refs` | repository, ref name, commit SHA | repository+ref unique |
| `git_commits` | repository, SHA, parent JSON/ref, author, time, message summary | repository+SHA unique, time |
| `repository_snapshots` | repository, commit SHA, inventory hash, status | repository+SHA unique |
| `repository_files` | snapshot, normalized path, type, size, content hash | snapshot+path unique, hash |
| `file_versions` | repository, path, commit SHA, hash | repository+path+SHA unique |
| `repository_sync_runs` | repository, job, result, refs/size summary | repository+time |
| `webhook_events` | provider, event ID, repository, signature state, payload hash | provider+event ID unique; retention |
| `workspace_states` | repository, last job, cleanup state | repository unique |
| `protected_branches` | repository, pattern, policy version | repository+pattern unique |

### `metadata`

| Table | Key fields and constraints | Important indexes |
|---|---|---|
| `metadata_runs` | repository/snapshot, parser version, input hash, status | unique analysis identity |
| `scenes` | run, normalized path, root metadata | run+path unique |
| `scene_nodes` | scene, stable path/ID, type, parent | scene+stable path unique |
| `scene_node_properties` | node, key, normalized value/type | node+key |
| `scene_connections` | scene, source, signal, target, method | scene/source/target |
| `scene_node_references` | node, target path/type | run+target path |
| `scripts` | run, path, class/extends, hash, metrics | run+path unique |
| `script_symbols` | script, name, kind, line range | script+name/kind |
| `resources` | run, path, type, hash | run+path unique |
| `assets` | run, path, media type, size, hash, dimensions/duration | run+path, hash |
| `dependencies` | run, source ID/path, target ID/path, edge type | run+source+target+type unique |
| `parser_diagnostics` | run, rule/code, severity, path, location, evidence | run+severity/path |

### `analysis`

| Table | Key fields and constraints | Important indexes |
|---|---|---|
| `validation_runs` | snapshot, validator version, profile, input hash, status | identity unique |
| `validation_findings` | run, rule, path, severity, evidence | run+rule/path |
| `analysis_runs` | snapshot, metadata run, rule/profile versions, mode full/incremental, status | deterministic identity unique |
| `health_reports` | analysis run unique, score/category scores, completeness | analysis run unique |
| `health_findings` | report, stable finding key, rule version, severity, path/location, evidence | report+finding key unique; severity/path |
| `health_rules` | stable rule key, category, default severity | rule key unique |
| `health_rule_versions` | rule, version, config/hash, active range | rule+version unique |
| `health_issue_suppressions` | project/rule/finding scope, reason, expiry, actor | active suppression lookup |
| `dependency_graph_snapshots` | metadata/analysis run, graph version, checksum | run unique |
| `dependency_graph_nodes` | snapshot, stable node key, type, label/ref | snapshot+key unique |
| `dependency_graph_edges` | snapshot, source, target, type | snapshot+source+target+type unique |
| `ai_analysis_runs` | deterministic analysis, provider/model/prompt/input hash, status, usage | AI identity unique |
| `ai_findings` | AI run, category, summary, evidence refs, confidence | AI run/category |

### `storage`

| Table | Key fields and constraints | Important indexes |
|---|---|---|
| `artifacts` | project, object key, type, checksum, size, status, retention | object key unique, project/type/time |
| `artifact_versions` | artifact, version, object key/checksum | artifact+version unique |
| `artifact_access_logs` | artifact, actor, outcome, time | artifact/time, actor/time |
| `asset_objects` | project, logical asset ID, owner, current version, status | project+ID unique |
| `asset_versions` | asset, version, object key, hash, media/size, quarantine | asset+version unique, hash |
| `asset_permissions` | asset/version optional, subject type/ID, permission, expiry | asset+subject unique active |
| `asset_manifests` | repository/snapshot or project manifest version, path, asset/version, checksum | manifest+path unique |
| `asset_download_audits` | asset/version, actor, result, time | asset/time, actor/time |
| `asset_licenses` | asset/version, SPDX/custom metadata | asset/version |
| `report_exports` | project, revisions, format, job, artifact, status | project+time, idempotency key |
| `scene_diffs` | base/head revisions, versions, artifact/summary | diff identity unique |

### `collab`

| Table | Key fields and constraints | Important indexes |
|---|---|---|
| `finding_assignments` | finding identity/project, assignee, priority/due | active assignee/status |
| `finding_comments` | finding identity, author, body, edited | finding+time |
| `finding_status_history` | finding identity, from/to, actor, revision ref | finding+time |
| `notifications` | recipient, type, project, read/status, dedupe key | recipient+unread, dedupe unique |
| `notification_preferences` | user unique, channels/types | user unique |
| `activities` | project, actor, action, target, status, correlation | project+time, actor+time |

### `ops`, `audit`, `governance`, `search`

- `ops.jobs`, `job_attempts`, `job_events`, `job_cancellations`, `job_leases`, `job_dependencies`, `outbox_messages`, `inbox_messages`, `dead_letter_messages`.
- `audit.audit_logs`, `audit.audit_log_hashes`, `audit.security_audit_events`, `audit.data_access_logs`.
- `governance.retention_policies`, `retention_runs`, `retention_run_items`, `purge_requests`, `legal_holds`, `archive_records`.
- `search.search_documents`, `search_index_runs`, `saved_searches`.

## 4.3 Query/index requirements

- All project-scoped high-volume tables index `project_id` plus primary filter/time.
- Commit feeds index `(repository_id, committed_at desc, sha)`.
- Job feeds index `(project_id, status, created_at desc)` and `(status, heartbeat_at)`.
- Findings index report, severity, category, path and stable key.
- Avoid unbounded text search without dedicated index/read model.

## 4.4 Migration rules

- Forward migrations only; never delete applied production migrations.
- Migration includes constraints/indexes and data backfill strategy.
- Destructive change requires backup, compatibility window and ADR when architectural.
- Fresh database and upgrade-from-prior-release are both tested.
- Production migrations run as a controlled release step. API replicas do not independently auto-migrate on startup.

## 4.5 M1 implementation gate

Before M1 code or migrations are written, use `04-database-m1-physical.md` as the authoritative column/constraint/index design for identity, organization, project, membership, invitation, audit and outbox tables. Any deviation requires the physical document and traceability matrix to be updated first.