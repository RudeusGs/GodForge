# CI/CD Quality Gates

All pull requests and commits to the `main` branch must pass the following quality gates before merging. AI Agents MUST verify these gates pass locally before declaring a feature complete.

## 1. Backend Build & Linting
- `dotnet build` must compile with ZERO warnings. Treat warnings as errors.
- Code must comply with standard .NET analyzer rules (e.g. IDE0005, CS8600).

## 2. Backend Tests
- `dotnet test` must execute all unit and integration tests successfully.
- Code coverage must not drop below the baseline (minimum 80% for Application layer).

## 3. Frontend Build & Linting
- `npm run lint` (ESLint) must pass with zero errors.
- `npm run typecheck` (Vue TSC) must pass with zero type errors.
- `npm run build` must successfully produce a production build.

## 4. Frontend Tests
- `npm run test:unit` (Vitest) must pass.

## 5. Security & Dependencies
- `dotnet list package --vulnerable` must return clean.
- `npm audit` must return 0 critical vulnerabilities.
