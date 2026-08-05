# Search

## Purpose

Find authorized projects, revisions, scenes, scripts, assets, findings and commits.

## Actors

Authenticated users with resource access.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-15.1 | Project-scoped metadata search | Must |
| FR-15.2 | Organization/global authorized search | Should |
| FR-15.3 | Saved search/filter | Could |

## Main flow

Query is normalized, length-limited and filtered by tenant permissions before results are returned. Results include type, safe highlight and navigation target.

## Error and edge cases

- Query too short/long.
- Unsupported filter/sort.
- Search index stale or unavailable.
- Result resource removed after index.

## Authorization and security

- Authorization is applied before/within search, never only after returning IDs.
- Highlights are escaped.
- Private source content is not globally indexed without policy.

## Async processing and idempotency

- Index refresh may run asynchronously after analysis completion.

## Acceptance criteria

- `AC-FR-15-01`: Non-member cannot discover private project names or metadata.
- `AC-FR-15-02`: Pagination and query timeout are enforced.
- `AC-FR-15-03`: Stale result resolves safely to not found.

## Related API

- `GET /api/v1/search`, project-scoped search and saved-search endpoints

## Related data

- `search.search_documents`, `search.search_index_runs`, `search.saved_searches`

## Tests and observability

- Test suite: `TC-SEARCH-*`, including tenant isolation, stale index, escaping, limits and timeout.
- Metrics: query latency, result count, timeout, indexing lag and authorization-filter rejection.
- Indexing traces never record Restricted source content.
