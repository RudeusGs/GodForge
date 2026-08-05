---
name: production-release
description: Prepare and validate a production/staging release.
---

# Production Release

## Use when

Prepare and validate a production/staging release.

## Required reading

- `docs/RELEASE_CHECKLIST.md`
- `docs/SRS/13-deployment-operations.md`
- `docs/OPERATIONS_RUNBOOK.md`
- `docs/BACKUP_RESTORE_RUNBOOK.md`

## Workflow

1. Verify immutable artifacts and change scope.
2. Run complete quality/security/performance gates.
3. Back up and test migration against production-like data.
4. Validate secrets, TLS, providers, buckets, queues and observability.
5. Deploy in controlled order.
6. Run critical smoke flow and monitor.
7. Record release, rollback/forward-fix and owners.

## Mandatory checks

- No default secrets.
- Outbox/DLQ/backup alerts active.
- Forgejo permission/webhook and Asset Vault authorization verified.
- Restore evidence current.

## Forbidden

- Do not run uncontrolled schema migration from multiple instances.
- Do not release with Critical/High security blocker.
- Do not call production-ready without operational evidence.

## Completion output

Provide release version, gates, migration/backup evidence, smoke results and residual risks.
