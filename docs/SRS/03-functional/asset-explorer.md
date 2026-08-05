# Asset Explorer

## Purpose

Present asset metadata, usage, duplicates, health and visibility without confusing repository assets with Asset Vault objects.

## Actors

Viewer and higher roles; protected preview additionally requires asset policy access.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-10 | Browse and filter parsed project assets | Must |
| FR-10.1 | Display usage, hash, type, size and health status | Must |
| FR-10.2 | Preview supported safe formats | Should |
| FR-10.3 | Link to Asset Vault policy when applicable | Must |

## Main flow

1. User selects revision and filters by type, path, usage, visibility or finding severity.
2. API returns paged metadata.
3. User views references, duplicate groups and findings.
4. Supported preview is loaded through authorized artifact/asset URL.

## Error and edge cases

- Asset metadata missing or stale.
- Preview unsupported, failed or quarantined.
- Asset is private to another member/policy.
- False-positive unused classification.

## Authorization and security

- Asset list never grants object access by itself.
- Signed preview/download is issued after current authorization.
- File names and metadata are escaped.
- Private object IDs are not enumerable.

## Async processing and idempotency

- No module-specific durable job is created by read operations.
- Writes, where present, use normal transaction/concurrency rules and do not perform hidden heavy work.

## Acceptance criteria

- `AC-FR-10-01`: Unused/duplicate status links to evidence.
- `AC-FR-10-02`: Protected preview fails after permission revocation.
- `AC-FR-10-03`: Large result sets remain paginated and filterable.

## Related API

- `GET /revisions/{sha}/assets`, preview and Asset Vault endpoints

## Related data

- `metadata.assets`, `metadata.dependencies`, `analysis.health_findings`, `storage.asset_objects`, `storage.asset_versions`

## Tests and observability

- Test suite: `TC-ASSET-EXP-*`, including pagination, visibility and revoked-preview cases.
- Metrics: query latency, result count, preview failures and authorization denials.
- Query-count tests prevent N+1 access across asset/reference/finding projections.
