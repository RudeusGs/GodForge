# Environment and Secrets

## Local services

- PostgreSQL 16.
- Redis 7.
- RabbitMQ 3.13 with management UI.
- MinIO.
- Optional Forgejo 11 through the `hosted-git` Compose profile.

## Configuration groups

| Prefix | Purpose |
|---|---|
| `ConnectionStrings__` | PostgreSQL and Redis. |
| `RabbitMQ__` | Broker connection, queues and publisher behavior. |
| `RepositoryProcessing__` | Workspace root, quotas, Git timeouts and remote policy. |
| `Gemini__` | Server-side provider settings and budgets. |
| `Forgejo__` | Server-side API token, base URL and webhook secret. |
| `Email__` | SMTP provider. |
| `Jwt__` | API issuer, audience, expiry and signing secret. |
| `OutboxEncryption__` | Encryption key for protected outbox payloads; shared by API and Worker. |
| `M1Quotas__` | Current M1 limits for organizations, projects and pending invitations. |
| `Frontend__` | Trusted frontend URL. |
| `Storage__` or `MinIO__` | Buckets, endpoint and credentials. |
| `Observability__` | OTLP endpoint, service name and sampling. |

## Secret rules

- `.env` is local only and never committed.
- Production secrets come from a secret manager or protected deployment environment.
- JWT signing secret, outbox encryption key, Forgejo token, webhook secret, SMTP password, MinIO credentials and Gemini API key must be independently rotatable.
- Do not embed credentials in Git remote URLs.
- Do not place secrets in RabbitMQ messages, activity records, traces or client responses.
- Use separate credentials per environment and least-privilege service accounts.

## Production validation

Startup must fail safely when a mandatory production secret is missing, default, too short or obviously copied from examples. Gemini and Forgejo may be disabled explicitly; silent partial configuration is not allowed.
## Outbox encryption key migration

- Configure the same `OutboxEncryption__Key` value for API and Worker.
- New email outbox payloads use this key; the Worker decrypts them before delivery.
- When upgrading from a build that encrypted email outbox payloads with the JWT secret, temporarily set `OutboxEncryption__LegacyKey` to the previous JWT secret. Remove the legacy value after all pre-upgrade email messages have reached a terminal state.
- The Worker no longer needs JWT configuration, and only the Worker runs `OutboxDispatcherService`.
