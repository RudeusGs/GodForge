# Merge Readiness — Blueprint Migration

Current status: **NOT READY UNTIL VALIDATED ON A MACHINE WITH .NET 9, Docker AND NETWORK PACKAGE ACCESS**.

The migration pack is structurally prepared, but merge requires all quality gates in `QUALITY_GATES.md` and a generated/reviewed EF migration.

## Required blockers to close

- Generate and commit `GodForge-FE/package-lock.json`.
- Restore/build/test .NET projects.
- Generate initial blueprint migration or reviewed forward migration.
- Start PostgreSQL, Redis, RabbitMQ and MinIO with root Compose.
- Run one linked-repository analysis fixture.
- Verify secret redaction and Gemini-disabled degraded behavior.
- Review provider/data privacy policy before enabling Gemini.
- Do not enable Forgejo hosted Git in production before permission synchronization and signed webhook tests exist.
