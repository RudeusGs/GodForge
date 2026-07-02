# Scene Diff

## Purpose

The Scene Diff module compares changes in Godot scenes based on node/property structures rather than just text diffs, helping reviewers understand `.tscn` changes more accurately.

## Actors

- Developer
- Reviewer/QA
- Viewer

## Scope

- Compare scenes between two commits or branch tips.
- Display added, removed, modified, moved, and unchanged nodes.
- Display property-level diffs.
- Cache diff artifacts in MinIO.
- Fallback to text diff for non-`.tscn` files.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-13 | Scene Diff Viewer | Compare scenes structurally between two revisions. | Must | Viewer+ |
| BR-58 | Diff Cache | Diffs are cached using a key comprising the scene path and two revisions. | Should | Worker |
| BR-59 | Cache Expiry | Diff cache expires after 7 days or upon re-analysis if invalidated. | Should | System |
| BR-60 | `.tscn` Scope | Scene-aware diffing only applies to `.tscn` files; other files use text diffs. | Must | System |
| BR-61 | Async Diff | Diffs run asynchronously and return 202 if computation is needed. | Must | Worker |

## Main Workflow

1. User selects a scene path and two revisions (commit or branch).
2. API checks permissions and validates the scene path within the repository.
3. If the diff artifact is already cached and valid, the API returns the result quickly.
4. If not cached, the API creates a diff job and returns `202 Accepted`.
5. Diff Worker retrieves the two versions of the `.tscn` file.
6. Worker parses the node tree/properties of both versions.
7. Worker generates a diff summary, node-level diff, and property-level diff.
8. The result is saved to MinIO and returned to the UI when the job completes.

## Diff Display

| Change Type | Display |
| --- | --- |
| Added nodes | Highlighted green, showing the new node info. |
| Removed nodes | Highlighted red, showing the node info before deletion. |
| Modified nodes | Highlighted yellow, showing property changes. |
| Moved nodes | Shows old and new parent paths. |
| Unchanged nodes | Collapsed or dimmed by default. |

## Exceptions / Errors

| Situation | HTTP Status / Job State | Error Code | Behavior |
| --- | --- | --- | --- |
| Scene path not in project | 400/403 | `PATH_NOT_ALLOWED` | Deny request. |
| Revision not found | 404 | `REVISION_NOT_FOUND` | Do not create diff. |
| Non-`.tscn` file | 200 | `TEXT_DIFF_FALLBACK` | Return text diff if supported. |
| Parsing one revision fails | Job failed/degraded | `DIFF_PARSE_FAILED` | Return clear error or fallback if possible. |
| Cache artifact missing | 202 | `DIFF_CACHE_MISS` | Recreate the diff job. |

## Acceptance Criteria

- AC-75: Comparing two commits of the same scene displays a node-level diff.
- AC-76: Added nodes are highlighted green and display full information.
- AC-77: Changed properties show old value and new value.
- AC-78: Cached diffs return a fast response in under 500ms.
- AC-79: Non-tscn files fallback to text diff.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{projectId}/diff/scene` | `viewer+` | scenePath, commitA/branchA, commitB/branchB | Diff job or cached result | `REVISION_NOT_FOUND`, `PATH_NOT_ALLOWED` |
| GET | `/api/v1/projects/{projectId}/diff/{diffId}` | `viewer+` | diff id | Diff result | `DIFF_NOT_FOUND` |
| GET | `/api/v1/projects/{projectId}/git/commits/{hash}/diff` | `viewer+` | commit hash | File diff summary | `COMMIT_NOT_FOUND` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `jobs` | Diff job lifecycle, progress, and errors. |
| `repositories` | Repository workspace and default branch. |
| `scenes` | Current scene metadata, path validation. |
| `activities` | Logs scene diff requests if audit policy requires it. |
| MinIO `diff-artifacts` | Stores diff results/cache. |

## Security / Authorization Notes

- Scene paths must be normalized and reside within the repository workspace.
- Diff results may contain source metadata; project permissions must be verified when reading artifacts.
- Do not expose server paths or raw Git stderr.
- The Diff Worker must not execute scene/script contents.
