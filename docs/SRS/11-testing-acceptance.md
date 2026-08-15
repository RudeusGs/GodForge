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
