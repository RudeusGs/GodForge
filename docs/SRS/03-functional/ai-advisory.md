# Gemini AI Advisory

## Purpose

Generate optional explanations and recommendations from bounded, redacted structured context.

## Actors

Authorized project member, Worker, Organization policy owner.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-22.1 | Organization/project opt-in AI advisory | Must |
| FR-22.2 | Bounded context selection and secret redaction | Must |
| FR-22.3 | Schema-validated output with evidence references | Must |
| FR-22.4 | Provider/model/prompt/input usage audit | Must |
| FR-22.5 | Degraded mode when provider fails | Must |

## Main flow

1. Authorized trigger selects revision/profile.
2. Context builder uses deterministic metadata/findings and approved source excerpts.
3. Redactor removes secrets and excluded paths.
4. Worker calls Gemini with fixed system policy and JSON schema.
5. Output is validated, stored as advisory and displayed with AI label.

## Error and edge cases

- Provider disabled, quota exceeded, timeout or unavailable.
- Empty/invalid/non-schema response.
- Context exceeds limit.
- Redaction uncertainty or prohibited organization policy.

## Authorization and security

- Repository text is untrusted data and cannot override system instructions.
- No credentials, `.env`, private keys or forbidden paths are sent.
- AI has no tool permission to mutate code, Git, jobs, users or assets.
- Provider calls are auditable without storing sensitive prompt data unnecessarily.

## Async processing and idempotency

- Runs as optional stage after deterministic report is committed or ready.

## Acceptance criteria

- `AC-FR-22-01`: Deterministic report completes when AI fails.
- `AC-FR-22-02`: Invalid output never appears as a valid report.
- `AC-FR-22-03`: Evidence references resolve to supplied context.
- `AC-FR-22-04`: Token and latency budgets are enforced.

## Related API

- AI trigger/status/report endpoints under revision analysis

## Related data

- `analysis.ai_analysis_runs`, `analysis.ai_findings`, `storage.artifacts` where large context/report is retained

## Tests and observability

- Test suite: `TC-AI-*`, including redaction, prompt injection, schema failure, quota and degraded-mode cases.
- Metrics: provider latency, token usage, context size, redaction blocks and invalid-response count.
- Logs/traces store safe run IDs and versions, never raw secrets or unrestricted prompts.
