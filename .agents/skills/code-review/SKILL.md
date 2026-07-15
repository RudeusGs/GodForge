---
name: code-review
description: Skill for reviewing GodForge code for adherence to strict architecture and standards.
---

# Code Review

## When to Use
Use this skill when auditing existing code, or when the user asks you to review a PR or specific module.

## Required Reading
- `docs/SRS/02-architecture.md`
- `docs/QUALITY_GATES.md`
- `.agents/AGENTS.md`

## Workflow
1. Check architectural boundaries (e.g., Domain must not reference Infrastructure).
2. Check error handling (no raw exceptions, use `SCREAMING_SNAKE_CASE` codes).
3. Check RBAC enforcement on API endpoints.
4. Check Git safety (distributed locks, sanitized input).

## Mandatory Checks
- Clean Architecture dependency violations.
- Controller business logic (controllers must orchestrate, not decide).
- Application/Domain infrastructure leakage.
- Missing RBAC.
- Missing tests.
- Missing docs sync.
- Secrets/logging issues.
- Async job misuse (e.g., synchronous long operations in HTTP requests).
- Migration risks (e.g., editing applied migrations).
- Frontend permission-only security mistakes.

## Forbidden Actions
- Do not rubber-stamp code without checking documentation alignment.

## Completion Checklist


## Output Expectations
Provide a detailed review listing issues categorized by the mandatory checks above.
