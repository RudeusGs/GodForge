# ADR 0009: Incremental analysis with full-analysis fallback

## Status
Accepted

## Context
Full parsing after every commit is wasteful for large projects, but incomplete affected-file calculation can produce wrong results.

## Decision
The first eligible revision receives full analysis. Later revisions may use Git changed files plus forward/reverse dependency impact. Unsupported parser changes, missing baseline, excessive impact, configuration changes or uncertainty force full analysis. Equivalence is verified on a corpus.

## Consequences
### Positive
- Reduced analysis time and compute cost.

### Negative
- More complex invalidation and correctness testing.

## Constraints enforced on implementation and AI agents
- Correctness takes priority over speed.
- Incremental output must use the same normalized model as full analysis.
- The reason for fallback must be recorded.
