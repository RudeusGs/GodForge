# Performance Test Plan

## Workloads

1. Authentication and project list.
2. Project dashboard and health summary.
3. Branch/commit/tree browsing.
4. Scene and asset pagination.
5. Dependency graph filtering.
6. Full analysis for small, medium and large Godot corpus projects.
7. Incremental analysis after representative changes.
8. Asset upload/download.
9. Concurrent webhook/job bursts.

## Dataset tiers

| Tier | Files | Scenes | Scripts | Assets | Repository size |
|---|---:|---:|---:|---:|---:|
| Small | <= 1,000 | <= 50 | <= 100 | <= 500 | <= 100 MB |
| Medium | <= 10,000 | <= 500 | <= 1,000 | <= 5,000 | <= 1 GB |
| Large thesis tier | <= 20,000 | <= 1,500 | <= 3,000 | <= 12,000 | <= 2 GB subject to environment |

## Measurements

- API p50/p95/p99 latency and error rate.
- Database query count and duration.
- Queue lag and worker throughput.
- Analysis duration by stage.
- CPU, memory, disk and object-storage usage.
- Incremental versus full analysis result equivalence and speedup.
- Gemini latency, tokens and cost estimate.

## Acceptance baseline

Targets are defined in `SRS/07-non-functional.md`. Tests must use production-like settings and record environment, commit SHA, parser/rule versions and dataset hashes.
