---
name: data-retention
description: Implement retention, deletion, purge or legal-hold behavior.
---

# Data Retention

## Use when

Implement retention, deletion, purge or legal-hold behavior.

## Required reading

- `docs/SRS/14-data-retention.md`
- `docs/DATA_CLASSIFICATION.md`
- ADR 0002, 0008, 0012

## Workflow

1. Identify data class, owner, references and retention rule.
2. Define archive/soft-delete/grace/purge states.
3. Cancel/block related jobs and revoke access.
4. Delete PostgreSQL/MinIO/Forgejo data in auditable stages.
5. Respect legal hold and retry partial failures.
6. Verify object deletion and preserve required tombstones/audit.
7. Add dry-run/metrics/tests.

## Mandatory checks

- Cross-system consistency and reconciliation.
- Backup retention caveat.
- Idempotent purge.
- No orphaned private object or active signed access.

## Forbidden

- Do not hard-delete immediately without lifecycle.
- Do not purge legal-hold data.
- Do not leave database claiming object deleted without verification state.

## Completion output

Report scope, policy, stages, legal-hold behavior, verification and tests.
