# Implementation Plan and Roadmap

This document outlines the ordered implementation roadmap for GodForge, maintaining the milestone dependencies defined in [MILESTONES.md](MILESTONES.md) and [Scope Specification](SRS/01-scope.md).

---

## 1. Milestone Dependency Order

```text
[M0: Foundation & Runtime] (Completed)
         |
         v
[M1: Internal Identity, Tenancy & Settings] (Completed - Internal foundation verified)
         |
         v
[M2: Hosted Git & Repository Foundation] (Partial implementation; real provider acceptance open)
         |
         v
[M3: Durable Worker & Pipeline Infrastructure]
         |
         v
[M4–M6: Godot Validation, Parser, AST & Health Engine]
         |
         v
[M7–M8: AI Advisory & Asset Vault]
         |
         v
[M9: Finding Collaboration & Technical Reports]
         |
         v
[M10: Production Release, Operations & Hardening]
```

---

## 2. Milestone Execution Roadmap

| Milestone | Target Scope | Current Status | Required Exit Evidence |
|---|---|---|---|
| **M0: Foundation & Runtime** | Clean Architecture solution, .NET 10 LTS target, Docker Compose, locked dependency restores, CI quality gates, container vulnerability scanning. | **Completed** | Full build, format, test passes in CI; reproducible clean-checkout execution. |
| **M1: Identity, Tenancy & Settings** | Authentication (`FR-01.*`), organization management (`FR-27`), project lifecycle (`FR-03`), membership hierarchy (`FR-27.1`, `FR-27.2`), settings & policies (`FR-17`). | **Completed (Internal Foundation)** | Internal tenancy, memberships, settings, and authorization verified across unit, integration, and E2E suites. External Forgejo reconciliation (`FR-27.2` / `AC-FR-27.2-02`) scheduled as an M2 acceptance requirement. |
| **M2: Hosted Git & Repositories** | Forgejo hosted Git provisioning, external linked Git adapters over HTTPS, branch/commit/tree navigation, signed webhooks, provider permission synchronization (`FR-27.2`). | **Partial implementation** | Real team creation, clone, push and browse; verified asynchronous external revocation; actual signed provider delivery and protection enforcement. |
| **M3: Durable Worker Pipeline** | Distributed job processing, transactional outbox/inbox, job retry with exponential backoff, dead-letter queues (DLQ), lock management, and cancellation tokens. | **Planned** | Reliable job dispatch under broker restarts; verified cleanup of temporary workspaces; idempotent message processing. |
| **M4–M6: Godot Intelligence & Health** | Godot validation gateway (`project.godot`), deterministic AST parser (`.tscn`, `.tres`, `.gd`), scene dependency graph, incremental analysis, and versioned health rules. | **Planned** | Deterministic and canonical parser outputs across Godot versions; verified equivalence between incremental and full analysis runs; hostile file rejection. |
| **M7–M8: AI Advisory & Asset Vault** | Gemini advisory integration with prompt redacting and schema validation; MinIO binary storage with access policies, manifests, and signed download URLs. | **Planned** | Advisory suggestions validated against deterministic rules; advisory failure degrades gracefully; private asset downloads restricted by scope and audited. |
| **M9: Collaboration & Reports** | Finding collaboration workflows, activity feeds, audit querying, and technical export in PDF, JSON, and CSV formats. | **Planned** | End-to-end finding resolution across revisions; authorized technical report downloads with verifiable provenance. |
| **M10: Production Release & Operations** | Observability dashboards, metrics/traces export, encrypted backup and restore drill, production secret management, and graduation defense demonstration. | **Planned** | Verified encrypted backup and restore drill; zero critical/high security findings; operational monitoring active. |

---

## 3. Next Milestone Focus: M2 Hosted Git & External Reconciliation

1. **Forgejo Repository Provisioning & Adapter Integration (`FR-04`–`FR-07`):**
   - Verify the implemented provisioning, browser and branch-protection adapters against real Forgejo.
   - Resolve provider registry access; test the isolated rootless candidate before upgrading retained data.

2. **Forgejo Reconciliation Outbox Consumer (`FR-27.2` / `AC-FR-27.2-02`):**
   - Verify the implemented desired-state consumer with real non-admin Git users, including member removal/suspension and stale delivery ordering.
   - Complete automatic webhook setup, linked-mode synchronization/browsing and the full M2 live journey.

3. **Analysis Profile Propagation (`AC-FR-17.4-01`):**
   - Propagate versioned analysis profiles into worker execution identities once the Godot parser engine (M4–M6) is initialized.

---

## 4. Implementation Rules

- Every new feature must satisfy the [Definition of Ready](DEFINITION_OF_READY.md) before code is written.
- Unit, integration, and E2E verification must pass before claiming completion (see [Testing Guide](TESTING.md)).
- Documentation must be updated synchronously with code changes.
