# Environment and Secrets

## Local services

- PostgreSQL 16.
- Redis 7.
- RabbitMQ 3.13 with management UI.
- MinIO.
- Optional Forgejo 11 through the `hosted-git` Compose profile.

## M2 provider development

The legacy root profile still uses Forgejo 11 and must not be deployed: its
support has ended. A pinned supported 15.0.11 rootless image is provided in
`docker-compose.hosted-git.yml`. It uses separate volumes and does not migrate
or overwrite an existing Forgejo 11 installation. Its manifest digest is pinned;
the official `data.forgejo.org/forgejo/forgejo` mirror serves the same digest.
The disposable provider adapter test has executed successfully. This does not
validate an upgrade of retained data. Back up and test an upgrade on copied data before changing an existing
instance. See the [Forgejo upgrade guide](https://forgejo.org/docs/v15.0/admin/upgrade/).

1. Put a random `FORGEJO_DB_PASSWORD` in ignored `.env`, then run
   `docker compose -f docker-compose.backend.yml -f docker-compose.hosted-git.yml up -d forgejo`.
2. Open loopback `http://localhost:13000`, initialize the instance and choose
   a unique administrator password locally. No administrator credentials are
   tracked or supplied to GodForge. Public registration and Actions are disabled.
3. Create a dedicated **non-admin** hosting user with no organization/team
   membership. Generate a token with `write:user` and `write:repository` scopes
   limited to the repositories that account owns. Do not reuse personal accounts.
4. Set `Forgejo__Enabled=true`, `Forgejo__ApiToken` and an independently random
   `Forgejo__WebhookSecret` of at least 32 characters in ignored server settings.
   Recreate API and Worker with both Compose files. They reach `http://forgejo:3000`
   on their shared network; browser clone URLs use `http://localhost:13000`.
   For host processes, configure BaseUrl/PublicUrl to the published provider URL
   and explicitly set `Forgejo__AllowLocalHttp=true`. HTTPS is required elsewhere.
5. Verify `/api/v1/user` using the service token in an authorization header;
   confirm `is_admin=false`. Never put the token in a URL or frontend variable.
6. Users bind their own non-admin provider account through
   `POST /api/v1/users/me/forgejo-identity`. Use a temporary `read:user` personal
   token, then revoke it. Git operations use each user's own Forgejo credentials
   or SSH key, never the GodForge service token.
7. Configure `Forgejo__WebhookUrl` to
   `http://api:8080/api/v1/webhooks/forgejo` with the configured secret. The Worker
   installs and verifies a managed JSON webhook before marking provisioning ready.
   Reconciliation also repairs missing hooks and rotates their secret. Forgejo
   outbound webhook policy must explicitly permit this trusted development host;
   do not broadly allow private-network deliveries. Production uses an HTTPS
   callback. The Compose override allows only the `api` webhook destination.

Run `npm.cmd run test:m2:provider` for a disposable real adapter test, or
`npm.cmd run test:m2:acceptance` for API/outbox/RabbitMQ/Worker/provider/Git/browser
acceptance. Both refuse to reuse an existing `godforge-m2-acceptance` project,
generate process-local credentials and remove only their owned containers and
volumes. The full suite uses loopback ports 35072, 35173, 38025, 33000 and 32222.
Its linked HTTPS fixture trusts a temporary CA only inside its test Worker and
allows only `linked-tls`; it does not change production TLS or network policy.
Linked browsing requires the shared workspace volume mounted read-only in the
API and writable in the Worker. Both runtime images include Git for bounded
object reads; only the Worker fetches remotes.

Provider permissions reconcile asynchronously and the API exposes pending/error
timestamps. A five-minute drift scan repairs out-of-band changes. Alert on access
errors, oldest pending timestamps and `repository.hosted.reconcile` dead letters.
Do not claim external revocation until real Git access tests pass. Private
repositories prevent anonymous access; provider instance administrators retain
their inherent administrative powers.

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
