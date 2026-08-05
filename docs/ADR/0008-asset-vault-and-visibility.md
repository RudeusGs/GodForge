# ADR 0008: Asset Vault and independent asset visibility

## Status
Accepted

## Context
Git repository visibility applies to all committed objects. A public repository cannot safely hide selected previously committed assets.

## Decision
Protected assets are stored in MinIO through an Asset Vault service. Git stores a versioned manifest containing logical path, asset ID, version, checksum and required visibility metadata. Access policies are `public`, `project-members`, `organization`, `selected-members` and `owner-only`. Downloads use short-lived signed URLs after authorization.

## Consequences
### Positive
- Public source can coexist with private commercial assets.
- Asset versions, licenses and downloads are auditable.

### Negative
- Clone alone may not recreate the project; a plugin or CLI must hydrate assets.
- Object storage and manifest consistency require reconciliation.

## Constraints enforced on implementation and AI agents
- Never claim a Git-committed asset can remain secret after repository publication.
- Never return direct bucket credentials.
- Every protected download must be authorized and audited.
