# ADR 0010: Analysis versioning and idempotency identity

## Status
Accepted

## Context
A commit may be analyzed repeatedly under changing parser, rules, profiles or prompts. Duplicate jobs must not corrupt or duplicate results.

## Decision
The logical result identity is:

```text
repositoryId + commitSha + parserVersion + ruleSetVersion
+ analysisProfileVersion + promptVersion + inputHash
```

Deterministic and AI outputs use separate identities; `promptVersion` is omitted from deterministic uniqueness. Writes are upserted or committed as immutable versions.

## Consequences
### Positive
- Reproducibility, caching and safe duplicate delivery.

### Negative
- Version lifecycle and storage retention become explicit responsibilities.

## Constraints enforced on implementation and AI agents
- Never overwrite historical output under a different engine version.
- Never use timestamps as the only idempotency key.
