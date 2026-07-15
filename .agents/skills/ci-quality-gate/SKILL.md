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
1. Run backend build: `cd GodForge-BE && dotnet restore && dotnet build --no-restore`.
2. Run backend tests: `cd GodForge-BE && dotnet test --no-build`.
3. Run backend format verification: `cd GodForge-BE && dotnet format --verify-no-changes`.
4. Run backend package vulnerability check: `cd GodForge-BE && dotnet list package --vulnerable`.
5. Run frontend lint: `cd GodForge-FE && npm ci && npm run lint`.
6. Run frontend typecheck: `cd GodForge-FE && npm run typecheck`.
7. Run frontend tests: `cd GodForge-FE && npm run test:unit`.
8. Run frontend build: `cd GodForge-FE && npm run build`.
9. Run frontend audit: `cd GodForge-FE && npm audit --audit-level=critical`.

## Mandatory Checks
- Zero warnings on `dotnet build`.
- Zero type errors on frontend.
- All tests pass.
- No package vulnerabilities in backend or critical audit failures in frontend.
- If a command cannot run because the skeleton is not created yet, you must report it as "not runnable yet" and specify the milestone that will make it runnable.

## Forbidden Actions
- Do not ignore warnings or bypass tests.
- Do not use this skill to write code; only use it for verification.
- Do not claim the gate passed if a command could not be run.

## Completion Checklist


## Output Expectations
The agent must list the exact commands executed and their pass/fail/not-runnable-yet status.
