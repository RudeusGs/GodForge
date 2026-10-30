# 11. Testing and Acceptance

## Test levels

### Unit

- Domain invariants and state transitions.
- Application authorization and use-case decisions.
- Parser/rule functions with deterministic fixtures.
- Input hash, score and policy calculations.

### Integration

- API with real PostgreSQL test environment.
- EF mappings, constraints, migrations and query projection.
- RabbitMQ/Redis/MinIO/Forgejo adapters through controlled containers where practical.
- Webhook signature and outbox/inbox behavior.

### End-to-end

- Workspace controlled contracts: `TC-WORKSPACE-BROWSER-001`–`008` in `GodForge-E2E/tests/workspace.spec.ts` exercise onboarding, project creation/detail/reload, organization selection, role-based creation, catalog/content separation, network retry, concurrency conflicts, masked detail, mobile navigation, member role management and removal, invitation creation/revocation/acceptance with URL token scrubbing, and settings update/deletion request/permission gating. Existing auth cases (`TC-AUTH-BROWSER-001`–`011`) still run (19 controlled browser tests total).
- Live workspace: `npm.cmd run test:workspace:live` at repository root builds a fresh isolated Compose project, runs `TC-WORKSPACE-LIVE-001` and `TC-WORKSPACE-LIVE-002` without HTTP interception, then removes its containers and fixture volumes even on failure. Registration and invitation fixture flows use real OTP/outbox/Worker/local SMTP (Mailpit); browser steps exercise real login, organization/project creation, detail reload, assigned-project list, account sessions, multi-user invitation delivery and acceptance, member role promotion, general settings update, candidate search and ownership transfer, server-enforced `LAST_OWNER_REQUIRED` on sole-owner self-leave, member removal with immediate access revocation, and organization deletion request. Ports 25072 (API), 25173 (Vite), 28025 (mailbox) are loopback-only. It supplements controlled contracts and PostgreSQL authorization suites; external provider reconciliation (`FR-27.2`), project membership UI (`FR-03.1`), and staging acceptance remain outstanding. See [acceptance evidence](../ORGANIZATION_MANAGEMENT_ACCEPTANCE_EVIDENCE.md) and [workspace foundation evidence](../WORKSPACE_FOUNDATION_EVIDENCE.md).

- M1 browser interaction suite: run `npm test` in `GodForge-E2E`. Chromium executes the actual Vue app against controlled HTTP contracts, covering login, refresh restoration, logout success/failure, cross-tab invalidation, session revoke/retry, reset-link scrubbing and bootstrap outage followed by definitive 401. No mandatory browser cases in this suite are skipped. Server transaction/security behavior remains covered separately by real PostgreSQL HTTP integration tests.

- Hosted repository push to displayed analysis.
- Linked repository sync to displayed analysis.
- Finding remediation across revisions.
- Protected asset upload/manifest/hydration.
- Report export/download.

### Security

Use `../SECURITY_TEST_PLAN.md` and the threat model. Cross-tenant authorization tests are mandatory for every project route.

### Performance and reliability

Use `../PERFORMANCE_TEST_PLAN.md`; test duplicate delivery, provider outage, retry, timeout, cancellation, worker restart and restore.

## Test case format

```text
ID
Requirement IDs
Preconditions and fixture hash
Actor/role
Steps
Expected API/data/job/audit result
Cleanup
```

## Minimum acceptance suites

| Suite | Minimum evidence |
|---|---|
| TC-LANDING | anonymous/authenticated calls to action, product positioning, semantic landmarks and bounded decorative 3D isolation |
| TC-AUTH | registration, login, refresh replay, logout, reset, rate limit |
| TC-TENANT | cross-org/project isolation and owner-transfer rules |
| TC-REPO | linked/hosted provisioning, sync, commit/tree/blob limits |
| TC-WEBHOOK | signature, replay, duplicate event, identity mismatch |
| TC-VALID | path, symlink, secret, size/count and marker corpus |
| TC-PARSER | supported corpus, malformed partial input, reproducibility |
| TC-GRAPH | canonical graph, cycles, missing edges, bounded query |
| TC-HEALTH | stable findings/score/suppression and rule versions |
| TC-INCR | incremental/full equivalence and fallback reasons |
| TC-AI | redaction, prompt injection, schema failure and degraded mode |
| TC-VAULT | visibility matrix, upload validation, signed URL and revocation |
| TC-JOB | duplicate, retry, DLQ, cancellation, timeout, stale heartbeat |
| TC-REPORT | provenance, authorization and artifact expiry |
| TC-BACKUP | restore controlled tenant and verify critical flows |

## Graduation release acceptance

- All Core Must requirements pass.
- No unresolved Critical/High security issue.
- Production-like deployment and monitoring operate.
- Backup/restore drill succeeds.
- Parser and incremental-analysis experiments are reproducible.
- AI remains optional and safe under failure.
- Complete demonstration workflow in `08-workflows.md` succeeds.


## Project acceptance extension — 2026-10-30

TC-PROJECT-BROWSER-001/002 cover member paging/search/confirmation, active navigation, retained conflict edits and restore. TC-PROJECT-LIVE-001–006 cover real assignment after Worker/Mailpit invitations, roles/escalation/removal, versioned ownership, archive/restore without reactivation, metadata/settings/deleting status, and multi-tenant/revocation behavior. Both prior workspace/organization live cases remain.

ProjectLifecycleHttpTests verifies persisted rows, versions and audit counts for eligibility/grants, optional-key replay, settings/role/add/ownership/removal/lifecycle concurrency, archive races, owner preservation and scale pagination. Revocation races observe the PostgreSQL advisory-lock wait before committing revocation. Frontend tests cover independent versions, duplicate submission, unsafe text, candidate paging, self-leave and late completion after switch/unmount. [Counts, commands and remaining M1 acceptance](../PROJECT_MANAGEMENT_ACCEPTANCE_EVIDENCE.md). Local Chromium does not establish all-browser/staging/release acceptance.
