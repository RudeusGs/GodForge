# 0. GodForge Overview

## Document Purpose

This SRS overview defines the product-level requirements for **GodForge**, a web platform for managing, analyzing, reviewing, and auditing Godot projects. The `docs/SRS` folder is the canonical project documentation source and replaces the previous Word-based SRS maintenance flow.

The goal of this document set is to give product, engineering, QA, operations, and stakeholders a shared understanding of system scope, functional requirements, data model, API contracts, security rules, operational requirements, and acceptance criteria.

## Product Statement

GodForge is a web platform that helps Godot development teams manage multiple projects, connect Git repositories, analyze project metadata, and visualize key information such as scene trees, asset usage, dependency graphs, scene-aware diffs, and project health.

GodForge focuses on project management, metadata analysis, change review, collaboration, traceability, and auditability. It does **not** replace the Godot Editor, an IDE, a full Git client, or a game build/export system.

## Product Value

| Value | Description |
| --- | --- |
| Centralized project management | Track Godot projects, repositories, members, project roles, analysis status, jobs, and recent activity in one web interface. |
| Project structure visibility | Parse `.tscn`, `.tres`, `.gd`, and asset files to build the Scene Explorer, Asset Explorer, Dependency Graph, statistics, and health reports. |
| Better change review | Provide Git UI, commit history, and Scene Diff Viewer so reviewers can understand changes by scene/node/property structure instead of relying only on text diffs. |
| Project health monitoring | Detect missing resources, missing scripts, broken references, cyclic dependencies, unused assets, oversized scenes, parser failures, and other metadata issues. |
| Audit and operations | Record Activity Log entries, notifications, job state, correlation IDs, structured logs, metrics, and operational events for troubleshooting and traceability. |

## Product Goals

- Provide a web portal for managing Godot projects and connected Git repositories.
- Support authentication and authorization using system-level and project-level roles.
- Provide Git UI workflows for common operations: status, stage, unstage, commit, push, pull, branch, merge, and commit history.
- Parse Godot metadata from `.tscn`, `.tres`, `.gd`, and asset files.
- Build Scene Explorer, Asset Explorer, Dependency Graph, Project Health, and Dashboard views from parsed metadata.
- Provide a Scene Diff Viewer that compares scene changes by node, property, resource, and structure.
- Provide Dashboard, Notification, and Activity Log features so users can track project state and important events.
- Process heavy operations through asynchronous jobs, RabbitMQ queues, workers, progress reporting, retry policies, timeout handling, and dead-letter handling.
- Preserve traceability from requirement to API, database, UI behavior, logs, tests, and acceptance criteria.

## User Roles

| Role | Primary Goal |
| --- | --- |
| System Admin | Administer users, global settings, storage/queue integrations, system audit logs, and recovery operations. |
| Organization Owner | Business role representing the owner of a team or workspace. In MVP, this role is mapped to project-level `project_owner` or `project_admin`; no separate organization entity is introduced. |
| Project Admin | Manage project settings, repository configuration, members, roles, branch policies, analysis settings, and project-level administrative actions. |
| Developer | Use Git UI, inspect metadata, run parse/analyze jobs, review dependency and health information, and work with repository state. |
| Reviewer / QA | Review commits, scene diffs, dependency impact, health reports, and related activity before accepting changes. |
| Viewer | Read dashboard, scene, asset, dependency, health, activity, and report information within granted permissions. |

## System Boundary

GodForge operates outside the Godot Editor. Users interact with the system through the web frontend. The frontend calls REST APIs under `/api/v1`; the API layer authenticates requests, enforces authorization, normalizes responses, attaches correlation IDs, and delegates business use cases to the Application layer.

Heavy operations such as repository clone/fetch, metadata parse, health analysis, dependency graph generation, scene diff generation, and preview generation must run asynchronously through RabbitMQ and worker handlers. PostgreSQL stores core business data and metadata. Redis is used for cache, rate limiting, job coordination, and distributed locks. MinIO stores generated artifacts such as diff outputs, reports, thumbnails, previews, snapshots, and archives.

## MVP Decisions

- Do not introduce a separate Organization database or API model in MVP. The `Organization Owner` concept remains a documented business role and is mapped to project-level roles.
- Prioritize in-app notification and setup-token flows for MVP. Email/SMTP delivery is optional when infrastructure is available.
- Keep `GodForge.Worker` as a shared worker host for MVP, but structure it internally as independent logical consumers and handlers so each worker type can be split into a separate process later.
- Treat PostgreSQL as the source of truth for durable job state. Queue messages trigger work but do not replace job records.
- Return `202 Accepted` plus `jobId` for API requests that trigger long-running operations instead of blocking the request thread.
- Enforce RBAC on the server side for all project-scoped actions and read models.
- Propagate `correlationId` through API requests, job records, RabbitMQ messages, worker logs, error responses, activity logs, and notifications when applicable.

## Related Documents

| Document | Purpose |
| --- | --- |
| `01-scope.md` | System scope, boundaries, included features, excluded features, and current-version limitations. |
| `02-architecture.md` | Layered architecture, Clean Architecture dependency rules, API/worker flow, integration principles, and asynchronous processing model. |
| `03-functional/` | Detailed functional requirements by module: authentication, project, repository/Git, parser, analyzer, scene/asset explorer, dependency graph, scene diff, dashboard, search, notification, and activity. |
| `04-database.md` | PostgreSQL schema, core data model, metadata model, Redis usage, MinIO object storage, indexes, retention, and migration rules. |
| `05-api.md` | REST API under `/api/v1`, request/response conventions, error format, pagination, async job endpoints, and module endpoints. |
| `06-security.md` | Authentication, authorization, RBAC matrix, JWT rules, password policy, secret management, audit requirements, and threat model. |
| `07-non-functional.md` | Performance targets, reliability, scalability, observability, maintainability, logging, and testing quality attributes. |
| `08-workflows.md` | End-to-end business workflows such as project creation, repository connection, Git operations, analysis, and scene diff review. |
| `09-ui-ux.md` | Screens, navigation, empty/loading/error states, accessibility, and role-based visibility rules. |
| `10-traceability.md` | Mapping between requirements, API, database, UI, jobs, tests, and acceptance criteria. |
| `11-testing-acceptance.md` | Testing strategy, Definition of Done, acceptance criteria, regression expectations, and sign-off rules. |
| `12-worker-processing.md` | Queue topology, job lifecycle, worker state transitions, retry, timeout, DLQ, idempotency, progress reporting, and cancellation. |
| `13-deployment-operations.md` | Local, development, and production deployment requirements, operations, backups, monitoring, and incident handling. |

## Non-Goals for Current Scope

- Do not provide an in-browser code editor.
- Do not provide a scene editor or direct asset editor.
- Do not run the game or export builds in the current scope.
- Do not replace the Godot Editor, a full IDE, or an advanced Git client.
- Do not automatically modify source code, refactor projects, or resolve Git conflicts without explicit user confirmation and a future approved workflow.
- Do not store Git credentials, access tokens, passwords, or secrets in plain text.