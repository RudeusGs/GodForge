# Environment Variables & Secrets Management

## Local Development
- All local environment configuration should reside in `.env` (which is gitignored).
- `.env.example` must contain the full template of required keys but WITHOUT real secrets.
- Use Docker Compose to spin up local backing services relying on `.env`.

## Production Secrets
- Never commit secrets, API keys, or production passwords to Git.
- Use a secure secret manager (e.g., AWS Secrets Manager, Azure Key Vault, HashiCorp Vault) or CI-injected environment variables.
- Git Credentials (PATs) submitted by users must be encrypted using AES-256-GCM before database insertion.

## Required Keys Template
```env
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=godforge;Username=postgres;Password=localpass

# Cache
Redis__ConnectionString=localhost:6379

# Queue
RabbitMQ__HostName=localhost
RabbitMQ__UserName=guest
RabbitMQ__Password=guest

# Storage
Minio__Endpoint=localhost:9000
Minio__AccessKey=minioadmin
Minio__SecretKey=minioadmin

# Auth
Jwt__Key=A_VERY_LONG_SECRET_KEY_FOR_LOCAL_DEV
Jwt__Issuer=GodForgeLocal
Jwt__Audience=GodForgeClient
```
