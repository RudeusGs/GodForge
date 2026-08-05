# GodForge Documentation

This directory is the authoritative product, architecture, security, operations and implementation specification for GodForge.

## Product direction

GodForge is a production-oriented Git platform specialized for Godot Engine projects. It combines repository hosting and collaboration with deterministic Godot parsing, project-health analysis, dependency visualization, AI-assisted advisory reports and independently controlled asset visibility.

GodForge is **not** a full GitHub clone. Forgejo provides Git transport and repository primitives. GodForge owns the Godot-specific product layer, security boundary, analysis pipeline, asset vault and team workflows.

## Status labels

Every document uses one or more of these labels:

- **Target**: required final design for the graduation project and production release.
- **Current**: supported by the repository snapshot used to prepare this documentation.
- **Planned**: approved scope but not yet implemented.
- **Deferred**: intentionally outside the graduation-project completion gate.

Target requirements must not be described as implemented until `IMPLEMENTATION_STATUS.md` is updated with evidence.

## Reading order

1. `PRODUCT_VISION.md`
2. `SRS/00-overview.md`
3. `SRS/01-scope.md`
4. `SRS/02-architecture.md`
5. Relevant file under `SRS/03-functional/`
6. `SRS/04-database.md`
7. `SRS/05-api.md`
8. `SRS/06-security.md`
9. `SRS/07-non-functional.md`
10. `SRS/08-workflows.md`
11. `SRS/11-testing-acceptance.md`
12. `SRS/12-worker-processing.md`
13. `SRS/13-deployment-operations.md`
14. `SRS/16-research-evaluation.md`

## Governance documents

| Document | Purpose |
|---|---|
| `DEFINITION_OF_READY.md` | Minimum evidence required before implementation starts. |
| `DEFINITION_OF_DONE.md` | Completion gate for a feature or milestone. |
| `MILESTONES.md` | Ordered implementation plan and exit gates. |
| `IMPLEMENTATION_STATUS.md` | Current evidence-based implementation state. |
| `RBAC_MATRIX.md` | Permission model for platform, organization, project and asset scopes. |
| `ERROR_CODES.md` | Stable public error catalog. |
| `QUALITY_GATES.md` | Build, test, security, documentation and release gates. |
| `THREAT_MODEL.md` | Security threats, controls and residual risks. |
| `DATA_CLASSIFICATION.md` | Data sensitivity and handling rules. |
| `OPERATIONS_RUNBOOK.md` | Production operations and incident procedures. |

## Architecture decisions

See `ADR/README.md`. No foundational architectural change may be implemented without an accepted ADR.

## Agent instructions

The repository uses `.agents/AGENTS.md` and `.agents/skills/*/SKILL.md`. The actual folder name is `.agents`, not `.agent`.
