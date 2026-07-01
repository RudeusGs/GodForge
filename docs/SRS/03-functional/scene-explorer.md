# Scene Explorer

## Purpose

The Scene Explorer displays Godot scene structures as interactive node trees, helping users understand scenes without opening the Godot Editor.

## Actors

- Developer
- Reviewer/QA
- Viewer

## Scope

- List of parsed scenes.
- Tree view of nodes in a scene.
- Node detail panel showing properties, scripts, signals, and groups.
- Search/filter nodes by name and type.
- Breadcrumbs from the root to the selected node.
- Direct editing of scenes is out of scope.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-09 | Scene Explorer | Display scene trees and node details from parsed metadata. | Must | Viewer+ |
| BR-45 | No Realtime Parse | Scene Explorer reads from the Metadata Schema; it does not parse files on UI request. | Must | System |
| BR-46 | Unparsed Scenes | Scenes without metadata display an empty state and a trigger parse button if authorized. | Must | Viewer+, Developer |

## Main Workflow

1. User opens the project's scene list.
2. Frontend calls the scenes API with pagination/filters.
3. User selects a scene.
4. API returns scene details and the node tree from `scenes`/`scene_nodes`.
5. User clicks a node to view properties, scripts, signals, groups, and children count.
6. User searches/filters nodes; UI highlights results and retains tree context.

## Exceptions / Errors

| Situation | HTTP Status | Error Code | Behavior |
| --- | --- | --- | --- |
| Project not parsed | 409 / 200 empty | `METADATA_NOT_READY` | Display empty state and parse action if authorized. |
| Scene not found | 404 | `SCENE_NOT_FOUND` | Do not return internal paths. |
| User lacks permission | 403 | `FORBIDDEN` | Do not return metadata. |
| Node tree too large | 200 | `PARTIAL_NODE_TREE` | Paginate/lazy load child nodes if necessary. |

## Acceptance Criteria

- AC-57: Opening the scene viewer displays the node tree in the correct structure.
- AC-58: Clicking a node displays its properties, scripts, and signals.
- AC-59: Searching for `Button` highlights matching node types/names.
- AC-60: Unparsed scenes display a message and a trigger parse button for Developer+.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{id}/scenes` | `viewer+` | pagination, search | Scene list | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{id}/scenes/{sceneId}` | `viewer+` | scene id | Scene detail + summary | `SCENE_NOT_FOUND` |
| GET | `/api/v1/projects/{id}/scenes/{sceneId}/nodes` | `viewer+` | tree/flat, search, type | Node tree/list | `SCENE_NOT_FOUND` |
| POST | `/api/v1/projects/{id}/parse` | `developer+` | options | Parse job | `REPO_NOT_READY` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `scenes` | Scene metadata, path, name, node count, file hash. |
| `scene_nodes` | Node tree, node type, parent path, properties, script path, groups. |
| `scripts` | Script details when a node attaches `.gd`. |
| `dependencies` | Resource/script references associated with the scene. |
| `jobs` | Parse job states for empty/loading UI states. |

## Security / Authorization Notes

- Scene paths returned are repository-relative paths.
- Users without project membership must not know if a scene exists.
- Properties may contain sensitive paths/resource names depending on the project; these must be filtered by RBAC.
- The Scene Explorer does not provide edit operations.
