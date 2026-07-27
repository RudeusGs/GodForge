# Deployment and Operations

## Base services

PostgreSQL, Redis, RabbitMQ and MinIO start with `docker compose up -d`. Forgejo starts only with `docker compose --profile hosted-git up -d`.

## Secrets

Production uses a secret manager or injected environment variables. Never commit `.env`, provider tokens, JWT secrets, SMTP passwords or repository credentials.

## Worker

API and worker are separate processes. Worker requires Git CLI and writable ephemeral workspace storage. Production should use a non-root container, disk quota, network egress policy, CPU/memory limits and scheduled workspace cleanup.

## Observability

All API/job logs include correlation ID, job ID and repository ID where applicable. Metrics include queue depth, job duration, retries/DLQ, workspace bytes, parser duration, context size, AI tokens and provider failures. Logs never include source content or credentials.

## Backup

Back up PostgreSQL and MinIO. Forgejo repository storage requires its own backup policy once hosted Git is enabled. RabbitMQ is not the source of business state.
