---
name: ai-advisory
description: Implement Gemini context building, request, validation or advisory display.
---

# Ai Advisory

## Use when

Implement Gemini context building, request, validation or advisory display.

## Required reading

- `docs/SRS/03-functional/ai-advisory.md`
- ADR 0006
- `docs/THREAT_MODEL.md`
- `docs/DATA_CLASSIFICATION.md`

## Workflow

1. Start from completed deterministic analysis.
2. Select bounded approved metadata/excerpts.
3. Scan/redact secrets and excluded paths.
4. Build fixed versioned prompt and schema.
5. Call provider with timeout/quota/cancellation.
6. Validate output and evidence references.
7. Store provider/model/prompt/input/usage provenance.
8. Return degraded state on failure.

## Mandatory checks

- Organization opt-in.
- Prompt-injection corpus.
- No authoritative score mutation.
- Token/latency/cost metrics.
- Invalid JSON never shown as valid report.

## Forbidden

- No whole-repository uncontrolled prompt.
- No secrets/raw credentials.
- No AI tool authority or automatic code push.
- No claim of correctness without evidence.

## Completion output

Report context policy, redaction, schema, model/version, degraded behavior and tests.
