# Search

## Purpose

Search helps users quickly find projects, scenes, nodes, assets, scripts, and commits within the scope they are authorized to access.

## Actors

- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Scope

- Full-text search on metadata and project fields.
- Filter by project, type, and date.
- Pagination.
- Mandatory RBAC filtering.
- Full source code content search (beyond parsed metadata) is out of scope.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-15 | Search | Find projects, scenes, nodes, assets, scripts, and commits according to permissions. | Should | Viewer+ |
| BR-17 | RBAC Search | Search results only include data from projects the user is authorized for. | Must | System |
| BR-18 | Query Sanitization | Queries are capped at 200 characters and sanitized/parameterized to prevent injection. | Must | System |

## Main Workflow

1. User enters a query and filters.
2. API validates query length, page size, and filters.
3. Backend determines the project scope the user is allowed to view.
4. Search query runs on PostgreSQL full-text/search indexes or optimized metadata queries.
5. API returns paginated results and facets if available.

## Exceptions / Errors

| Situation | HTTP Status | Error Code | Behavior |
| --- | --- | --- | --- |
| Query empty or too long | 400 | `VALIDATION_ERROR` | Return field error. |
| Project filter outside permissions | 403 / empty | `FORBIDDEN` | Do not return data for that project. |
| Page size exceeds limit | 400 | `VALIDATION_ERROR` | Max page size is 100. |
| Search index not ready | 200 degraded | `SEARCH_INDEX_NOT_READY` | Fallback to metadata queries or empty state. |

## Acceptance Criteria

- AC-21: Searching for `Player` returns scenes, nodes, or scripts containing `Player` within authorized scopes.
- AC-22: If a user lacks permission for Project A, the results will not contain data from Project A.
- AC-23: Results are paginated correctly.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/search` | Authenticated | q, type, projectId, page, pageSize | Search results, pagination | `VALIDATION_ERROR` |
| GET | `/api/v1/projects/{projectId}/scenes` | `viewer+` | search/filter | Scene results | `FORBIDDEN` |
| GET | `/api/v1/projects/{projectId}/assets` | `viewer+` | search/filter | Asset results | `FORBIDDEN` |
| GET | `/api/v1/projects/{projectId}/git/commits` | `viewer+` | author/date/search | Commit results | `REPO_NOT_READY` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `projects` | Project name/slug searches. |
| `project_members` | RBAC scope limits. |
| `scenes`, `scene_nodes` | Scene/node searches. |
| `assets`, `scripts`, `resources` | Metadata searches. |
| `repositories` | Commit history scopes and repo states. |

## Security / Authorization Notes

- Queries must use parameterized SQL or safe ORM expressions.
- Search results must be filtered by permission before being returned to the client.
- Snippets containing secrets or internal server paths must not be returned.
