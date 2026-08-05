# Architecture Decision Records

ADRs capture decisions that constrain implementation. Accepted ADRs are mandatory until superseded by a later ADR.

## Status values

- Proposed
- Accepted
- Superseded
- Rejected
- Deprecated

## Index

| ID | Decision | Status |
|---|---|---|
| 0001 | Clean Architecture boundaries | Accepted |
| 0002 | PostgreSQL as durable business-state source of truth | Accepted |
| 0003 | Durable asynchronous job processing | Accepted |
| 0004 | Secure isolated Git workspaces | Accepted |
| 0005 | Forgejo as hosted Git engine | Accepted |
| 0006 | Deterministic analysis authoritative; AI advisory | Accepted |
| 0007 | Single deployable worker host with logical workers | Accepted |
| 0008 | Asset Vault and independent asset visibility | Accepted |
| 0009 | Incremental analysis with full-analysis fallback | Accepted |
| 0010 | Analysis versioning and idempotency identity | Accepted |
| 0011 | Multi-tenant organization/project security boundary | Accepted |
| 0012 | Outbox and inbox for production message reliability | Accepted |
| 0013 | No execution of untrusted Godot projects | Accepted |

## ADR template

```markdown
# ADR NNNN: Title

## Status
Proposed | Accepted | Superseded by ADR ...

## Context

## Decision

## Consequences
### Positive
### Negative

## Constraints enforced on implementation and AI agents

## Validation
```
