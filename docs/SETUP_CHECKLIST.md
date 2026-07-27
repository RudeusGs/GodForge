# Setup Checklist — Blueprint

- [ ] `.env` created from `.env.example`; no secrets committed.
- [ ] `docker compose config` succeeds.
- [ ] PostgreSQL, Redis, RabbitMQ and MinIO are healthy.
- [ ] Frontend `package-lock.json` generated and committed.
- [ ] `dotnet restore`, build, test and format checks pass with zero warnings.
- [ ] EF initial/forward migration is generated, reviewed and applied.
- [ ] API `/health` returns 200.
- [ ] Worker consumes `repository.pipeline`.
- [ ] Public fixture repository can be linked and analyzed.
- [ ] Job reaches `completed`, `retrying` or `dead_lettered` consistently.
- [ ] `.env`/private-key fixture does not appear in generated AI context.
- [ ] Gemini disabled mode completes deterministic output.
- [ ] Removed project member cannot read repository/jobs.
- [ ] Forgejo profile remains disabled until signed webhook and permission-sync tests pass.
