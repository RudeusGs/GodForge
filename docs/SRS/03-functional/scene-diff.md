# Revision and Scene Diff

## Purpose

Compare Godot structure and health between immutable revisions.

## Actors

Viewer and higher roles.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-13.1 | Compare scene nodes, properties, scripts and references | Must |
| FR-13.2 | Compare files, graph impact, findings and scores | Must |
| FR-13.3 | Export bounded diff artifact | Should |

## Main flow

1. User selects two revisions from same repository lineage.
2. API creates/returns diff job when not cached.
3. Worker compares normalized metadata, not only raw text.
4. UI groups added, removed and changed elements and affected dependencies.

## Error and edge cases

- Revisions unavailable or parser versions incompatible.
- Diff too large for inline response.
- One revision has partial/failed metadata.

## Authorization and security

- Both revisions must be accessible under current project permission.
- Large artifacts use authorized MinIO references.
- Diff does not expose protected asset bytes.

## Async processing and idempotency

- Large diff generation is asynchronous and idempotent by revision pair plus versions.

## Acceptance criteria

- `AC-FR-13-01`: Structural changes are stable and attributable to paths/nodes.
- `AC-FR-13-02`: Incompatible versions return actionable state or normalized recomputation path.
- `AC-FR-13-03`: Large diff is generated asynchronously.

## Related API

- Diff create/status/read/export endpoints

## Related data

- `storage.scene_diffs`, `analysis.analysis_runs`, metadata tables, `storage.artifacts`

## Tests and observability

- Test suite: `TC-DIFF-*`, including incompatible versions, partial runs, large artifact and authorization.
- Metrics: diff duration, change count, artifact size and cache hit.
- Golden fixtures verify stable normalized differences.
