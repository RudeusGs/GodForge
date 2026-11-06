# Quality Gates

This document defines the mandatory engineering quality gates required for any change submitted to GodForge.

---

## 1. Documentation Gate

- All Markdown links must resolve to valid files and directories (verified via `node scripts/check-doc-links.mjs`).
- Requirement IDs (`FR-*`), acceptance IDs (`AC-*`), and threat IDs (`T-*`) must be unique and traceable.
- Features must keep SRS, API contracts, database specifications, security models, and traceability synchronized.
- Implementation status in `docs/IMPLEMENTATION_STATUS.md` must reflect actual verified code, not target designs.

---

## 2. Backend Gate

Execute from `GodForge-BE/`:

```bash
dotnet restore --locked-mode
dotnet format --verify-no-changes
dotnet build --no-restore
dotnet test --no-build
```

Mandatory checks:
- Clean Architecture boundaries: Domain has 0 dependencies; Application depends only on Domain; Infrastructure implements interfaces; Api and Worker orchestrate.
- All migrations apply cleanly to a clean PostgreSQL instance and upgrade from prior schema versions.
- OpenAPI specification generation succeeds via Swagger provider.
- `dotnet list package --vulnerable --include-transitive` reports 0 High or Critical vulnerabilities.
- Target runtime: .NET 10 LTS (`net10.0`), resolved via `global.json`.

---

## 3. Frontend Gate

Execute from `GodForge-FE/`:

```bash
npm ci --no-audit --no-fund
npm run lint
npm run typecheck
npm run test:unit
npm run build
npm audit --audit-level=high
```

Mandatory checks:
- ESLint passes with zero errors.
- Strict TypeScript typechecking (`vue-tsc` and `tsc`) passes with zero errors.
- Vitest unit tests pass with zero failures.
- Production bundle build completes without unresolved imports.
- Zero High or Critical npm security vulnerabilities.

---

## 4. API & Database Change Checklist

### 4.1 API Endpoints
- [ ] Requirement ID and actor permission verified from `docs/RBAC_MATRIX.md`.
- [ ] Route uses standard `/api/v1` prefix and conforms to standard envelopes `{ "data": ..., "meta": ... }`.
- [ ] Request DTOs prevent over-posting and enforce input validation via FluentValidation.
- [ ] Permissions evaluated in Application layer, not solely by controller attributes.
- [ ] Stable error codes used from `docs/ERROR_CODES.md`.
- [ ] Asynchronous or heavy operations return `202 Accepted` with a durable job summary.
- [ ] Audit logs and structured activity events emitted on state mutation.

### 4.2 Database Changes
- [ ] Table, column, constraint, and index documented in `docs/SRS/04-database.md`.
- [ ] Tenant ownership path (`organization_id`, `project_id`) is explicit.
- [ ] Migrations are forward-only and tested on both fresh and prior schemas.
- [ ] Large binary assets and files are stored in MinIO, never in PostgreSQL.
- [ ] Concurrency tokens (`version`) and uniqueness constraints prevent race conditions.

---

## 5. Security & Isolation Gate

- **Tenant Isolation:** Cross-tenant access attempts return masked `404` responses to prevent resource enumeration.
- **Secret Scanning:** Gitleaks scan passes with zero committed credentials or tokens.
- **Container Scanning:** Trivy scans of API and Worker runtime images report zero High or Critical CVEs.
- **Input Validation:** Symlinks, path traversal (`../`), and hostile file paths in Godot repositories are rejected.
- **Repository Safety:** Untrusted repository code or Godot scripts must never be executed on host machines or workers.

---

## 6. End-to-End & Acceptance Gate

- **Controlled Browser Tests:** `npm test` in `GodForge-E2E` executes Playwright tests against Vite app with mock contracts.
- **Live Browser Acceptance:** `npm.cmd run test:workspace:live` builds runtime images, boots isolated services (PostgreSQL, Redis, RabbitMQ, Mailpit), and executes end-to-end browser workflows without HTTP interception.
- **Runtime Smoke:** `npm.cmd run test:backend:smoke` verifies real HTTP endpoints, OTP emails, outbox dispatching, and session lifecycles against `docker-compose.backend.yml`.
- See [Testing and Verification Guide](TESTING.md) for full commands and test catalog.

---

## 7. Merge & Release Gate

A pull request is ready to merge only when:
- [ ] Scope and requirement IDs are stated.
- [ ] Build, format, unit, integration, and browser tests pass.
- [ ] API contracts, error codes, and RBAC matrix remain synchronized.
- [ ] No secrets, generated artifacts, or temporary logs are committed.
- [ ] Asynchronous workflows are idempotent and cancellation-aware.
- [ ] Documentation and `docs/IMPLEMENTATION_STATUS.md` reflect verified behavior.
- [ ] Backup and restore procedures in `docs/BACKUP_RESTORE_RUNBOOK.md` are verified for production releases.
