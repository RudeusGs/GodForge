---
name: report-export
description: Implement or modify PDF/JSON/CSV technical report export.
---

# Report Export

## Use when

Implement or modify PDF/JSON/CSV technical report export.

## Required reading

- `docs/SRS/03-functional/report-export.md`
- `docs/SRS/14-data-retention.md`
- `docs/DATA_CLASSIFICATION.md`

## Workflow

1. Define report identity, revisions, sections and format.
2. Create durable idempotent export job.
3. Separate deterministic and AI sections with provenance.
4. Render bounded artifact and checksum.
5. Store privately in MinIO and authorize signed download.
6. Apply retention and audit.
7. Test authorization, expiry and large/failure paths.

## Mandatory checks

- Includes commit and engine versions.
- No protected asset bytes unless authorized.
- Current authorization checked at download.
- Artifact status committed before completion.

## Forbidden

- Do not generate large report synchronously.
- Do not return direct permanent object URL.
- Do not present AI text as deterministic fact.

## Completion output

Report formats, provenance, job/artifact identity, retention and tests.
