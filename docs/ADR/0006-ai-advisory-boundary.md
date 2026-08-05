# ADR 0006: Deterministic analysis is authoritative; AI is advisory

## Status
Accepted

## Context
Generative models may hallucinate, vary across runs, exceed token budgets and receive prompt injection from repository content.

## Decision
Parser and versioned rule engine produce authoritative metadata, findings and health score. Gemini receives bounded, redacted structured context and returns schema-validated recommendations with evidence references. AI failure produces degraded status, not analysis failure.

## Consequences
### Positive
- Reproducible core results and safer provider use.

### Negative
- Requires both deterministic engines and AI integration.

## Constraints enforced on implementation and AI agents
- AI must never invent repository facts not present in supplied context.
- AI cannot directly alter health score, code, permissions or repository state.
- Prompt/model/input versions and token usage must be recorded.
