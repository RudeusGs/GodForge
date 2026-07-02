# 8. Workflows

## Purpose

This document outlines the main business workflows of GodForge to ensure consistent behavior across UI, API, workers, databases, notifications, and activity logs.

## WF-01 Login / Refresh Session

| Step | Description |
| --- | --- |
| 1 | User enters email/password on the Login screen. |
| 2 | API validates input, rate limits, and account status. |
| 3 | Auth Service verifies bcrypt password hash. |
| 4 | If valid, API issues an access token and a refresh token. |
| 5 | Refresh token is stored as a hash in `refresh_tokens`. |
| 6 | API records `user.login.success`; errors record `user.login.failed`. |
| 7 | Upon access token expiration, client calls refresh to obtain a new token pair. |
| 8 | Old refresh token is revoked according to rotation policy. |

Control Point: Do not reveal user existence; do not log tokens/passwords; use correlation ID.

## WF-02 Create Project

| Step | Description |
| --- | --- |
| 1 | User chooses to create a project. |
| 2 | Enters name, description, Godot version, and visibility. |
| 3 | API validates unique name, role, and inputs. |
| 4 | Project Service creates `projects`, `project_settings`, and `project_owner` membership. |
| 5 | Activity Log records `project.created`. |
| 6 | UI navigates to Project Dashboard showing an empty state, prompting repository connection. |

Control Point: Creator is always owner; new project lacks repository/metadata.

## WF-03 Connect Repository

| Step | Description |
| --- | --- |
| 1 | Project Admin opens Repository Settings. |
| 2 | Enters HTTPS remote URL, default branch, and PAT. |
| 3 | API validates URL, permissions, and ensures project has no active repository. |
| 4 | Credential is encrypted and stored as a `credential_ref`. |
| 5 | API creates clone job and returns 202. |
| 6 | Notification/Activity records `repo.connected` or error if failed. |

Control Point: Do not store/log PAT in plaintext; URL must prevent SSRF based on policy.

## WF-04 Initial Clone And Analyze

| Step | Description |
| --- | --- |
| 1 | Clone Worker receives `RepositoryCloneRequested`. |
| 2 | Worker acquires repository lock. |
| 3 | Worker clones/fetches repository into workspace. |
| 4 | Worker updates `repositories.clone_status` and job progress. |
| 5 | If clone succeeds, system triggers initial parse/analyze based on settings. |
| 6 | Parser Worker creates metadata; Analyze Worker creates health report. |
| 7 | Dashboard cache is invalidated/rebuilt; user receives a notification. |

Control Point: Clone auth failures do not retry infinitely; network failures may retry.

## WF-05 Sync Snapshot (Fetch)

| Step | Description |
| --- | --- |
| 1 | Developer triggers a repository sync. |
| 2 | API creates fetch job and returns 202. |
| 3 | Fetch Worker acquires the repository lock. |
| 4 | Worker fetches updates from the remote. |
| 5 | Worker updates repository snapshot metadata (branch heads). |
| 6 | Upon successful fetch, system may trigger parse/analyze depending on settings. |

Control Point: Do not mutate working tree; only fetch snapshot metadata.

## WF-07 Parse Project

| Step | Description |
| --- | --- |
| 1 | Developer triggers parse or system triggers after clone/push. |
| 2 | API creates parse job. |
| 3 | Parser Worker takes snapshot by commit/branch. |
| 4 | Worker parses `.tscn`, `.tres`, `.res`, `.gd`, and asset files. |
| 5 | Worker saves metadata and progress. |
| 6 | Individual file errors are isolated when possible. |
| 7 | Completed job sends notification. |

Control Point: Parser does not execute scripts and cannot read outside workspace.

## WF-08 View Scene

| Step | Description |
| --- | --- |
| 1 | User opens Scene Explorer. |
| 2 | API returns scene list based on permissions. |
| 3 | User selects a scene. |
| 4 | API returns scene details and node tree from metadata. |
| 5 | UI displays tree, node detail, search/filter, and breadcrumb. |

Control Point: If unparsed, UI shows empty state and trigger parse action for Developer+.

## WF-09 View Asset Usage

| Step | Description |
| --- | --- |
| 1 | User opens Asset Explorer. |
| 2 | API returns asset list. |
| 3 | User selects an asset. |
| 4 | API returns asset metadata and usage from `dependencies`. |
| 5 | UI displays asset detail, thumbnail/fallback, usage, and warnings for unused/missing. |

Control Point: Unused assets may have false positives with dynamic loading.

## WF-10 Generate Dependency Graph

| Step | Description |
| --- | --- |
| 1 | User opens Dependency Graph. |
| 2 | API reads project `dependencies` and metadata nodes. |
| 3 | UI renders graph using client-side layout. |
| 4 | User filters by type/depth/root. |
| 5 | Cyclic dependencies are highlighted and linked to health issues if any. |

Control Point: Limit depth/response size to avoid huge payloads.

## WF-11 Run Health Analysis

| Step | Description |
| --- | --- |
| 1 | Developer triggers analyze. |
| 2 | API verifies parse completion. |
| 3 | Analyze Worker reads metadata and runs health rules. |
| 4 | Worker saves `health_reports` and `health_issues`. |
| 5 | Redis dashboard cache is updated/invalidated. |
| 6 | Critical issues generate notifications. |

Control Point: Health reports retain history, do not overwrite old reports.

## WF-12 View Scene Diff

| Step | Description |
| --- | --- |
| 1 | Reviewer selects a scene and two revisions. |
| 2 | API validates path/revision and checks diff cache. |
| 3 | If cache hit, API returns diff result. |
| 4 | If cache miss, API creates diff job. |
| 5 | Diff Worker parses both versions and creates node/property diff. |
| 6 | UI displays added/removed/modified/moved nodes and summary. |

Control Point: Scene-aware diff only applies to `.tscn`; other files fallback to text diff.

## WF-13 Notification And Activity Log

| Step | Description |
| --- | --- |
| 1 | Domain event or worker result occurs. |
| 2 | Notification Service determines recipients by membership/settings. |
| 3 | Activity Log records action if it's an audit event. |
| 4 | Notification saved to DB and sent via SignalR if user is online. |
| 5 | User opens Notification Center or Activity Log to view history. |

Control Point: Logged metadata must be sanitized and contain no credentials/tokens.

- GodForge does not handle advanced Git conflict resolution or mutation from the Web UI. Users resolve conflicts and push changes using their preferred local Git client, then sync snapshots in GodForge for analysis.
