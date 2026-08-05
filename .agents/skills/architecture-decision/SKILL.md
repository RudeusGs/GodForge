---
name: architecture-decision
description: Propose, review or record a foundational architecture decision.
---

# Architecture Decision

## Use when

Propose, review or record a foundational architecture decision.

## Required reading

- `docs/ADR/README.md`
- Existing ADRs
- `docs/SRS/02-architecture.md`
- `docs/THREAT_MODEL.md`

## Workflow

1. Confirm the decision is architectural, not a local implementation detail.
2. Describe context and alternatives.
3. Record decision, positive and negative consequences.
4. State constraints for implementation and AI agents.
5. Update ADR index and affected SRS/security/operations docs.
6. Obtain user approval before implementation.

## Mandatory checks

- Alternative and rejection rationale included.
- Migration/compatibility and security impact included.
- ADR status and supersession are correct.

## Forbidden

- Do not implement before accepted.
- Do not edit an accepted decision silently; supersede when meaning changes.

## Completion output

Provide ADR path, decision summary, consequences and affected docs/code areas.
