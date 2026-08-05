---
name: security-review
description: Review a change against GodForge threat model and security requirements.
---

# Security Review

## Use when

Review a change against GodForge threat model and security requirements.

## Required reading

- `docs/SRS/06-security.md`
- `docs/THREAT_MODEL.md`
- `docs/SECURITY_TEST_PLAN.md`
- `docs/DATA_CLASSIFICATION.md`

## Workflow

1. Identify trust boundaries and data classes changed.
2. Map relevant threats and abuse cases.
3. Review authentication, authorization, input, output, secrets and logging.
4. Review provider/workspace/message/object behavior.
5. Add or update security tests.
6. Record residual risk and required blockers.

## Mandatory checks

- Cross-tenant checks.
- SSRF/path/symlink/untrusted execution where relevant.
- Prompt injection/redaction for AI.
- Signed URL/upload checks for assets.
- No Critical/High unresolved at release.

## Forbidden

- Do not say “secure” without controls/tests.
- Do not suppress a risk because feature is a thesis project.
- Do not expose exploit secrets/data in report.

## Completion output

Provide findings by severity, evidence, remediation, residual risk and release recommendation.
