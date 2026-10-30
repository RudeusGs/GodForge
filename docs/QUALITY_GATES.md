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

The frontend and browser test packages contain lockfiles. Use `npm ci` to reproduce dependency resolution; dependency updates must update and verify the lockfile together with `package.json`.


```bash
cd GodForge-FE
npm ci --no-audit --no-fund
npm run lint
npm run typecheck
npm run test:unit
npm run build
npm audit --audit-level=high
```

## Local combined gate

From the repository root, run `npm run verify` (Windows PowerShell: `npm.cmd run verify`). This runs local documentation-link checks, isolated PostgreSQL/Redis startup, backend restore/format/build/tests, frontend clean install/lint/typecheck/tests/build, frontend/browser dependency audits at severity High, and Chromium browser tests. Docker Compose removes only the verification project's disposable containers on completion. Ports `15433` and `16379` must be available; development services are not reused.

The documentation checker validates relative Markdown file/directory targets, including reference definitions, excluding fenced code samples. It does not validate remote URLs or heading anchors.

The browser suite starts its own Vite server on `127.0.0.1:15173` and uses controlled API responses without a real account, email provider or development database. It proves browser interaction, routing, storage and multi-tab behavior; the real PostgreSQL HTTP suite independently verifies server-side session and tenancy behavior. A combined browser-to-live-API release workflow remains required.

The local browser-to-live-API workspace and organization collaboration workflows are available separately as `npm.cmd run test:workspace:live` after installing the frontend/browser lockfiles. It starts/builds the fixed disposable `godforge-workspace-acceptance` Compose project with its own ports and volumes, refuses to replace an existing run, installs Chromium and runs `playwright.live.config.ts` (`TC-WORKSPACE-LIVE-001` and `TC-WORKSPACE-LIVE-002`). Cleanup removes only that project's fixtures. It is separate from the controlled-contract gate and is not staging/provider or complete M1 acceptance. [Organization management acceptance evidence](ORGANIZATION_MANAGEMENT_ACCEPTANCE_EVIDENCE.md) and [workspace foundation evidence](WORKSPACE_FOUNDATION_EVIDENCE.md) record execution and remaining gates.

The combined runner also checks backend dependencies and E2E TypeScript. `scripts/check-dotnet-vulnerabilities.mjs` fails on High/Critical findings or an incomplete audit report; the informational `dotnet list package --vulnerable` exit code alone is insufficient. Secret/container scans, clean-checkout CI, production monitoring, performance and backup/restore remain separate gates.

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


## Project acceptance gates — 2026-10-30

Run root npm.cmd run verify, npm.cmd run test:workspace:live, and npm.cmd run test:backend:smoke against their isolated stacks. Live cases TC-PROJECT-LIVE-001–006 preserve both previous workspace/organization cases. Each independent live case resets only auth:rate:* counters in the fixed disposable godforge-workspace-acceptance Redis, guarded by the runner environment marker. Production limits remain unchanged. E2E TypeScript: node ../GodForge-FE/node_modules/typescript/bin/tsc --noEmit --ignoreDeprecations 6.0. [Actual counts, Node 22 isolation attempt, failures and limitations](PROJECT_MANAGEMENT_ACCEPTANCE_EVIDENCE.md). A copied local snapshot does not establish remote clean-checkout CI.

CI runs live workspace acceptance in a separate isolated job, typechecks controlled/live browser tests, checks the root lockfile/audit and rejects focused live tests. Generated Repomix exports, build outputs, browser reports and local verification artifacts are not source deliverables and must remain outside commits.
