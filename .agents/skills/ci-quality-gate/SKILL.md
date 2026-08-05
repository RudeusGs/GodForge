---
name: ci-quality-gate
description: Run the required local quality gates before completion.
---

# Ci Quality Gate

## Use when

Run the required local quality gates before completion.

## Required reading

- `docs/QUALITY_GATES.md`
- `docs/DEFINITION_OF_DONE.md`

## Workflow

1. Run backend restore/format/build/tests.
2. Run frontend install/lint/type-check/tests/build.
3. Run migrations/integration/security tests relevant to change.
4. Run documentation link check.
5. Record exact commands and failures.
6. Do not declare completion if a mandatory gate fails.

## Mandatory checks

- Clean checkout reproducibility.
- No skipped mandatory test without reason.
- No generated/secrets accidentally added.
- Artifact/package versions recorded.

## Forbidden

- Do not alter tests merely to make gates green without preserving requirements.
- Do not report unrun checks as passed.

## Completion output

Provide command-by-command result, environment limits and blockers.
