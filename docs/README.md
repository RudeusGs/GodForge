# GodForge Technical Documentation

This directory contains the authoritative architecture, functional requirements, database designs, API contracts, security models, testing guidelines, and operational runbooks for GodForge.

---

## 1. Product & Architecture

| Document | Purpose |
|---|---|
| [Product Vision](PRODUCT_VISION.md) | High-level product definition, differentiation, and problem domain. |
| [System Architecture](SRS/02-architecture.md) | Clean Architecture boundaries, service topology, and communication flow. |
| [Backend Architecture](BACKEND_ARCHITECTURE.md) | ASP.NET Core (.NET 10), CQRS/MediatR, transactional outbox, and EF Core persistence. |
| [Frontend Architecture](FRONTEND_ARCHITECTURE.md) | Vue 3 + TypeScript, Pinia, session coordination, and workspace layout. |
| [Architecture Decision Records (ADRs)](ADR/README.md) | Accepted architectural decisions and immutable constraints. |
| [System Scope](SRS/01-scope.md) | Core, Advanced, and Extension milestone boundaries. |
| [Software Requirements Specification](SRS/00-overview.md) | Comprehensive SRS index across all functional areas. |

---

## 2. API & Data Contracts

| Document | Purpose |
|---|---|
| [API Specifications](SRS/05-api.md) | REST API catalog, common envelopes, pagination, and error patterns. |
| [Auth API Contract](SRS/05-api-contracts/auth.md) | Registration, OTP challenge, session refresh, logout, and password recovery. |
| [Organizations API Contract](SRS/05-api-contracts/organizations.md) | Multi-tenant organization CRUD, members, invitations, and ownership transfer. |
| [Projects API Contract](SRS/05-api-contracts/projects.md) | Project lifecycle, assigned project list, catalog, members, and archive/restore. |
| [Settings API Contract](SRS/05-api-contracts/settings.md) | User preferences and organization-wide AI advisory restrictions. |
| [Logical Database Model](SRS/04-database.md) | Normalized entity-relationship schema across identity, core, and analysis tables. |
| [M1 Physical Database Schema](SRS/04-database-m1-physical.md) | Physical table layouts, constraints, indexes, and migrations for M1. |
| [Error Codes](ERROR_CODES.md) | Stable public error code catalog. |

---

## 3. Security & Governance

| Document | Purpose |
|---|---|
| [RBAC Matrix](RBAC_MATRIX.md) | Multi-tenant permission model for platform, organization, and project scopes. |
| [Security Architecture](SRS/06-security.md) | Trust boundaries, JWT cookie hygiene, rate limiting, and input sanitization. |
| [Threat Model](THREAT_MODEL.md) | STRIDE analysis, threat catalog, controls, and residual risks. |
| [Data Classification](DATA_CLASSIFICATION.md) | Sensitivity tiers, retention rules, and protection controls. |
| [Definition of Ready](DEFINITION_OF_READY.md) | Quality gate required before implementation begins. |
| [Definition of Done](DEFINITION_OF_DONE.md) | Completion criteria for code, testing, and documentation synchronization. |

---

## 4. Development & Testing

| Document | Purpose |
|---|---|
| [Testing & Verification Guide](TESTING.md) | Complete guide for running unit, integration, browser E2E, and smoke tests. |
| [Quality Gates](QUALITY_GATES.md) | Automated CI gates, linters, security scans, and PR checklist. |
| [Local Development](LOCAL_DEVELOPMENT.md) | Environment setup, Docker dependencies, backend, and frontend execution. |
| [Traceability Matrix](SRS/10-traceability.md) | Requirement-to-test and acceptance criteria traceability. |
| [Testing Acceptance](SRS/11-testing-acceptance.md) | Minimum acceptance suites and graduation release criteria. |

---

## 5. Operations & Runbooks

| Document | Purpose |
|---|---|
| [Operations Runbook](OPERATIONS_RUNBOOK.md) | Deployment, health checks, metrics, and logging guidelines. |
| [Incident Response](INCIDENT_RESPONSE.md) | Operational incident handling, severity triage, and post-mortem template. |
| [Backup & Restore Runbook](BACKUP_RESTORE_RUNBOOK.md) | Encrypted database backup, MinIO storage snapshot, and recovery drill procedures. |
| [Release Checklist](RELEASE_CHECKLIST.md) | Production deployment verification checklist. |

---

## 6. Implementation Status & Roadmap

| Document | Purpose |
|---|---|
| [Implementation Status](IMPLEMENTATION_STATUS.md) | Feature matrix reflecting verified implementation state. |
| [Implementation Plan](IMPLEMENTATION_PLAN.md) | Actionable upcoming work and milestone dependencies. |
| [Milestones](MILESTONES.md) | Ordered delivery milestones from M0 to M10. |
