---
name: architecture-decision
description: Skill for proposing and recording architectural decisions.
---

# Architecture Decision

## When to Use
Use this skill when proposing a new technical direction, library choice, or fundamental structural change to GodForge.

## Required Reading
- `docs/ADR/README.md`
- Existing ADRs in `docs/ADR/`

## Workflow
1. Identify the architectural problem or opportunity.
2. Draft a new ADR using the standard format (Status, Context, Decision, Consequences, Constraints).
3. Update `docs/ADR/README.md` to index the new ADR.

## Mandatory Checks
- Must clearly define "Constraints enforced on AI agents".
- Must explain the "Negative" consequences of the decision.

## Forbidden Actions
- Do not implement the architectural change before the ADR is written and approved by the user.

## Completion Checklist
- [ ] ADR drafted with Context and Decision.
- [ ] Positive and Negative consequences listed.
- [ ] AI constraints explicitly stated.
- [ ] README indexed.
