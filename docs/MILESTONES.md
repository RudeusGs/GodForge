# GodForge Milestones

Do not start a later milestone until the previous milestone exit gate passes. Parallel documentation and isolated UI prototyping are allowed, but production integration must respect this order.

## M0 — Documentation and baseline stabilization

**Deliverables**

- Approved product scope, architecture, ADRs, SRS, threat model and traceability.
- Clean checkout restore/build/test.
- Reproducible local environment and baseline migration.
- CI quality gates.

**Exit gate**

- Documentation link check passes.
- Backend and frontend build/test pass from a clean checkout.
- No undocumented architecture conflict remains.

## M1 — Identity, organization, project and RBAC

- Registration, login, refresh rotation, logout, reset password and session management.
- Organization and project lifecycle.
- Member invitation, role changes and removal.
- Server-side permission checks and audit events.

**Exit gate**: cross-tenant access tests pass for every role.

## M2 — Repository foundation and hosted Git

- Linked HTTPS repository.
- Forgejo provisioning for hosted repositories.
- Branch, commit, tree and text-blob views.
- Signed webhooks, permission synchronization and protected-branch policy foundation.

**Exit gate**: a team member can create, clone, push and view a hosted Godot repository without credential leakage.

## M3 — Durable worker and secure workspace

- Durable job state, queue envelope, retry, timeout, cancellation and DLQ.
- Redis repository lock.
- Workspace isolation, quotas, cleanup and no untrusted execution.
- Outbox-based publication for production path.

**Exit gate**: duplicate and failed messages do not duplicate outputs or leave stale locks/workspaces.

## M4 — Godot validation and deterministic parser

- Validation of `project.godot`, supported version, paths, symlinks, file count and size.
- Parsing of project config, `.tscn`, `.tres`, `.gd`, shader and supported metadata.
- Versioned normalized JSON and diagnostics.

**Exit gate**: parser corpus tests are reproducible and measured.

## M5 — Health engine, graph and explorers

- Dependency graph, scene explorer and asset explorer.
- Versioned health rules, evidence, severity and score.
- Finding suppression with reason and audit.

**Exit gate**: reports are reproducible for the same revision and rule-set version.

## M6 — Incremental analysis and revision comparison

- Git diff-driven affected-file calculation.
- Dependency impact expansion.
- Safe fallback to full analysis.
- Scene/revision diff and score comparison.

**Exit gate**: benchmark proves reduced work while preserving result equivalence for the test corpus.

## M7 — Gemini advisory

- Bounded context selection and secret redaction.
- Prompt/version/input hashing.
- Structured JSON schema validation, evidence and degraded mode.
- AI usage, latency and cost metrics.

**Exit gate**: deterministic report remains complete when Gemini is disabled or unavailable.

## M8 — Asset Vault

- Object upload, hash deduplication, versioning and visibility policy.
- Repository asset manifest.
- Signed download and audit history.
- Public source with non-public assets.

**Exit gate**: unauthorized users cannot enumerate or download protected assets; authorized restore reproduces expected files and hashes.

## M9 — Collaboration, dashboard and reporting

- Assign, comment, resolve, ignore and reopen findings.
- Notifications and activity timeline.
- Dashboard trends and operational status.
- PDF/JSON/CSV report export.

**Exit gate**: complete team remediation workflow works across two revisions.

## M10 — Production hardening and thesis evaluation

- OpenTelemetry, metrics, dashboards, alerts and runbooks.
- Backup/restore verification and retention jobs.
- Security, performance, reliability and usability experiments.
- Production deployment and final demonstration dataset.

**Exit gate**: release checklist passes and thesis measurements are reproducible.

## Deferred after graduation release

- Full pull-request review engine.
- General-purpose CI/Actions platform.
- Web IDE and merge-conflict editor.
- Package registry, wiki and marketplace.
- Running or exporting untrusted games on the platform.
- Automatic AI code modification or automatic push.
