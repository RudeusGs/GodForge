# GodForge SRS

`docs/SRS` is the primary documentation source for the GodForge Software Requirements Specification. The Word-based SRS maintenance flow has been replaced with Markdown in the repository for easier reviewing, versioning, traceability, and updating alongside the source code.

The Word file `SRS_GodForge.docx`, if still present in the repository, is considered only a reference/archive document. Do not update the primary content in the Word file.

## Document Structure

| Path | Content |
| --- | --- |
| `00-overview.md` | Product overview, goals, user roles. |
| `01-scope.md` | Included/excluded scope, boundaries, and MVP limits. |
| `02-architecture.md` | Multi-tier web architecture, async workers, and design principles. |
| `03-functional/` | Functional requirements by module. |
| `04-database.md` | PostgreSQL schema, Redis, MinIO, and data rules. |
| `05-api.md` | REST API `/api/v1`, response/error formats, and endpoint catalog. |
| `06-security.md` | Authentication, RBAC, secret handling, threat model. |
| `07-non-functional.md` | Performance, reliability, scalability, observability, maintainability. |
| `08-workflows.md` | Primary business workflows. |
| `09-ui-ux.md` | Screens, navigation, states, and role-based visibility. |
| `10-traceability.md` | Requirement mapping to APIs, database, UI, and test cases. |
| `11-testing-acceptance.md` | Test strategy, acceptance criteria, DoD, and test case format. |
| `12-worker-processing.md` | Queues, job lifecycle, retries, DLQ, idempotency, and worker metrics. |
| `13-deployment-operations.md` | Local/dev/prod deployment, monitoring, backups, incidents, and DR. |

## Update Rules

- Write in clear, technical, and consistent English.
- Do not add features outside the SRS scope without product decisions.
- When adding or modifying a requirement, you must update:
  - the corresponding module file in `03-functional/`;
  - `10-traceability.md`;
  - `11-testing-acceptance.md` or related test cases.
- Keep requirement IDs stable if the requirement retains the same meaning: `FR-xx`, `NFR-xx`, `BR-xx`, `AC-xx`.
- If API contracts change, update `05-api.md` and traceability.
- If schemas change, update `04-database.md`, traceability, and testing.
- If worker/job behaviors change, update `12-worker-processing.md` and related workflows.
- If information is uncertain, add a brief confirmation note in the relevant file.

## Review Rules

- Every PR changing SRS docs must ensure the product name is `GodForge`.
- Do not use placeholders for content that can be inferred from the SRS; explicitly state any missing decisions in a confirmation section.
- Do not delete valuable content from the original SRS; if content overlaps, merge it more concisely.
