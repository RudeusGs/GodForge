# GodForge Product Vision

## Vision

GodForge helps Godot teams understand, secure and operate their projects as structured engineering systems rather than opaque folders of scenes, scripts and assets.

## Product statement

> GodForge is a Git-based platform for hosting, validating, analyzing and visualizing Godot Engine projects. It provides repository collaboration, deterministic project intelligence, health scoring, AI-assisted recommendations and asset-level visibility controls suitable for individual developers, teams and organizations.

## Primary users

- Independent Godot developers.
- Student and research teams.
- Small and medium game studios.
- Company teams that need private code, controlled assets, auditability and repeatable analysis.
- Technical reviewers, mentors and project managers who need project-health visibility without reading every file manually.

## Problems solved

1. Godot repositories are difficult to understand at scale because scene, script, resource and asset relationships are distributed across text files.
2. Generic Git platforms do not provide Godot-aware project validation, scene graph exploration or dependency impact analysis.
3. AI-only code review is non-deterministic and can hallucinate. Teams need reproducible parser and rule-engine output first.
4. Repository visibility is too coarse for teams that want public source but private commercial assets.
5. Long-running clone, parse and analysis operations require durable asynchronous processing, observability and retry safety.
6. Companies require project-scoped authorization, audit history, secret protection and production operations that a simple student CRUD application does not provide.

## Differentiators

- Godot project validation before analysis and optional policy enforcement before protected-branch acceptance.
- Deterministic parsing of `project.godot`, scenes, resources, scripts and dependency relationships.
- Versioned health rules and reproducible health scores.
- Interactive dependency graph and revision-to-revision impact visualization.
- Gemini advisory generated from bounded, redacted, structured context rather than uncontrolled whole-repository prompts.
- Asset Vault that separates source-code visibility from asset visibility.
- Hosted Git through Forgejo without reimplementing Git protocol internals.
- Production-oriented worker pipeline, idempotency, audit logging, observability and recovery procedures.

## Product principles

1. **Deterministic before generative**: parser and rule engine are authoritative; AI is advisory.
2. **Git-first, Godot-aware**: Git remains source-of-code truth; GodForge adds domain intelligence.
3. **No untrusted execution**: uploaded repositories are data, not executable workloads.
4. **Least privilege**: every API, job, artifact and asset access is project-scoped and server-authorized.
5. **Immutable revision analysis**: analysis is bound to commit SHA and versioned configuration.
6. **Async by default for heavy work**: HTTP requests create durable jobs and return quickly.
7. **Evidence over claims**: production readiness and thesis results are demonstrated by tests and measurements.
8. **Depth over feature imitation**: implement Godot-specific capabilities deeply instead of copying every GitHub feature.

## Graduation-project success definition

The graduation project is successful when the complete end-to-end flow is production-deployed and measurable:

1. A user creates an organization and project.
2. The user creates or links a repository.
3. A valid Godot project is pushed or synchronized.
4. A signed webhook or manual action creates a durable analysis job.
5. The worker securely obtains an immutable revision.
6. Validation, parsing, health analysis and dependency graph generation complete.
7. Optional Gemini advisory is generated without exposing excluded secrets.
8. The UI displays revision history, health findings, graph, scenes and assets.
9. Team members assign and resolve findings across commits.
10. Public source can reference separately protected assets from the Asset Vault.
11. The system exports a technical report and exposes operational metrics.
12. Security, performance, reliability and usability are evaluated in the thesis.

## Explicit positioning

Do not market GodForge as “GitHub 100% for Godot.” Use:

> A specialized Git and project-intelligence platform for Godot Engine teams.
