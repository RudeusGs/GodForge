# Database Change Checklist

- [ ] Requirement and ownership/tenant scope identified.
- [ ] Table/column/constraint/index documented.
- [ ] Data classification and retention defined.
- [ ] Migration is forward-only and tested on clean/upgrade database.
- [ ] Backfill is bounded, resumable and observable when needed.
- [ ] Query projections avoid N+1 and unbounded reads.
- [ ] Unique/idempotency constraint matches business identity.
- [ ] Object bytes remain outside PostgreSQL.
- [ ] Rollout compatibility and forward-fix path documented.
