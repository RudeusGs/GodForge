# Threat Model

## Security objectives

1. Prevent cross-organization and cross-project data access.
2. Prevent repository content from escaping or compromising worker hosts.
3. Protect Git, object-storage, AI and email credentials.
4. Preserve analysis integrity and reproducibility.
5. Maintain availability under large or malicious repositories.
6. Provide audit evidence for sensitive actions.

## Trust boundaries

- Browser to API.
- API to PostgreSQL/Redis/RabbitMQ/MinIO/Forgejo/Gemini/email.
- RabbitMQ to Worker.
- Worker to untrusted repository workspace.
- API to signed object-storage download.
- Forgejo/external provider to webhook endpoint.

## Threat register

| ID | Threat | Impact | Required controls | Residual risk |
|---|---|---|---|---|
| T-01 | IDOR/cross-project access | Critical | Tenant-scoped queries, Application RBAC, authorization tests, masked 404 | Logic regression remains possible; continuous tests required. |
| T-02 | SSRF via clone URL | Critical | HTTPS allow rules, DNS/IP resolution checks, private/link-local block, redirect limits | DNS rebinding requires resolution re-check. |
| T-03 | Credential leakage | Critical | Encryption/vault, sanitized URL, no message/log inclusion, rotation | Compromised service account can still access permitted repositories. |
| T-04 | Path traversal | Critical | Canonical root checks, path normalization, reject `..`, safe extraction | Parser/library defects require updates. |
| T-05 | Symlink escape | Critical | Do not follow external symlinks; verify resolved path stays in root | Platform-specific filesystem behavior. |
| T-06 | Untrusted code execution | Critical | Static analysis only, non-root worker, no Docker socket, ADR 0013 | Vulnerability in Git/parser dependency. |
| T-07 | Resource exhaustion | High | Repository/file quotas, timeouts, concurrency limits, disk accounting, rate limits | Large valid projects may need configurable tiers. |
| T-08 | Webhook forgery/replay | High | HMAC/provider signature, timestamp/event ID, replay store, idempotency | Provider key compromise. |
| T-09 | Queue poisoning/duplicate delivery | High | Versioned schema, inbox, idempotent handlers, DLQ | Operational backlog. |
| T-10 | Prompt injection from source | High | Treat source as data, fixed system policy, structured context, output schema, no tool authority | Misleading advisory may remain; label AI output. |
| T-11 | Secret exfiltration to AI | Critical | Secret scanning/redaction, file denylist, organization opt-out, audit | Novel secret formats may evade detection. |
| T-12 | Asset unauthorized download | Critical | Per-asset policy, signed URL after authorization, short TTL, audit | URL sharing during TTL; keep TTL minimal. |
| T-13 | Stored XSS from README/source | High | Escape text, sanitize Markdown/HTML, CSP | Sanitizer bypass vulnerability. |
| T-14 | SQL injection | High | Parameterized EF queries, no raw interpolation, validation | Provider/library vulnerabilities. |
| T-15 | Token theft | High | Short access token, refresh rotation, revocation, HTTPS, and an HttpOnly SameSite=Strict refresh cookie scoped to auth endpoints | Compromised client device or same-origin request abuse. |
| T-16 | Privilege drift in Forgejo | High | Hosted repository provisioning is isolated; no unprocessable membership-reconciliation messages are emitted | Provider membership synchronization is not implemented in the current source and must not be assumed. |
| T-17 | Analysis result tampering | High | Immutable version identity, checksums, authorization, audit | Privileged database compromise. |
| T-18 | Malicious asset upload | High | MIME/magic validation, size limits, malware scan, no execution, private bucket | Unknown malware not detected. |
| T-19 | Backup exposure | Critical | Encryption, restricted access, tested retention and deletion | Backup operator compromise. |
| T-20 | Audit log tampering | High | Append-only policy, hash chaining/immutable export, restricted admin access | Database administrator compromise. |

## Abuse cases

- User creates many projects/jobs to exhaust queue and storage.
- User links a remote that redirects to internal metadata service.
- Repository includes `.env`, private key, huge binary, recursive symlink or crafted filenames.
- Source contains instructions telling Gemini to reveal secrets or ignore policy.
- Removed member reuses stale Forgejo access or signed asset URL.
- Duplicate webhook creates repeated analysis and cost.

## Security review trigger

Update this threat model before adding dynamic project execution, new external providers, public anonymous uploads, billing, organization-wide search or cross-region deployment.
