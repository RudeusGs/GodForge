# 6. Security Requirements

## Identity and session

- `SEC-01`: adaptive password hashing and constant-time verification behavior.
- `SEC-02`: short-lived access tokens, rotating refresh families and revocation.
- `SEC-03`: rate limits and lockout for authentication/OTP/reset abuse.
- `SEC-04`: MFA support for privileged production accounts.

## Authorization and tenancy

- `SEC-05`: organization/project scope is enforced in Application logic.
- `SEC-06`: every resource query validates membership and permission.
- `SEC-07`: cross-tenant existence is masked where required.
- `SEC-08`: SystemAdmin actions record actor, reason, target and outcome.

## Git and workspace

- `SEC-09`: external remote URLs pass scheme, DNS/IP, redirect and private-network policy.
- `SEC-10`: credentials are encrypted/vaulted and absent from logs/messages/client output.
- `SEC-11`: Git commands use safe argument invocation with timeout and cancellation.
- `SEC-12`: workspace canonical paths remain under configured root; symlink escapes are rejected.
- `SEC-13`: repository file count, byte, depth and processing quotas are enforced.
- `SEC-14`: standard analysis never executes repository code, plugins, binaries or Godot Editor.

## Webhooks and messaging

- `SEC-15`: provider signature, timestamp/replay and repository identity are validated.
- `SEC-16`: messages use versioned schemas, no secrets and bounded references.
- `SEC-17`: consumers are idempotent; poison messages go to DLQ.

## AI

- `SEC-18`: context selection denies binary/generated/sensitive paths.
- `SEC-19`: secret scanning/redaction occurs before provider request.
- `SEC-20`: repository content is untrusted prompt data.
- `SEC-21`: AI output has no authority to mutate code, users, permissions, Git or health score.
- `SEC-22`: organization can disable external AI.

## Asset Vault

- `SEC-23`: protected buckets are private.
- `SEC-24`: signed URL issued only after current authorization and has short TTL.
- `SEC-25`: upload validates size, MIME/magic and quarantine state.
- `SEC-26`: policy changes and downloads are audited.

## Web/API/browser

- `SEC-27`: repository text, Markdown, filenames and comments are escaped/sanitized.
- `SEC-28`: CORS, TLS, trusted proxy and security headers are environment-specific and restrictive.
- `SEC-29`: errors exclude stack traces, SQL, credentials, raw provider payloads and workspace paths.
- `SEC-30`: state-changing APIs validate allowed fields and prevent mass assignment.

## Data and operations

- `SEC-31`: Restricted data classification handling follows `../DATA_CLASSIFICATION.md`.
- `SEC-32`: backups are encrypted and access-controlled.
- `SEC-33`: dependency, secret and container scans are release gates.
- `SEC-34`: audit logs are append-oriented and protected from normal mutation.

The detailed threats and test cases are in `../THREAT_MODEL.md` and `../SECURITY_TEST_PLAN.md`.
