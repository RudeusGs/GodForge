# GodForge Software Requirements Specification

## Purpose

This SRS defines the target graduation-project and production design. `../IMPLEMENTATION_STATUS.md` separately records what is currently implemented.

## Requirement language

- **Must**: required for the graduation-project release gate.
- **Should**: expected unless a documented risk or schedule decision defers it.
- **Could**: extension after Core completion.
- **Will not**: explicitly excluded.

## Requirement IDs

- `FR-*`: functional requirement.
- `NFR-*`: non-functional requirement.
- `SEC-*`: security control.
- `WF-*`: end-to-end workflow.
- `AC-*`: acceptance criterion.
- `TC-*`: test case.

IDs are stable. Do not renumber existing requirements; mark deprecated and add a new ID. `../REQUIREMENT_REGISTRY.md` records ownership of requirement families and prevents duplicates.

## Functional-document section order

Every file under `03-functional/` uses this order:

1. Purpose.
2. Actors.
3. Requirements.
4. Main flow.
5. Error and edge cases.
6. Authorization and security.
7. Async processing and idempotency.
8. Acceptance criteria with `AC-*` IDs.
9. Related API.
10. Related data.
11. Tests and observability.

Do not place table names under Authorization, test assertions under Related API or routes under Related data.

## Structure

- `00-overview.md`: product and stakeholders.
- `01-scope.md`: release scope and exclusions.
- `02-architecture.md`: boundaries and system design.
- `03-functional/`: module requirements.
- `04-database.md`: target logical data model.
- `04-database-m1-physical.md`: implementation-ready physical data design for M1.
- `05-api.md`: API route catalog and contract index.
- `05-api-contracts/`: implementation-ready API contracts, beginning with M1.
- `06-security.md`: mandatory security controls.
- `07-non-functional.md`: performance, reliability and quality targets.
- `08-workflows.md`: end-to-end flows.
- `09-ui-ux.md`: screens and interaction rules.
- `10-traceability.md`: requirements mapping.
- `11-testing-acceptance.md`: test strategy and release acceptance.
- `12-worker-processing.md`: queue and job semantics.
- `13-deployment-operations.md`: environment and production operations.
- `14-data-retention.md`: retention, deletion and legal hold.
- `15-observability.md`: logs, metrics, traces and alerts.
- `16-research-evaluation.md`: thesis experiments and evidence.

## Change rule

A change to behavior is incomplete until relevant functional, API, database, security, workflow, testing and traceability documents are synchronized. A Must requirement is not implementation-ready without at least one objective acceptance criterion and mapped automated test ID.
