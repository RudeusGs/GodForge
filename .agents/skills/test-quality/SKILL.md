---
name: test-quality
description: Design or review tests for a GodForge change.
---

# Test Quality

## Use when

Design or review tests for a GodForge change.

## Required reading

- `docs/SRS/11-testing-acceptance.md`
- Relevant requirement and threat docs
- `docs/DEFINITION_OF_DONE.md`

## Workflow

1. Map tests to requirement/acceptance IDs.
2. Select unit, integration, e2e, security, performance and reliability levels.
3. Use deterministic fixtures and avoid brittle internal assertions.
4. Include role/tenant negative cases.
5. Include duplicate/retry/failure cases for async work.
6. Run and report exact commands.

## Mandatory checks

- Tests fail for the intended regression.
- No shared-state/order dependency.
- External providers are controlled without hiding contract behavior.
- Data cleanup and fixture hashes documented.

## Forbidden

- Do not replace integration tests with mocks for persistence/authorization behavior.
- Do not assert only HTTP 200.
- Do not ignore flaky tests.

## Completion output

Report coverage by requirement, commands/results, gaps and follow-up tests.
