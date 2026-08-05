# ADR 0005: Use Forgejo as the hosted Git engine

## Status
Accepted

## Context
Implementing Git Smart HTTP, SSH, pack negotiation, refs, hooks and object storage is high-risk and not the Godot-specific contribution.

## Decision
Use Forgejo for hosted repository creation, clone, push, pull, branches, commits and Git authentication. GodForge provisions repositories, synchronizes permissions, validates signed webhooks and owns analysis/business workflows. External linked repositories remain supported through HTTPS adapters.

## Consequences
### Positive
- Standards-compliant Git behavior and reduced security surface.

### Negative
- Additional service, database, backups and provider reconciliation.

## Constraints enforced on implementation and AI agents
- Do not implement a custom Git protocol server.
- Do not expose Forgejo admin tokens to clients.
- Hosted Git is not production-ready until permission sync and webhook verification pass.
