# Dependency Graph

## Purpose

The Dependency Graph visualizes the relationships between scenes, scripts, resources, and assets to help users understand the project structure and detect risks such as missing/cyclic dependencies.

## Actors

- Developer
- Reviewer/QA
- Viewer

## Scope

- Display nodes and edges from `dependencies`.
- Filter by type, depth, and root.
- Focus mode for incoming/outgoing dependencies.
- Highlight cyclic dependencies.
- Direct editing of dependencies is out of scope.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-11 | Dependency Graph | Build and visualize dependency graphs between resources in a Godot project. | Must | Viewer+ |
| BR-50 | Data Source | Graph data is read from the `dependencies` table. | Must | System |
| BR-51 | Project Scope | The graph only displays data belonging to the current project. | Must | System |
| BR-52 | Analyze Required | Projects that have not been analyzed display an empty state and an analyze action if authorized. | Must | Viewer+, Developer |
| BR-53 | Client Layout | The graph layout uses a force-directed layout on the client. | Should | Frontend |

## Main Workflow

1. User opens the Dependency Graph.
2. Frontend calls the dependencies API with default filters.
3. API returns nodes/edges filtered by project permissions.
4. UI renders the graph, legend, and detail panel.
5. User filters by file type, depth, or selects a root.
6. User clicks a node to highlight incoming/outgoing edges.
7. Cyclic dependencies are highlighted and linked to health issues if they exist.

## Dependency Types

| From | To | Relation |
| --- | --- | --- |
| Scene | Scene | `instances` |
| Scene | Script | `attaches` |
| Scene | Resource | `uses` |
| Scene | Asset | `references` |
| Script | Script | `extends`, `preload`, `load` |
| Script | Scene | `preload`, `load` |
| Script | Asset | `preload`, `load` |
| Resource | Asset | `references` |

## Exceptions / Errors

| Situation | HTTP Status | Error Code | Behavior |
| --- | --- | --- | --- |
| Project not analyzed | 409 / 200 empty | `ANALYZE_REQUIRED` | Empty state, CTA to analyze for Developer+. |
| Root node does not exist | 404 | `DEPENDENCY_ROOT_NOT_FOUND` | Do not return the graph. |
| Filter depth too large | 400 | `VALIDATION_ERROR` | Limit depth to avoid overly large responses. |
| User lacks permission | 403 | `FORBIDDEN` | Do not return the graph. |

## Acceptance Criteria

- AC-65: Opening the Dependency Graph displays an interactive graph with nodes and edges.
- AC-66: Filtering by scene only shows scene nodes and relevant connections.
- AC-67: If a cyclic dependency A -> B -> A exists, the cycle is highlighted.
- AC-68: Clicking a node toggles a focus mode for incoming/outgoing edges.
- AC-69: An unanalyzed project displays a message and a button to trigger analysis for Developer+.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/dependencies` | `viewer+` | root, depth, type | Graph nodes/edges | `ANALYZE_REQUIRED` |
| GET | `/api/v1/projects/{projectId}/health/{reportId}/issues` | `viewer+` | issue type | Cycle/missing issues | `REPORT_NOT_FOUND` |
| POST | `/api/v1/projects/{projectId}/analyze` | `developer+` | options | Analyze job | `PARSE_REQUIRED` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `dependencies` | Graph edges and relation types. |
| `scenes`, `scripts`, `resources`, `assets` | Node labels, types, and metadata. |
| `health_issues` | Cyclic/missing issue links. |
| `jobs` | Analyze job state. |

## Security / Authorization Notes

- The graph can reveal source/asset structures, so project RBAC must be enforced.
- Query depth and page/size must be limited to prevent DoS.
- APIs must not return server workspace paths.
