# 14. Data Retention and Deletion

## Principles

- Retain only what is required for product behavior, security, audit, thesis evidence and contractual policy.
- Deletion of a project is a lifecycle, not an immediate uncontrolled cascade.
- Legal hold or active security investigation can suspend purge with explicit authorization.

## Default target policies

| Data | Default target |
|---|---|
| Active project/revision metadata | Retained while project active; historical revision limits configurable. |
| Temporary workspaces | Deleted after job; failed cleanup retried promptly. |
| Raw AI context | Not retained by default; if retained, short configurable period and Restricted handling. |
| AI advisory metadata | Retained with analysis history. |
| Generated reports/previews | Configurable, e.g. 30-90 days unless pinned. |
| Job events/attempts | Operational retention, e.g. 90 days; terminal summary longer as needed. |
| Webhook payloads | Store hash/minimal safe fields; short payload retention if needed for debugging. |
| Audit/security events | Longer retention, e.g. 1-2 years subject to policy. |
| Deleted protected assets | Soft-delete grace period, then purge unless referenced/legal hold. |
| Refresh sessions/tokens | Purge after expiry/revocation plus short security window. |

Exact production values are configuration and policy decisions; documentation must not claim compliance with a regulation without review.

## Project deletion flow

1. Mark deleting/archived and block new mutations.
2. Cancel or drain project jobs.
3. Revoke Forgejo/member and signed-asset access.
4. Schedule metadata, artifact, asset and hosted-repository purge according to grace period.
5. Preserve required audit tombstones and legal holds.
6. Verify object deletion and record retention run result.

## User deletion

Personal data is removed/anonymized where allowed while historical technical attribution may be replaced by a tombstone identifier when needed for repository/audit integrity.
