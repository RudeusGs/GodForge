# Repository and Git Management

## Purpose

Provide linked and hosted repository workflows while delegating Git protocol behavior to external providers/Forgejo.

## Actors

ProjectOwner, Maintainer, Developer, Reviewer, Viewer; Forgejo/external provider.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-04 | Connect or create one primary repository per project | Must |
| FR-05 | Synchronize refs and immutable revisions | Must |
| FR-06 | Browse commits, tree and bounded text blobs | Must |
| FR-07 | Branch listing and protection policy | Must |
| FR-21 | Forgejo-hosted repository provisioning and permission sync | Must |

## Main flow

Linked mode:
1. Authorized user submits HTTPS remote and optional protected credential reference.
2. API validates URL policy and creates a sync job.
3. Worker fetches refs and stores sanitized repository metadata.

Hosted mode:
1. Owner/Maintainer requests repository creation.
2. GodForge provisions Forgejo repository and permissions.
3. Users clone/push through Forgejo.
4. Signed webhook creates an idempotent revision pipeline.

## Error and edge cases

- Unsupported/private-network remote.
- Invalid credential, repository not found or quota exceeded.
- Provider outage or command timeout.
- Non-fast-forward/protected-branch rejection is a Git-engine concern surfaced safely.
- Duplicate webhook or stale ref.

## Authorization and security

- Do not implement custom Git protocol.
- Credentials remain server-side and encrypted/vaulted.
- Webhooks require signature, replay protection and repository identity match.
- Tree/blob paths are normalized; blobs are text-only and size-limited.
- Forgejo permission drift is reconciled.

## Async processing and idempotency

- Clone/fetch/sync/provision/reconciliation operations are durable jobs where provider latency or retries are possible.

## Acceptance criteria

- `AC-FR-21-01`: Hosted repository can be created, cloned and pushed by an authorized Developer.
- `AC-FR-21-02`: Removed member loses repository access after reconciliation.
- `AC-FR-21-03`: Duplicate webhook does not duplicate revision or analysis.
- `AC-FR-07-01`: Blob endpoint cannot read outside the selected revision tree.

## Related API

- `/api/v1/projects/{projectId}/repository/*`, `/api/v1/webhooks/forgejo`

## Related data

- `repo.repositories`, `repo.repository_credentials`, `repo.git_refs`, `repo.git_commits`, `repo.repository_snapshots`, `repo.repository_files`, `repo.webhook_events`, `ops.outbox_messages`

## Tests and observability

- Test suites: `TC-REPO-*` and `TC-WEBHOOK-*`, including SSRF, provider failure, permission reconciliation and duplicate webhook.
- Metrics: provision/sync latency, provider error, reconciliation drift, webhook validation and repository quota rejection.
- Logs never contain credentials, raw remote URLs with secrets or provider admin tokens.
