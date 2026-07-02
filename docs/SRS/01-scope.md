# 1. System Scope

## Included Scope

GodForge includes the following capabilities in the current SRS scope.

| Scope Area | Included Capabilities |
| --- | --- |
| Authentication and RBAC | Login, logout, token refresh, invitation flow, user profile management, system roles, project roles, and server-side authorization checks for every protected API and worker-triggering action. |
| Project Management | Create, view, update, soft-delete, restore, search, and configure Godot projects; manage project members, roles, visibility, default branch, analysis settings, retention settings, and project status. |
| Repository and Git | Connect HTTPS Git repositories, securely store credential references, clone/fetch repositories, inspect working tree status, stage/unstage files, commit, push, pull, create/switch/delete branches, merge branches, and view commit history. |
| Parser and Metadata | Parse `.tscn`, `.tres`, `.res` where supported, `.gd`, and asset files; extract scenes, nodes, resources, scripts, asset metadata, references, dependencies, parse status, and parse errors. |
| Analyzer and Project Health | Build dependency graphs, detect cyclic or broken dependencies, run health rules, calculate health score, store health reports/history, and provide issue severity, location, and suggestions. |
| Explorer and Visualization | Provide Dashboard, Scene Explorer, Asset Explorer, Dependency Graph, Scene Diff Viewer, health report views, commit detail views, and search/navigation across project metadata. |
| Collaboration, Audit, and Notifications | Record Activity Log entries for important successful and failed actions, create in-app notifications, expose job status and progress through SignalR, and preserve correlation IDs for traceability. |
| Async Worker Processing | Process long-running work through RabbitMQ workers, including clone, fetch, parse, analyze, scene diff, preview, notification dispatch, retry handling, timeout handling, DLQ inspection, and idempotent reprocessing. |
| Operations and Observability | Provide structured logging, metrics, health checks, backup/restore support, deployment guidance, data retention, repository lock behavior, and operational diagnostics. |

## Out of Scope

GodForge does **not** provide the following capabilities in the current version.

- It does not replace the Godot Editor.
- It does not provide a browser-based scene editor or visual editor for directly editing Godot scenes.
- It does not provide a browser-based code editor, IDE, debugger, profiler, or refactoring tool.
- It does not run games, debug runtime gameplay, profile game performance, export builds, sign builds, or publish games.
- It does not automatically modify source code, resources, or scenes to fix health issues without explicit future product approval.
- It does not support Git SSH in the MVP; repository connection uses HTTPS and token-based authentication.
- It does not provide plugin marketplace, AI assistant, desktop client, MCP server, or Godot editor plugin as required MVP capabilities.
- It does not expose server-side repository workspace paths to users or clients.
- It does not store Git credentials in plain text or return secrets through API responses.

## System Boundary

| Boundary | Rule |
| --- | --- |
| Client boundary | Users interact with GodForge through the web frontend. The frontend calls REST APIs under `/api/v1` and receives normalized success/error responses. |
| API boundary | API endpoints handle authentication, authorization, request validation, response shaping, and lightweight orchestration only. Controllers must not contain business logic or perform direct database, Git, Redis, RabbitMQ, or MinIO operations. |
| Async boundary | Long-running work such as clone, fetch, parse, analyze, scene diff, and preview must run through jobs and workers when it can exceed normal request latency. The API should create a durable job record and return `202 Accepted` with `jobId` for asynchronous operations. |
| Repository workspace | Repositories are cloned into server-side workspaces managed by backend services/workers. Users must never access internal repository paths directly. |
| Metadata boundary | Metadata is derived from repository content and can be regenerated. Core business data such as users, projects, memberships, credential references, activities, notifications, and jobs is the source of truth for application behavior. |
| Job state boundary | PostgreSQL is the source of truth for job state, progress, terminal status, error code, timestamps, and correlation ID. RabbitMQ is a delivery mechanism, not the authoritative job store. |
| Git operation boundary | Write or conflict-prone Git operations must go through Git service/worker logic, acquire a distributed repository lock with an owner token, respect cancellation/timeout rules, and record activity logs. |
| Credential boundary | Git credentials and tokens must be encrypted or stored through a secret manager reference. Secrets must not appear in API responses, logs, activity logs, job payloads, or exception messages. |
| External systems | Remote Git providers, SMTP/email, object storage, Redis, RabbitMQ, monitoring tools, and deployment infrastructure are external dependencies. Their failures must be handled gracefully and surfaced through actionable errors. |

## Roles and Access Scope

| Role | Access Scope |
| --- | --- |
| System Admin | Manage users and system configuration, inspect audit/operational data, perform recovery operations, and bypass project-level RBAC only for explicit administrative purposes. |
| project_owner | Business role representing a team or project_owner. In the MVP, this is mapped to project-level `project_owner` or `project_admin`; no standalone organization entity/API is required. |
| Project Admin | Manage project settings, members, repository settings, analysis settings, retention settings, and administrative project actions. |
| Developer | Use Git operations according to granted permissions, trigger parse/analyze jobs, inspect metadata, view dependency/health data, and review scene diffs. |
| Reviewer / QA | View commit history, scene diff, dependency graph, health reports, activities, and review-relevant metadata. Write access is limited to review/QA actions explicitly allowed by RBAC. |
| Viewer | Read-only access to permitted project dashboard, scenes, assets, dependency graph, health report, personal notifications, and visible activity entries. |

All API queries and commands must enforce server-side RBAC using the authenticated user, project scope, target resource, and required permission. UI role visibility is not a security boundary.

## Version 1.0 Limits

- Godot 4.x is the primary target.
- HTTPS Git repositories are the primary repository integration path.
- The MVP is designed for small teams or academic/project environments: approximately 50 concurrent users, 100 projects, and repositories up to 2 GB.
- One project has one primary repository in the MVP.
- Metadata retention may be shorter than core business data retention because metadata can be regenerated from the repository.
- Large metadata views such as scene nodes, assets, dependency graph, commit history, and activity log should support pagination, filtering, lazy loading, or clustering where applicable.
- Heavy operations must be asynchronous when needed to protect API latency and reliability.

## MVP Decisions

- No standalone organization database model or organization API is implemented in the MVP.
- `project_owner` remains a business/documentation role and is mapped to project-level ownership or administration.
- Project soft-delete keeps project data for 30 days before it becomes eligible for hard-delete. The default retention is 30 days and should be configurable per environment.
- Invite and notification capabilities prioritize in-app notifications and setup tokens. SMTP/email delivery is optional when infrastructure is available.
- A shared `GodForge.Worker` host may be used for MVP deployment simplicity, but each queue must still have separate consumer and handler/service abstractions so logical workers can be split later.
- Completion notifications/events must only be emitted after durable output and job state have been committed.