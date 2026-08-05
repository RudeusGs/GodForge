---
name: code-review
description: Review code changes for correctness, architecture, security and maintainability.
---

# Code Review

## Use when

Review code changes for correctness, architecture, security and maintainability.

## Required reading

- `.agents/AGENTS.md`
- Relevant SRS/ADR
- `docs/DEFINITION_OF_DONE.md`

## Workflow

1. Understand requirement and diff scope.
2. Review behavior and failure paths before style.
3. Check architecture boundaries, tenant authorization and data integrity.
4. Check async/idempotency/resource cleanup.
5. Check tests, observability and docs.
6. Report actionable findings with severity and file/line evidence.

## Mandatory checks

- Focus on defects and risks, not cosmetic churn.
- Verify current code rather than trusting comments.
- Distinguish blocker, high, medium and suggestion.

## Forbidden

- Do not approve based only on build success.
- Do not request broad unrelated refactor.
- Do not expose sensitive source/details outside review context.

## Completion output

Provide prioritized findings, questions, test gaps and approval/block recommendation.
