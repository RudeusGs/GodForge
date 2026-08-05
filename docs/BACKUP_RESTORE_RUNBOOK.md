# Backup and Restore Runbook

## Backup scope

- PostgreSQL GodForge database.
- PostgreSQL Forgejo database.
- Forgejo Git data/configuration.
- MinIO buckets for assets and artifacts.
- Deployment configuration excluding secrets that are managed separately.

Redis and RabbitMQ are not the only source of business truth and are rebuilt/reconciled rather than treated as primary backups.

## Requirements

- Encrypted backups.
- Separate failure domain from production.
- Defined retention tiers.
- Restore tests at least before major release and on a scheduled basis.
- Backup metadata includes timestamp, environment, schema version and checksum.

## Restore order

1. Freeze writes and record incident point.
2. Restore PostgreSQL databases.
3. Restore Forgejo data and verify refs/objects.
4. Restore MinIO objects.
5. Reconfigure service secrets and endpoints.
6. Start dependencies, API, then workers.
7. Reconcile outbox/inbox, jobs, Forgejo permissions and object checksums.
8. Run smoke tests and document RPO/RTO achieved.

## Verification

A successful restore must prove login, project access, hosted clone, revision read, health report read and protected asset download for a controlled test tenant.
