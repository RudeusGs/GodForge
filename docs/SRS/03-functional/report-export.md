# Report Export

## Purpose

Generate auditable project-health and revision-comparison reports for thesis, review and release decisions.

## Actors

Reviewer and higher roles with export permission.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-25.1 | Export PDF, JSON and CSV reports | Must |
| FR-25.2 | Report by revision and comparison pair | Must |
| FR-25.3 | Include version/evidence/provenance metadata | Must |
| FR-25.4 | Short-lived authorized download and retention | Must |

## Main flow

1. User selects report type, revision(s) and sections.
2. API creates idempotent export job.
3. Worker renders artifact, computes checksum and stores in MinIO.
4. Metadata is committed and authorized signed download is issued.

## Error and edge cases

- Required analysis absent.
- Report too large or renderer failure.
- Artifact expired/purged.
- Permission revoked after generation.

## Authorization and security

- Reports do not embed protected asset bytes unless explicitly authorized.
- AI sections are clearly labeled and separated from deterministic results.
- Download authorization is evaluated at request time.

## Async processing and idempotency

- Always asynchronous for PDF/large exports.

## Acceptance criteria

- `AC-FR-25-01`: Report includes commit SHA, parser/rule/profile/prompt versions and generation timestamp.
- `AC-FR-25-02`: Same input produces logically equivalent structured report.
- `AC-FR-25-03`: Unauthorized user cannot retrieve artifact by ID.

## Related API

- Report create/status/list/download endpoints

## Related data

- `storage.report_exports`, `storage.artifacts`, analysis and audit tables

## Tests and observability

- Test suite: `TC-REPORT-*`, including provenance, authorization, renderer failure and artifact expiry.
- Metrics: generation duration, artifact size, failure reason and signed-download issuance.
- Output fixtures verify AI/deterministic section separation.
