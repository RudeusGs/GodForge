# Deterministic Godot Parser

## Purpose

Convert supported Godot project files into a normalized, versioned metadata model.

## Actors

Worker and all read-only product modules consuming metadata.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-08.1 | Parse `project.godot`, `.tscn`, `.tres`, `.gd` and supported shader metadata | Must |
| FR-08.2 | Extract scenes, nodes, scripts, resources, assets, signals and references | Must |
| FR-08.3 | Produce normalized JSON and parser diagnostics | Must |
| FR-08.4 | Version parser output and preserve historical runs | Must |

## Main flow

1. Load validated inventory for a commit.
2. Parse supported text formats with bounded readers.
3. Normalize `res://` paths and stable identifiers.
4. Resolve references without executing project code.
5. Batch persist metadata and diagnostics under parser version.
6. Publish completion only after transaction succeeds.

## Error and edge cases

- Unsupported binary resource format.
- Malformed scene/resource/script.
- Encoding/read error or file too large.
- Missing reference becomes a diagnostic/finding rather than arbitrary job failure when safe.
- Parser version mismatch forces new run.

## Authorization and security

- No script/plugin/native-extension execution.
- Parser must not read outside inventory/workspace.
- Diagnostics contain safe evidence only.

## Async processing and idempotency

- Worker parser stage; no direct synchronous parse endpoint.

## Acceptance criteria

- `AC-FR-08-01`: Corpus output is reproducible byte-for-byte after canonical ordering.
- `AC-FR-08-02`: Partial malformed files do not erase valid metadata from other files.
- `AC-FR-08-03`: Historical parser runs remain queryable by revision/version.

## Related API

- Parser results are read through revision scene/asset/graph endpoints; parsing is triggered by analysis jobs.

## Related data

- `metadata.metadata_runs`, `metadata.scenes`, `metadata.scene_nodes`, `metadata.scripts`, `metadata.resources`, `metadata.assets`, `metadata.dependencies`, `metadata.parser_diagnostics`

## Tests and observability

- Test suite: `TC-PARSER-*` with canonical corpus hashes, malformed partial files and unsupported formats.
- Metrics: files parsed/skipped, diagnostics, duration, memory and output size.
- Parser logs contain safe normalized paths and codes, not unrestricted source contents.
