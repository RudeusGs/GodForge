---
name: hosted-git
description: Implement Forgejo hosted-repository provisioning, webhook or permission synchronization.
---

# Hosted Git

## Use when

Implement Forgejo hosted-repository provisioning, webhook or permission synchronization.

## Required reading

- `docs/SRS/03-functional/git.md`
- ADR 0005, 0011, 0012
- `docs/THREAT_MODEL.md`
- `docs/RBAC_MATRIX.md`

## Workflow

1. Define GodForge state and Forgejo operation/idempotency.
2. Use server-side service account through adapter.
3. Provision/update through durable job/outbox.
4. Validate signed webhook, replay and repository identity.
5. Synchronize/reconcile permissions after membership changes.
6. Sanitize provider errors and add outage/retry behavior.
7. Test create/clone/push/removal/reconciliation.

## Mandatory checks

- No admin token in client/log/message.
- GodForge role mapping is explicit.
- Duplicate webhook/provision request is safe.
- Removed member access is reconciled and audited.

## Forbidden

- Do not implement Git protocol.
- Do not call Forgejo directly from controller.
- Do not mark permission change complete before provider confirmation/reconciliation state.

## Completion output

Report provider operations, role mapping, signatures, idempotency, tests and production blockers.
