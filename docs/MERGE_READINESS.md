# Merge Readiness

# Merge Readiness

Documentation and agent rules may be merged into main when:
- SRS files exist and references are valid.
- .agents skills exist and are populated.
- DoR, milestones, quality gates, RBAC, error codes, database spec, environment rules, and ADRs are present.
- No product source code has been implemented as part of this documentation pass.

After merge:
- READY_FOR_BOOTSTRAP_IMPLEMENTATION: yes
- READY_FOR_FEATURE_IMPLEMENTATION: no

**Blocker**: PostgreSQL Authentication Error `28P01`

The local environment can build and pass tests, but running `dotnet ef database update` fails with a PostgreSQL password authentication error. (Note: The previous `.NET 9 SDK missing` blocker has been resolved, and the `SearchVector` mapping issue was fixed).

**Command fail and error output**:
Command: `dotnet ef database update --project GodForge-BE/src/GodForge.Infrastructure --startup-project GodForge-BE/src/GodForge.Api`
Output:
```text
fail: Microsoft.EntityFrameworkCore.Database.Connection[20004]
      An error occurred using the connection to database 'godforge' on server 'tcp://localhost:5432'.
Npgsql.PostgresException (0x80004005): 28P01: password authentication failed for user "postgres"
```

**Resolution steps**:
Ensure the local PostgreSQL instance is running with the correct credentials defined in `appsettings.json` or `.env` and retry the EF migrations.
