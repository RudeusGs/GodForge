# GodForge — Production and Graduation Project Blueprint

## 1. System identity

GodForge is a specialized Git and project-intelligence platform for Godot Engine. It is designed to be deployable for real users while providing sufficient architectural, analytical and experimental depth for a graduation thesis.

## 2. High-level capabilities

### Repository and collaboration

- External linked Git repositories over HTTPS.
- Internally hosted Git repositories through Forgejo.
- Project membership and role-based access.
- Branch, commit, tree, text-blob and revision views.
- Signed push webhooks and automatic analysis pipeline.

### Godot intelligence

- Secure validation of Godot repository structure.
- Deterministic parsing to normalized metadata and JSON.
- Scene, script, resource and asset dependency graph.
- Project-health rules with evidence and versioned scoring.
- Scene/revision comparison and affected-component analysis.

### AI advisory

- Gemini receives selected structured context, not uncontrolled raw repository dumps.
- Secrets and excluded files are removed before provider calls.
- Output is advisory, schema-validated, versioned and auditable.
- Provider failure never invalidates deterministic results.

### Asset Vault

- Binary assets stored in MinIO instead of public Git history when independent visibility is required.
- Public, project, organization, selected-member and owner-only policies.
- Versioned manifest, checksum validation and signed download.
- Download audit and retention policy.

### Production engineering

- ASP.NET Core API, Vue 3 frontend, PostgreSQL, Redis, RabbitMQ, MinIO, Forgejo and .NET worker.
- Clean Architecture and CQRS use cases.
- Durable asynchronous processing with idempotency and DLQ.
- Threat model, least privilege, audit history and isolated repository workspaces.
- Metrics, traces, backup/restore and incident runbooks.

## 3. Architecture

```text
Browser / Godot plugin / optional CLI
                  |
                  v
             Vue 3 Web UI
                  |
                  v
           ASP.NET Core API
     +------------+-------------+
     |            |             |
 PostgreSQL     Redis       RabbitMQ
 durable data   cache/lock   job transport
     |                          |
     |                          v
     |                    GodForge Worker
     |           +----------+-----+---------+
     |           |          |     |         |
     |        Git/Forgejo  Parser Health  Gemini
     |                               |      advisory
     +-------------------------------+
                    |
                  MinIO
      assets, previews, reports, large artifacts
```

## 4. Source-of-truth boundaries

| Concern | Authoritative system |
|---|---|
| Git objects, refs, clone/push | Forgejo or external Git provider |
| Users, organizations, projects, memberships | PostgreSQL |
| Jobs and business state | PostgreSQL |
| Queue delivery | RabbitMQ, never treated as business state |
| Cache and distributed locks | Redis |
| Generated artifacts and protected assets | MinIO |
| Parsed metadata and health findings | PostgreSQL, versioned by revision and engine versions |
| AI recommendations | PostgreSQL as advisory output with provider/version metadata |

## 5. Canonical analysis pipeline

```text
push/webhook/manual trigger
  -> durable job creation
  -> immutable commit resolution
  -> isolated secure checkout
  -> Godot validation
  -> repository inventory
  -> deterministic parse
  -> dependency graph
  -> health rule evaluation
  -> incremental impact calculation when eligible
  -> bounded and redacted AI context
  -> optional Gemini advisory
  -> atomic persistence and artifact publication
  -> notifications, metrics and UI update
```

A result identity includes:

```text
repositoryId + commitSha + parserVersion + ruleSetVersion
+ analysisProfileVersion + promptVersion + inputHash
```

## 6. Security posture

- Repository contents are hostile input.
- No repository script, plugin, binary or Godot project is executed by the API or standard worker.
- Git URLs are normalized and checked against SSRF rules.
- Workspaces are rooted, non-shared, quota-limited and deleted after use.
- Symlink and path traversal escapes are rejected.
- Credentials are encrypted, never embedded in URLs and never placed in queue messages.
- Project and organization scope is checked in Application logic for every request.
- AI context excludes secrets, private keys, tokens, generated folders and disallowed files.
- Asset access is authorized before short-lived signed URLs are issued.

## 7. Data and performance strategy

- PostgreSQL stores metadata, not large source trees or binary assets.
- All large lists are paginated; commit/activity feeds use cursor pagination when appropriate.
- Parser output is batch inserted and upserted by deterministic keys.
- Content hashes deduplicate assets and immutable analysis artifacts.
- Incremental analysis starts from changed files and expands through reverse dependencies.
- Redis caches read-heavy summaries and protects mutable repository workspaces with owner-token locks.
- Retention policies remove obsolete generated data while preserving required audit and legal-hold records.

## 8. Thesis research contribution

1. A normalized graph representation of Godot project structure.
2. A reproducible, versioned project-health scoring method.
3. Incremental Godot analysis based on Git changes and dependency impact.
4. Independent asset visibility despite repository visibility.
5. Hybrid deterministic analysis plus generative advisory.
6. Measured security and performance behavior under representative repository sizes.

## 9. Completion discipline

A large scope is acceptable only when the Core path is complete. Each module must have requirements, data model, API, authorization, tests, observability and acceptance evidence. Feature count without end-to-end reliability does not satisfy the project goal.
