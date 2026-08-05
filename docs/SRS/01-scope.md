# 1. Scope

## 1.1 Core release — Must

- Identity, secure sessions, organizations, projects, members and RBAC.
- External linked repository and Forgejo-hosted repository.
- Clone/push/pull through Git engine; branch, commit, tree and text-blob browsing.
- Signed webhook and manual synchronization.
- Durable job system, outbox/inbox, retry, cancellation, timeout, DLQ and repository locks.
- Godot validation gateway.
- Deterministic parser and normalized metadata.
- Scene/asset explorers, dependency graph and health analysis.
- Incremental analysis with safe full-analysis fallback.
- Gemini advisory with redaction, limits, evidence and degraded mode.
- Finding collaboration, activity, notifications and dashboard.
- Asset Vault with independent visibility and signed download.
- Revision comparison and report export.
- Production deployment, observability, backup/restore and security/performance testing.

## 1.2 Advanced release — Should

- Protected-branch policy based on critical validation/health findings.
- Godot Editor plugin or CLI for Asset Vault hydration and local validation.
- Organization policies and quotas.
- Malware scanning and asset preview generation.
- Scheduled analysis and retention jobs.

## 1.3 Extensions — Could

- Repository chat over approved context.
- More advanced GDScript semantic analysis.
- Multi-provider AI abstraction.
- Pull-request-like review focused on scene/health changes.
- Self-hosted AI provider.

## 1.4 Explicit non-goals

- Custom Git Smart HTTP or SSH protocol implementation.
- Full GitHub clone: Actions, Packages, Wiki, Marketplace and general-purpose issue/PR platform.
- Web IDE or merge-conflict editor.
- Running, building or exporting untrusted Godot projects in the standard API/worker environment.
- Automatic AI code changes or automatic pushes.
- Claiming absolute security or zero vulnerability.

## 1.5 Constraints

- Initial worker deployment may be one host with logical workers.
- Production-ready status requires outbox/inbox, permission reconciliation, backups and observability.
- AI may be disabled per organization and cannot be required for deterministic reports.
- Asset Vault hydration requires a client integration because Git clone alone cannot retrieve protected objects.
