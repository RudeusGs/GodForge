---
name: asset-vault
description: Implement Asset Vault upload, versioning, policy, manifest or download.
---

# Asset Vault

## Use when

Implement Asset Vault upload, versioning, policy, manifest or download.

## Required reading

- `docs/SRS/03-functional/asset-vault.md`
- ADR 0008
- `docs/DATA_CLASSIFICATION.md`
- `docs/THREAT_MODEL.md`

## Workflow

1. Define logical asset/version/checksum and visibility policy.
2. Validate upload size, MIME/magic, hash and quarantine state.
3. Store bytes privately in MinIO and metadata atomically/reconciliably.
4. Update manifest with optimistic concurrency.
5. Authorize before signed URL; audit protected access.
6. Implement retention/purge and client checksum verification.
7. Add complete visibility-matrix tests.

## Mandatory checks

- Public Git history caveat is respected.
- Object IDs are non-enumerable and project-scoped.
- Signed URL TTL short and current authorization checked.
- Permission revocation and manifest mismatch tested.

## Forbidden

- No public bucket by default.
- No bucket credentials or permanent object URL to client.
- No protected bytes in public repository workflow.

## Completion output

Report object/metadata transaction, policies, audit, retention, tests and client hydration requirements.
