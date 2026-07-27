# Implementation Milestones — Blueprint Order

Không bắt đầu phase sau trước khi exit gate của phase trước pass.

1. **Stabilization**: clean checkout build/test/migrate; lockfile; environment; CI; baseline migration.
2. **Linked repository**: link HTTPS Git repository, manual sync job, commit/revision record.
3. **Worker foundation**: RabbitMQ, durable job state, retry/DLQ, repository lock, workspace cleanup.
4. **Deterministic Godot parser**: `project.godot`, `.tscn`, `.tres`, `.gd`, diagnostics.
5. **Health and dependency graph**: measured findings and versioned health score.
6. **Bounded GitIngest context**: ignore, binary exclusion, redaction, quota, input hash.
7. **Gemini advisory**: manual trigger, structured output, evidence, usage tracking, degraded mode.
8. **Internal hosted repository**: Forgejo provisioning, permission sync, signed webhook, auto pipeline.
9. **Review and scene-aware diff**: compare revisions and dependency impact.
10. **Observability and deployment hardening**: metrics, alerts, backups, retention and production secrets.

## Non-goals for MVP

- Git protocol implementation.
- Full pull-request/issue/wiki/actions replacement.
- Web IDE or merge-conflict editor.
- Automatic AI code modification/push.
- Running or exporting untrusted games on the API/worker host.
