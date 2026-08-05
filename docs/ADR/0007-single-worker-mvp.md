# ADR 0007: Single deployable worker host with logical workers

## Status
Accepted

## Context
A graduation project needs operational simplicity while preserving a path to independent scaling.

## Decision
Deploy one `GodForge.Worker` host initially, but isolate consumers and handlers by queue/job type. Contracts must allow later extraction into separate processes without changing Domain/Application behavior.

## Consequences
### Positive
- Simpler local and initial production deployment.

### Negative
- One process can suffer noisy-neighbor effects and requires careful concurrency limits.

## Constraints enforced on implementation and AI agents
- No giant universal consumer.
- Each logical worker owns explicit queue, timeout, retry and metrics configuration.
