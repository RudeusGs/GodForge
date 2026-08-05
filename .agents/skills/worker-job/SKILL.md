---
name: worker-job
description: Implement or modify a durable background job/consumer.
---

# Worker Job

## Use when

Implement or modify a durable background job/consumer.

## Required reading

- `docs/SRS/12-worker-processing.md`
- `docs/SRS/15-observability.md`
- ADR 0003, 0007, 0010, 0012
- Relevant functional SRS

## Workflow

1. Define job type, producer, queue, message schema and output identity.
2. Create job plus outbox atomically.
3. Implement consumer validation, inbox dedupe and state revalidation.
4. Add lock when repository workspace/provider mutation requires it.
5. Implement progress, heartbeat, timeout, cancellation and cleanup.
6. Classify retry/non-retry/DLQ errors.
7. Commit output before completion event and add tests/metrics.

## Mandatory checks

- Message has required identifiers and no secrets/large payload.
- Duplicate delivery is harmless.
- Owner-token lock and TTL renewal.
- Terminal paths release resources.
- Stale project/repository state cannot publish output.

## Forbidden

- No `Task.Run` background work from API.
- No generic catch-and-retry-all.
- No completion before durable commit.
- No RabbitMQ-only job state.

## Completion output

Report job lifecycle, idempotency key, retry table, locks, metrics and tests.
