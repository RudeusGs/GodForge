# Scene Explorer

## Purpose

Allow authorized users to inspect scene structure, node properties, attached scripts, signals and references for an immutable revision.

## Actors

Viewer and higher project roles.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-09 | Browse parsed scenes and node tree | Must |
| FR-09.1 | Filter/search nodes and inspect references | Must |
| FR-09.2 | Navigate from scene elements to source/dependency context | Should |

## Main flow

1. User selects revision and scene.
2. API returns bounded scene summary and paginated/lazy node tree.
3. UI displays node type, path, properties, script and reference links.
4. User navigates to dependency graph or text blob when authorized.

## Error and edge cases

- Revision or parser output absent.
- Scene malformed/partial.
- Extremely large scene requires lazy loading.
- Source blob unavailable due to type/size policy.

## Authorization and security

- Project read permission required.
- Render repository text as escaped content.
- Do not expose protected asset URL unless separately authorized.

## Async processing and idempotency

- No module-specific durable job is created by read operations.
- Writes, where present, use normal transaction/concurrency rules and do not perform hidden heavy work.

## Acceptance criteria

- `AC-FR-09-01`: Scene with thousands of nodes remains usable through limits/lazy loading.
- `AC-FR-09-02`: Node paths and references match normalized parser output.
- `AC-FR-09-03`: Unauthorized users cannot infer scene names through error differences.

## Related API

- `GET /projects/{projectId}/revisions/{sha}/scenes`, scene detail/node endpoints

## Related data

- `metadata.scenes`, `metadata.scene_nodes`, `metadata.scene_node_properties`, `metadata.scene_connections`, `metadata.scene_node_references`

## Tests and observability

- Test suite: `TC-SCENE-*`, including large-scene paging, malformed partial scene and masked access.
- Metrics: scene/node query latency, node count, page count and source-blob rejection.
- Query-count tests prevent N+1 property/reference loading.
