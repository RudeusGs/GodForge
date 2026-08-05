# Incident Response

## Severity

- **SEV-1**: confirmed credential compromise, cross-tenant data exposure, destructive loss or active remote-code execution.
- **SEV-2**: major outage, unauthorized access contained to one tenant, persistent corruption or unavailable recovery path.
- **SEV-3**: limited degradation with workaround, delayed jobs or provider outage.
- **SEV-4**: minor defect or alert requiring planned correction.

## Process

1. Detect and assign incident commander.
2. Contain access or traffic without destroying evidence.
3. Preserve logs, audit records, job IDs, hashes and relevant configuration.
4. Eradicate root cause and rotate affected secrets.
5. Recover using tested runbooks.
6. Notify affected stakeholders according to policy.
7. Complete post-incident review with corrective actions and owners.

## Prohibited actions

- Do not paste secrets or private source into chat/tickets.
- Do not delete logs or workspaces that may be evidence before authorization.
- Do not silently modify audit history.
