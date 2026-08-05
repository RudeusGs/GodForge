# Asset Vault and Independent Visibility

## Purpose

Allow protected binary assets to be versioned and permissioned independently from repository visibility.

## Actors

Asset owner/uploader, ProjectOwner/Maintainer/Developer, authorized consumers.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-23.1 | Upload, hash, validate and version asset objects | Must |
| FR-23.2 | Visibility policies: public, project, organization, selected, owner | Must |
| FR-23.3 | Repository manifest mapping logical paths to asset versions | Must |
| FR-23.4 | Authorized short-lived signed download and audit | Must |
| FR-23.5 | Hydration through CLI or Godot plugin | Should |
| FR-23.6 | License, preview, quarantine and retention metadata | Should |

## Main flow

1. User uploads an asset through authorized API.
2. Service validates size, type, hash and optional malware scan.
3. Bytes are stored privately in MinIO; metadata/version and policy are committed.
4. Manifest entry points Godot logical path to asset ID/version/checksum.
5. Authorized client requests hydration/download and receives short-lived signed URL.
6. Client verifies checksum before writing the file.

## Error and edge cases

- Hash mismatch, type mismatch, malware/quarantine.
- Visibility conflict or missing selected member.
- Manifest references deleted/unavailable version.
- Signed URL expired or grant revoked.
- Public repository previously committed the private bytes: secrecy cannot be restored.

## Authorization and security

- Bucket is not public except explicitly public object path policy.
- Authorization occurs before URL issuance.
- Protected object identifiers are non-guessable and project-scoped.
- Downloads and policy changes are audited.
- Deletion obeys retention/legal hold.

## Async processing and idempotency

- Upload validation, preview, malware scan and purge may use durable jobs.

## Acceptance criteria

- `AC-FR-23-01`: Public repository can be cloned without protected bytes.
- `AC-FR-23-02`: Authorized hydration reconstructs expected paths/checksums.
- `AC-FR-23-03`: Unauthorized user cannot list or download protected asset.
- `AC-FR-23-04`: Revocation blocks new signed URLs immediately.

## Related API

- `/api/v1/projects/{projectId}/assets`, versions, grants, manifest and download endpoints

## Related data

- `storage.asset_objects`, `storage.asset_versions`, `storage.asset_permissions`, `storage.asset_manifests`, `storage.asset_download_audits`, `storage.asset_licenses`

## Tests and observability

- Test suite: `TC-VAULT-*`, including visibility matrix, checksum, quarantine, signed URL and revocation.
- Metrics: upload/download bytes, validation failures, quarantine count, signed-URL issuance and storage growth.
- Audit events cover protected download and policy change.
