# Asset Explorer

## Purpose

Asset Explorer lets project members browse Godot project assets, inspect safe metadata, preview supported asset types, and understand how assets are used by scenes, resources, scripts, and dependency graph entries.

This module is read-only for repository files in the current scope. It must not edit, rename, delete, move, import, or regenerate source assets through the web UI.

## Actors

- Viewer
- Reviewer/QA
- Developer
- Project Admin
- Worker/System

## Scope

Asset Explorer includes:

- Paginated grid/list view of assets within a project and repository/metadata version scope.
- Filtering and sorting by asset type, path, size, usage status, import status, and health status.
- Asset detail view with normalized repository-relative path, type, size, hash, import metadata, dimensions/duration when available, and usage summary.
- Reverse lookup of references from `dependencies` to show which scenes/resources/scripts use an asset.
- Display of unused, missing, broken, or import-related warnings produced by parser/analyzer/health rules.
- Thumbnail or preview display when a preview artifact exists and the current user has permission.
- Empty/loading/stale states when metadata is not ready or an analysis job is still running.

Out of scope:

- Direct asset editing, deletion, moving, renaming, importing, or re-exporting.
- Arbitrary file browsing outside the repository workspace.
- Executing scripts, loading untrusted asset code, or invoking Godot Editor runtime behavior.
- Treating dynamic runtime loads as fully known references. Dynamic load usage may produce false positives for unused detection.

## Functional Requirements

| ID | Requirement | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-10 | Asset Explorer | Browse and inspect asset metadata in a Godot project. | Must | Viewer+ |
| BR-47 | Metadata source | Read asset data from the metadata schema, scoped by project and metadata version. | Must | System |
| BR-48 | Preview artifacts | Generate image thumbnails or lightweight previews through a worker and store artifacts in MinIO when needed. | Should | Worker |
| BR-49 | Usage caveat | Mark unused detection as graph-based and potentially incomplete for dynamic runtime loads. | Should | Viewer+ |
| BR-50 | Permission filtering | Every asset query must enforce project-level RBAC on the server side before returning paths, metadata, or previews. | Must | API |
| BR-51 | Metadata version safety | Asset results must be tied to a metadata version or commit scope so stale parser/analyzer output does not overwrite newer views. | Must | API/Worker |
| BR-52 | Preview safety | Preview generation must never execute repository scripts and must reject unsupported or unsafe file types. | Must | Worker |

## Main Flow

1. The user opens Asset Explorer from a project.
2. The frontend calls the asset list API with pagination, filters, sort order, and optional metadata version/branch/commit scope.
3. The API authenticates the user, checks project membership/RBAC, validates query limits, and reads asset metadata from PostgreSQL.
4. The API returns repository-relative asset metadata, usage summary, health status, and preview availability.
5. The user opens an asset detail panel.
6. The API returns asset details plus reverse dependencies from scenes/resources/scripts and related health issues.
7. If a preview artifact exists, the API returns a short-lived signed URL or proxy URL after permission checks.
8. If a preview is not available but supported, a developer-level action may enqueue a preview job and return `202 Accepted` with a `jobId`.
9. The frontend shows fallback icons, loading states, stale metadata indicators, or actionable parse/analyze/preview actions based on the user role.

## Metadata and Versioning Rules

- Asset Explorer must read from the latest successful metadata version by default.
- A request may specify branch, commit, repository, or metadata version when the API supports historical views.
- Metadata rows must include enough lineage to trace back to project, repository, commit/snapshot, parser version, and job id.
- If a newer metadata version exists, older worker output must not overwrite the latest asset state.
- Missing and unused status should come from dependency/health output, not from ad-hoc heavy computation in the HTTP request.
- Dashboard and list counts may use Redis cache, but PostgreSQL metadata remains the durable source of truth.

## Supported Asset Types

| Type | Extensions | Display Behavior |
| --- | --- | --- |
| Image | `.png`, `.jpg`, `.jpeg`, `.svg`, `.webp` | Thumbnail when available; dimensions when parser/preview worker can extract safely. |
| Audio | `.wav`, `.ogg`, `.mp3` | Basic file info; duration/codec only when safely extracted. |
| Font | `.ttf`, `.otf` | Basic metadata; font metadata if supported by parser/preview worker. |
| Shader | `.gdshader` | File info, path, dependency references; no execution. |
| Godot import metadata | `.import` | Import status, source linkage, warning state. |
| Other | Any allowed project file not classified above | Basic metadata and fallback icon. |

## Usage and Health Status

| Status | Source | Meaning | UI Guidance |
| --- | --- | --- | --- |
| `used` | `dependencies` | At least one parsed scene/resource/script references the asset. | Show reverse references. |
| `unused` | `health_issues` or dependency summary | No known parsed reference points to the asset. | Show caveat about dynamic loads. |
| `missing` | `health_issues` / broken dependency | A scene/resource/script references a path that does not exist in the snapshot. | Show issue severity and referencing object. |
| `broken_import` | parser/analyzer | Import metadata is inconsistent or source file is missing. | Show warning and related file path. |
| `unknown` | metadata not ready or stale | Parser/analyzer has not produced enough data. | Show parse/analyze action when allowed. |

## Preview / Thumbnail Rules

- Preview generation is an async worker task if it can exceed API latency targets.
- The API must return `202 Accepted` with a `jobId` when preview generation is queued.
- The preview worker must store durable artifacts in MinIO before publishing completion events or notifications.
- Preview artifacts must be keyed by project, repository/metadata version, asset id/path hash, content hash, and preview version.
- Preview output must be idempotent: retrying the same job must not create duplicate user-visible artifacts.
- Preview URLs must be short-lived signed URLs or backend-proxied URLs with permission checks.
- Unsupported or unsafe file types must return fallback metadata instead of failing the whole asset view.

