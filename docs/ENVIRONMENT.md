# Environment Variables & Secrets Management

## Local Development
- All local infrastructure environment configuration (e.g. for Docker Compose) should reside in a root `.env` (which is gitignored).
- Backend local settings should be in `GodForge-BE/appsettings.Development.json` or user secrets.
- Frontend public Vite variables should be in `GodForge-FE/.env.local`.
- `.env.example` must contain the full template of required keys but WITHOUT real secrets.
- Use Docker Compose to spin up local backing services relying on the root `.env`.

## Production Secrets
- Never commit secrets, API keys, or production passwords to Git.
- Use a secure secret manager (e.g., AWS Secrets Manager, Azure Key Vault, HashiCorp Vault) or CI-injected environment variables.
- Git Credentials (PATs) submitted by users must be encrypted using AES-256-GCM before database insertion.

## Required Keys Template (`.env.example`)
```env
# Database
POSTGRES_USER=postgres
POSTGRES_PASSWORD=localpass
POSTGRES_DB=godforge

# RabbitMQ
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest

# MinIO
MINIO_ROOT_USER=minioadmin
MINIO_ROOT_PASSWORD=minioadmin
```
