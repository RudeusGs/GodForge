# CI/CD Quality Gates

All pull requests and commits to the `main` branch must pass the following quality gates before merging. AI Agents MUST verify these gates pass locally before declaring a feature complete.

## 1. Backend Build & Linting
- `cd GodForge-BE && dotnet restore`
- `cd GodForge-BE && dotnet build --no-restore` must compile with ZERO warnings. Treat warnings as errors.
- Code must comply with standard .NET analyzer rules (e.g. IDE0005, CS8600).
- `cd GodForge-BE && dotnet format --verify-no-changes`

## 2. Backend Tests
- `cd GodForge-BE && dotnet test --no-build` must execute all unit and integration tests successfully.
- Code coverage must not drop below the baseline (minimum 80% for Application layer).

## 3. Frontend Build & Linting
- `cd GodForge-FE && npm ci`
- `cd GodForge-FE && npm run lint` (ESLint) must pass with zero errors.
- `cd GodForge-FE && npm run typecheck` (Vue TSC) must pass with zero type errors.
- `cd GodForge-FE && npm run build` must successfully produce a production build.

## 4. Frontend Tests
- `cd GodForge-FE && npm run test:unit` (Vitest) must pass.

## 5. Security & Dependencies
- `cd GodForge-BE && dotnet list package --vulnerable` must return clean.
- `cd GodForge-FE && npm audit --audit-level=critical` must return 0 critical vulnerabilities.

If a command cannot run because the skeleton is not created yet, the agent must report it as "not runnable yet" and explain which milestone will make it runnable. It must not claim the gate passed.
