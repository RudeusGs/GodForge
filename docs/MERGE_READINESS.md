# Merge Readiness

Documentation and agent rules may be merged into main when:
- SRS files exist and references are valid.
- .agents skills exist and are populated.
- DoR, milestones, quality gates, RBAC, error codes, database spec, environment rules, and ADRs are present.
- No product source code has been implemented as part of this documentation pass.

After merge:
- READY_FOR_BOOTSTRAP_IMPLEMENTATION: yes
- READY_FOR_FEATURE_IMPLEMENTATION: no

**Blocker**: .NET 9 SDK missing

The local environment is missing the .NET 9 SDK (only .NET 8.0 and .NET 10.0 exist), which causes `dotnet test` and `dotnet ef migrations` to fail.

**Command fail and error output**:
Command: `dotnet test --no-build`
Output:
```text
Testhost process for source(s) '...' exited with error: You must install or update .NET to run this application.
App: D:\GodForge\GodForge-BE\tests\GodForge.UnitTests\bin\Debug\net9.0\testhost.exe
Architecture: x64
Framework: 'Microsoft.AspNetCore.App', version '9.0.0' (x64)
.NET location: C:\Program Files\dotnet
The following frameworks were found:
  8.0.26 at [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
  10.0.6 at [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
```

**Resolution steps**:
Install the .NET 9 SDK and ASP.NET Core 9.0 Runtime, then re-run all the backend quality gates.
