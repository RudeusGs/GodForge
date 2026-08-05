---
name: debug-issue
description: Diagnose and fix a GodForge defect with evidence.
---

# Debug Issue

## Use when

Diagnose and fix a GodForge defect with evidence.

## Required reading

- Relevant SRS/workflow/error code
- Logs/metrics/tests without exposing secrets
- Related ADR and implementation status

## Workflow

1. Reproduce with minimal deterministic case.
2. Trace correlation/job/message/tenant context.
3. Identify root cause and affected invariant.
4. Add failing regression test.
5. Implement smallest safe fix.
6. Run relevant gates and update docs if behavior changes.

## Mandatory checks

- Distinguish symptom, trigger and root cause.
- Inspect retry/duplicate/stale state for async issues.
- Confirm no cross-tenant or data-loss impact.
- Verify cleanup and migration state.

## Forbidden

- Do not hide error with catch/ignore.
- Do not delete production data/workspaces as first response.
- Do not log extra secrets to debug.

## Completion output

Report reproduction, root cause, fix, regression test, commands and residual risk.
