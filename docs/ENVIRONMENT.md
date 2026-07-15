# Environment and Secrets

Use one root `.env` for local Docker and environment-variable overrides. The committed `.env.example` contains names/defaults only; `.env` is ignored.

## Required local services

- PostgreSQL: `godforge/godforge_local_password/godforge` by default.
- Redis: `localhost:6379`.
- RabbitMQ: `godforge/godforge_local_password`, AMQP `5672`, management `15672`.
- MinIO: API `9000`, console `9001`.
- Forgejo: optional `hosted-git` profile, HTTP `3000`, SSH `2222`.

## Application configuration

ASP.NET configuration uses environment keys such as:

```text
ConnectionStrings__DefaultConnection
ConnectionStrings__Redis
RabbitMQ__Enabled
RepositoryProcessing__WorkspaceRoot
Gemini__Enabled
Gemini__ApiKey
Forgejo__Enabled
Forgejo__ApiToken
Jwt__Secret
```

Gemini/Forgejo API keys and repository credentials must be injected through a secret manager or environment. Never expose them through Vite variables.