## Error Cases

| Situation | HTTP Status / Job State | Error Code | Behavior |
| --- | --- | --- | --- |
| Metadata is not ready | `409` or `200` empty state | `METADATA_NOT_READY` | Show empty state and parse/analyze action when user has permission. |
| Asset does not exist in the requested scope | `404` | `ASSET_NOT_FOUND` | Do not leak server workspace paths. |
| Asset exists but preview is absent | `200` | `THUMBNAIL_NOT_READY` | Show fallback icon and optional preview job action. |
| User lacks project permission | `403` | `FORBIDDEN` | Return no asset metadata. |
| Query exceeds page/timeout limits | `400` or `422` | `ASSET_QUERY_INVALID` | Ask client to reduce filters, page size, or search length. |
| Requested metadata version is stale or unavailable | `409` | `METADATA_VERSION_NOT_READY` | Show current available metadata version and job status when allowed. |
| Preview job fails transiently | Job `retrying` | `JOB_TRANSIENT_FAILURE` | Retry by worker policy and keep fallback icon. |
| Preview job fails permanently | Job `failed` / `dead_lettered` | `PREVIEW_GENERATION_FAILED` | Keep metadata available and expose job error without stack trace. |

All API errors must include `correlationId` and must not expose stack traces, internal server paths, MinIO credentials, Git credentials, or raw parser exceptions.

## Acceptance Criteria

- AC-61: Opening Asset Explorer displays a paginated asset list with preview thumbnail or fallback icon.
- AC-62: Asset detail displays type, size, repository-relative path, hash/import metadata when available, and usage summary.
- AC-63: An asset with no known references is marked `Unused` with a dynamic-load caveat.
- AC-64: A scene/resource/script reference to a missing file is surfaced as a `Missing` warning without exposing server paths.
- AC-65: Viewer+ users can access only assets inside projects they are authorized to view.
- AC-66: Asset list and detail queries support bounded pagination, filter, sort, and search without heavy recalculation in the request.
- AC-67: Preview generation, when needed, is executed through a worker and tracked through the job lifecycle.
- AC-68: Asset results are scoped to a metadata version or commit so stale worker output cannot overwrite newer metadata.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/assets` | `viewer+` | `type`, `status`, `search`, `page`, `pageSize`, `sortBy`, `sortOrder`, optional `metadataVersionId` | Paginated asset list | `METADATA_NOT_READY`, `FORBIDDEN`, `ASSET_QUERY_INVALID` |
| GET | `/api/v1/projects/{projectId}/assets/{assetId}` | `viewer+` | Asset id, optional metadata scope | Asset detail | `ASSET_NOT_FOUND`, `FORBIDDEN` |
| GET | `/api/v1/projects/{projectId}/assets/{assetId}/usage` | `viewer+` | Asset id, filters, pagination | Reverse references from dependency graph | `ASSET_NOT_FOUND`, `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/dependencies` | `viewer+` | `root`, `target`, `type`, pagination | Usage/dependency graph data | `METADATA_NOT_READY` |
| POST | `/api/v1/projects/{projectId}/assets/{assetId}/preview` | `developer+` | Asset id, optional metadata scope | `202` preview job | `ASSET_NOT_FOUND`, `PREVIEW_UNSUPPORTED`, `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/jobs/{jobId}` | Project member | Job id | Job status/progress | `JOB_NOT_FOUND` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `assets` | Asset metadata, repository-relative path, type, size, hash, import info, preview reference, metadata version. |
| `dependencies` | Reverse lookup between scenes/resources/scripts/assets. |
| `health_issues` | Missing, unused, broken import, and other asset-related issues. |
| `metadata_versions` | Commit/snapshot lineage and schema/parser version for metadata. |
| `repositories` | Repository scope and state. |
| `jobs` | Preview/analyze/parse job lifecycle, progress, error code, correlation id. |
| `object_artifacts` or MinIO metadata | Preview/thumbnail artifact references and retention metadata when modeled in DB. |
| `notifications` | User-facing notification records when asset/health events require notification. |
| `activities` / `audit_logs` | Activity for meaningful state-changing operations such as queued preview jobs. |

## Security and Authorization Notes

- Asset paths and dependency edges reveal repository structure; every query must enforce project-level RBAC server-side.
- Never expose server workspace paths, MinIO bucket internals, private object keys, credentials, stack traces, or raw parser exceptions.
- Thumbnail access must use signed short-lived URLs or a backend proxy that re-checks permission.
- Asset Explorer must not read arbitrary paths from client input. All paths must resolve inside the repository workspace and be derived from metadata.
- Analyzer/preview/parser workers must not execute project scripts or untrusted binaries.
- Search/filter endpoints must limit page size, query length, sort fields, and timeout to reduce database DoS risk.
- Logs must include `correlationId`, project id when available, and job id when applicable, but must not include secrets or private tokens.

## Testing Requirements

- Unit test asset query handlers for RBAC, pagination, filtering, sorting, metadata-not-ready, and missing asset cases.
- Unit test usage lookup to ensure dependencies are scoped by project and metadata version.
- Unit test unused/missing status mapping from health issues and dependency summaries.
- Integration test API responses for list/detail/usage endpoints with authorized and unauthorized users.
- Worker test preview idempotency, unsupported file fallback, retry behavior, and completion after artifact commit.
- Security test that server paths, MinIO keys, credentials, and stack traces are never returned to clients.