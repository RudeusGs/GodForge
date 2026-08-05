# GodForge Agent Entry Point

This file is the root entry point for every coding agent, reviewer and automation tool.

## Mandatory instruction source

Read and follow, in order:

1. `.agents/AGENTS.md` - authoritative engineering rules.
2. `docs/DEFINITION_OF_READY.md` - implementation gate.
3. `docs/PRODUCT_VISION.md` and `docs/SRS/01-scope.md`.
4. `docs/SRS/02-architecture.md`.
5. The relevant `docs/SRS/03-functional/*.md` requirement file.
6. `docs/SRS/04-database.md` and, for M1, `docs/SRS/04-database-m1-physical.md`.
7. `docs/SRS/05-api.md` and the relevant file under `docs/SRS/05-api-contracts/`.
8. `docs/RBAC_MATRIX.md`, `docs/SRS/06-security.md` and `docs/THREAT_MODEL.md`.
9. `docs/SRS/10-traceability.md` and `docs/SRS/11-testing-acceptance.md`.
10. The most specific `.agents/skills/*/SKILL.md` for the task.

If documents conflict, stop and resolve the documentation. Do not silently invent behavior.

## Current implementation order

- Finish M0 foundation work first, including migration of backend projects to `.NET 10 LTS`.
- Implement M1 identity and tenancy only from documented `FR-*`, `AC-*` and M1 API contracts.
- Do not start hosted Git, worker pipeline, parser, AI or Asset Vault code before their milestone dependencies are complete.

## M1 authorization invariants

- Every project belongs to exactly one organization.
- Every active project member must also be an active member of the same organization.
- Removing or suspending an organization membership revokes all project memberships in that organization in the same business transaction and emits reconciliation events.
- OrganizationOwner and OrganizationAdmin may administer organization/project metadata but do not automatically receive repository, source, asset or analysis access unless they also hold a project role.
- Effective permission is the intersection of platform minimums, organization policy, project role and resource-specific policy.
- Tenant ownership is verified server-side. Client-supplied organization or project IDs are never authorization evidence.

## Runtime and migration rule

- Target runtime: `.NET 10 LTS`.
- Do not claim the migration is complete until all projects target `net10.0`, restore/build/test pass and runtime images are updated.
- Production database migrations run as a controlled release step. Do not add automatic multi-instance startup migration.

## Completion rule

A task is not complete until code, tests, API/data/security documentation, traceability and `docs/IMPLEMENTATION_STATUS.md` are synchronized with verified behavior.
