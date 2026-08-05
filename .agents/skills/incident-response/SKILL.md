---
name: incident-response
description: Handle a production or staging security/reliability incident.
---

# Incident Response

## Use when

Handle a production or staging security/reliability incident.

## Required reading

- `docs/INCIDENT_RESPONSE.md`
- `docs/OPERATIONS_RUNBOOK.md`
- `docs/THREAT_MODEL.md`

## Workflow

1. Classify severity and assign incident commander.
2. Contain without destroying evidence.
3. Preserve safe logs, audit, hashes, job/message IDs and config versions.
4. Identify affected tenants/data/providers.
5. Eradicate root cause and rotate credentials when needed.
6. Recover through runbooks and verify.
7. Document timeline, impact and corrective actions.

## Mandatory checks

- Cross-tenant/credential/RCE incidents treated as SEV-1.
- Communications exclude secrets/private source.
- Manual repair is audited.
- Post-incident tests prevent recurrence.

## Forbidden

- Do not delete evidence or silently edit audit history.
- Do not paste secrets into tickets/chat.
- Do not restore service before containment/validation when exposure continues.

## Completion output

Provide severity, timeline, containment, impact, recovery, rotated secrets and follow-up owners.
