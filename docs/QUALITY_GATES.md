# Quality Gates

## Documentation gate

- Markdown links resolve.
- Requirement IDs are unique.
- New features update SRS, API, database, security, traceability and tests.
- Current status is not confused with target design.

## Backend gate

```bash
cd GodForge-BE
dotnet restore
dotnet format --verify-no-changes
dotnet build --no-restore
dotnet test --no-build
```

Additional gates:

- Clean Architecture dependency test.
- Migration applies to a clean PostgreSQL database.
- OpenAPI document generation succeeds.
- No high-severity dependency or secret scan finding.
- The SDK is resolved from `GodForge-BE/global.json`; CI and local builds use the same version.

## Frontend gate

The current snapshot does not contain an npm lockfile. Direct dependency versions are pinned in `package.json`; use the command below until a lockfile is generated and committed from a network-enabled environment.


```bash
cd GodForge-FE
npm install --no-audit --no-fund
npm run lint
npm run typecheck
npm run test:unit
npm run build
```

## Security gate

- Cross-tenant authorization tests.
- SSRF, path traversal and symlink tests.
- File and repository quota tests.
- Webhook signature/replay tests.
- AI redaction and prompt-injection tests.
- Asset signed-URL authorization tests.
- No Critical or High unresolved finding.

## Worker gate

- Duplicate delivery test.
- Retry and backoff test.
- Timeout/cancellation cleanup test.
- DLQ test.
- Lock owner-token test.
- Outbox/inbox reliability test.

## Release gate

- Backup and restore drill passed.
- Observability dashboard and alerts verified.
- Production configuration contains no defaults.
- Performance targets in `SRS/07-non-functional.md` met.
- `MERGE_READINESS.md` and `RELEASE_CHECKLIST.md` completed.