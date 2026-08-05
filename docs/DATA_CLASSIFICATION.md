# Data Classification

## Classes

| Class | Examples | Handling |
|---|---|---|
| Public | Public project metadata, public repository information, public assets | May be served publicly after policy checks. |
| Internal | Operational metrics, non-sensitive logs, internal documentation | Authenticated staff/service access. |
| Confidential | Private source metadata, findings, project membership, private assets, AI reports | Encrypted in transit and at rest; project-scoped authorization. |
| Restricted | Password hashes, persisted refresh-token hashes, repository credentials, provider tokens, webhook secrets, private keys | Never returned to clients; minimal service access; rotation and audit required. A raw refresh token is returned only once in a successful login/refresh response and is never persisted or logged by the server. |

## Storage mapping

- Passwords: one-way adaptive hash only.
- Refresh tokens: the server persists only a hash; the raw one-time value is returned only in login/refresh success responses, then rotated and revoked according to session policy.
- Repository credentials: authenticated encryption or external secret vault reference.
- Source files: remain in Git/workspace; only bounded text may be persisted for approved artifacts.
- Protected assets: MinIO private bucket with object IDs and checksums in PostgreSQL.
- AI context: temporary or retained only by explicit policy; always redacted.
- Logs/traces: no Restricted data and no full confidential payloads.

## Data minimization

- Store only fields required for product behavior, audit, security or thesis evaluation.
- Use IDs and checksums instead of duplicating raw source/binary data.
- AI prompts contain only selected files/metadata needed for the requested analysis.

## Deletion and retention

Deletion requests follow `SRS/14-data-retention.md`. Legal hold or security investigation may suspend purge when explicitly recorded and authorized.