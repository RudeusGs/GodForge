---
name: security-review
description: Skill for performing security audits on GodForge features.
---

# Security Review

## When to Use
Use this skill to audit features related to authentication, RBAC, secret storage, Git execution, or input validation.

## Required Reading
- `docs/SRS/06-security.md`
- `docs/RBAC_MATRIX.md`
- `docs/ADR/0004-git-workspace-safety.md`

## Workflow
1. Verify endpoint RBAC attributes.
2. Check credential handling across logging and API responses.
3. Ensure Git commands use sanitized inputs and distributed locks.
4. Validate SQL queries against injection.

## Mandatory Checks
- Credentials must be encrypted at rest (e.g., AES-256-GCM).
- JWT access token lifetime must be 15 minutes.
- Parameterized SQL or EF Core must be used exclusively.

## Forbidden Actions
- Do not return or log Git PATs, passwords, or tokens.
- Do not allow path traversal vulnerabilities in artifact retrieval.
- Do not allow raw shell execution for Git commands.

## Completion Checklist


## Output Expectations

