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
- Ensure CQRS is strictly separated (Commands mutate, Queries return DTOs).
- Verify async jobs do not run synchronously in API requests.

## Forbidden Actions
- Do not rubber-stamp code without checking documentation alignment.

## Completion Checklist
- [ ] Boundaries checked.
- [ ] Error codes verified.
- [ ] RBAC audited.
- [ ] Git locks verified.
