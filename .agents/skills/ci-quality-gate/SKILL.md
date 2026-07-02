---
name: ci-quality-gate
description: Skill for running CI quality gates locally before reporting completion.
---

# CI Quality Gate

## When to Use
Use this skill after implementing a feature, just before declaring the task completed, to ensure it won't break the build.

## Required Reading
- `docs/QUALITY_GATES.md`

## Workflow
1. Run backend build: `dotnet build`.
2. Run backend tests: `dotnet test`.
3. Run frontend lint: `npm run lint`.
4. Run frontend typecheck: `npm run typecheck`.
5. Run frontend tests: `npm run test:unit`.

## Mandatory Checks
- Zero warnings on `dotnet build`.
- Zero type errors on frontend.
- All tests pass.

## Forbidden Actions
- Do not ignore warnings or bypass tests.
- Do not use this skill to write code; only use it for verification.

## Completion Checklist
- [ ] `dotnet build` passed cleanly.
- [ ] `dotnet test` passed.
- [ ] Frontend lint & typecheck passed.
