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
| `SecretHashing__` | HMAC key for OTP, invitation and password-reset challenge tokens. |
| `OutboxEncryption__` | Encryption key for protected outbox payloads; shared by API and Worker. |
| `M1Quotas__` | Current M1 limits for organizations, projects, pending invitations and concurrent active user sessions (`MaxActiveSessionsPerUser`, default `10`, minimum `2`). |
| `Frontend__` | Trusted frontend URL. |
| `ReverseProxy__KnownProxies` / `ReverseProxy__KnownNetworks` | Explicit trusted proxy IPs/CIDRs used for forwarded client IP/proto; never configure an unrestricted network. |
| `Storage__` or `MinIO__` | Buckets, endpoint and credentials. |
| `Observability__` | OTLP endpoint, service name and sampling. |

## Secret rules

- `.env` is local only and never committed.
- Production secrets come from a secret manager or protected deployment environment.
- JWT signing secret, challenge-token hashing key, outbox encryption key, Forgejo token, webhook secret, SMTP password, MinIO credentials and Gemini API key must be independently rotatable.
- `SecretHashing__Key` must be at least 32 characters and must not equal `Jwt__Secret`. During rollout, `SecretHashing__LegacyKey` may temporarily contain the previous JWT secret so outstanding OTP, invitation and password-reset tokens remain verifiable; remove it after their maximum lifetime.
- Do not embed credentials in Git remote URLs.
- Do not place secrets in RabbitMQ messages, activity records, traces or client responses.
- Use separate credentials per environment and least-privilege service accounts.

## Production validation

Startup must fail safely when a mandatory production secret is missing, default, too short or obviously copied from examples. Gemini and Forgejo may be disabled explicitly; silent partial configuration is not allowed. Enabled Forgejo endpoints must use HTTPS, except loopback HTTP endpoints used for local development. SMTP configuration must be omitted completely or provide a valid host and sender identity.

Redis is mandatory for API startup outside Development/Testing because authentication abuse counters must be shared by all API instances. A runtime Redis failure causes auth-sensitive endpoints to fail closed with `503 DEPENDENCY_UNAVAILABLE`; operators must alert on this condition. Configure only the immediate reverse proxy addresses/networks under `ReverseProxy__KnownProxies` / `ReverseProxy__KnownNetworks`; untrusted `X-Forwarded-For` input is ignored.
## Outbox encryption key migration

- Configure the same `OutboxEncryption__Key` value for API and Worker.
- New email outbox payloads use this key; the Worker decrypts them before delivery.
- When upgrading from a build that encrypted email outbox payloads with the JWT secret, temporarily set `OutboxEncryption__LegacyKey` to the previous JWT secret. Remove the legacy value after all pre-upgrade email messages have reached a terminal state.
- The Worker no longer needs JWT configuration, and only the Worker runs `OutboxDispatcherService`.
