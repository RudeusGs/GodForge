---
name: performance-review
description: Review database, API, worker and frontend performance.
---

# Performance Review

## Use when

Review database, API, worker and frontend performance.

## Required reading

- `docs/SRS/07-non-functional.md`
- `docs/PERFORMANCE_TEST_PLAN.md`
- Relevant data/API/worker docs

## Workflow

1. Define workload, dataset tier and target.
2. Measure baseline with environment and commit recorded.
3. Inspect query count/plans, allocations, I/O, queue lag and render size.
4. Fix highest-impact bottleneck without weakening correctness/security.
5. Re-measure and compare.
6. Add regression test/metric where practical.

## Mandatory checks

- N+1 and unbounded queries.
- Pagination and graph/tree limits.
- Cache correctness/invalidation.
- Incremental/full equivalence.
- Resource quotas and concurrency.

## Forbidden

- Do not optimize from intuition alone.
- Do not cache authorization unsafely.
- Do not claim performance without reproducible measurement.

## Completion output

Report dataset/environment, before/after metrics, changes, tradeoffs and regression protection.
