# Database Model — Blueprint

PostgreSQL stores durable state. Schemas remain grouped by concern: `core`, `identity`, `repo`, `metadata`, `analysis`, `ops`, `audit`, `storage`.

## Required repository fields

`repo.repositories`: project ID, mode, provider, sanitized clone URL, encrypted credential reference, external/hosted provider ID, default branch, status, current commit SHA, workspace key, size, auto-analysis flag, timestamps.

## Revision and metadata

`git_refs`, `git_commits`, `git_commit_files`, `repository_snapshots`, `repository_files`, `file_versions`, `metadata_runs`, scenes/nodes/resources/scripts/assets/dependencies and parser diagnostics.

## Analysis

Deterministic output uses health report/issue/rule tables. AI output uses `analysis.ai_analysis_runs` and `analysis.ai_findings` with provider, model, prompt version, input hash, status, usage and evidence JSON.

## Jobs

`ops.jobs` is authoritative and includes status, progress, queue, attempts, idempotency key, correlation ID, safe error code and cancellation intent. Inbox/outbox/DLQ tables are used as reliability is expanded.

## Migration rule

A fresh development environment must be reproducible from an initial baseline migration. Existing production data requires a reviewed forward migration; never delete production migrations or recreate the database without backup.
