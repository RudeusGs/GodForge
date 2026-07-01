# Parser / Metadata

## Purpose

The Parser module extracts metadata from Godot repositories for use by the Scene Explorer, Asset Explorer, Dependency Graph, Scene Diff, and Project Health modules without requiring real-time parsing.

## Actors

- Developer
- Project Admin
- Worker/System

## Scope

- Parse `.tscn` scene files.
- Parse `.tres`/`.res` resource files to extract necessary metadata.
- Parse `.gd` script files to extract classes, extends, preload/load, and basic statistics.
- Detect common asset files.
- Save metadata to the Metadata Schema.
- Support incremental parsing based on file hashes.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-08 | Scene Parser | Worker parses `.tscn`, `.tres`, `.gd`, and asset metadata from the repository workspace. | Must | Developer, Worker |
| BR-41 | Isolate File Errors | A parsing error on a single file must not fail the entire job if partial mode can continue. | Must | Worker |
| BR-42 | Async Parsing | Parsing jobs run through RabbitMQ Workers. | Must | System |
| BR-43 | Incremental Parsing | Unchanged files (based on hash) do not need to be re-parsed. | Should | Worker |
| BR-44 | Reproducible Metadata | Parser output is saved to the Metadata Schema and can be regenerated from the repository. | Must | Worker |

## Main Workflow

1. User or system triggers a parse/analyze job.
2. API verifies that the project has a ready repository and the actor has `developer+` permission.
3. API creates a `parse` job and publishes a message to RabbitMQ.
4. Parser Worker takes a snapshot by branch/commit to avoid branch head changes during the job.
5. Worker scans for `.tscn`, `.tres`, `.res`, `.gd`, and asset files.
6. Worker calculates file hashes, skipping unchanged files for incremental parsing.
7. Worker parses metadata and upserts into `scenes`, `scene_nodes`, `assets`, `scripts`, `resources`, and `dependencies` tables.
8. Worker updates progress via the job record and SignalR.
9. Upon completion, the job sends a notification and may trigger an analyze job if configured.

## Extracted Parser Data

| File Type | Metadata |
| --- | --- |
| `.tscn` | Scene name/path, format version, node count, node tree, script paths, groups, signals, important properties. |
| `.tres`/`.res` | Resource path, resource type, key properties, external references. |
| `.gd` | Script path, `class_name`, `extends`, preload/load dependencies, line count. |
| Asset | Path, filename, asset type, file size, MIME/dimensions if available, hash, and thumbnail path if generating a preview. |

## Exceptions / Errors

| Situation | Job/API | Error Code | Behavior |
| --- | --- | --- | --- |
| Repository not connected | 400 | `REPO_NOT_CONNECTED` | Do not create a parse job. |
| Repository not ready | 400 | `REPO_NOT_READY` | Require clone/sync first. |
| Corrupt `.tscn` file | File warning | `PARSE_INVALID_SCENE` | Skip file, log a warning, continue with other files if possible. |
| File too large | File warning | `PARSE_FILE_TOO_LARGE` | Skip file based on configured limits. |
| Unsupported encoding | File warning | `PARSE_ENCODING_UNSUPPORTED` | Detect/fallback, skip if failed. |
| Temporary DB error | Job retry | `JOB_TRANSIENT_FAILURE` | Retry according to worker policy. |

## Acceptance Criteria

- AC-53: Parsing a project with 100 scenes saves all scenes and node trees to the DB.
- AC-54: A corrupt `.tscn` file is skipped, while valid files are successfully parsed.
- AC-55: Re-parsing a project only parses files that have changed based on their hash.
- AC-56: Parse jobs display progress in real-time.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{id}/parse` | `developer+` | branch/commit/options | `202` parse job | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{id}/jobs/{jobId}` | Project member | job id | Job status/progress | `JOB_NOT_FOUND` |
| GET | `/api/v1/projects/{id}/scenes` | `viewer+` | pagination/filter | Parsed scenes | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{id}/assets` | `viewer+` | pagination/filter | Parsed assets | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{id}/scripts` | `viewer+` | pagination/filter | Parsed scripts | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{id}/resources` | `viewer+` | pagination/filter | Parsed resources | `METADATA_NOT_READY` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `jobs` | Stores parse job status, progress, errors, and correlation id. |
| `repositories` | Provides workspace path, default branch, and repository state. |
| `scenes` | Stores `.tscn` scene metadata. |
| `scene_nodes` | Stores the node tree for scenes. |
| `assets` | Stores asset metadata and thumbnail references. |
| `scripts` | Stores `.gd` metadata. |
| `resources` | Stores `.tres`/`.res` metadata. |
| `dependencies` | Stores dependencies discovered during parsing. |

## Security / Authorization Notes

- The parser must only read within the normalized workspace; do not follow symlinks or paths escaping the root.
- File paths returned to the client are repository-relative, not absolute server paths.
- Malicious Godot files must be handled as untrusted input: apply size limits, timeouts, memory guards, and do not execute scripts.
- The parser does not run GDScript.
- Job results must be filtered by project permissions.
