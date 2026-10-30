# GodForge Web UI

Vue 3 and TypeScript frontend for GodForge. Use Node 22 and install the locked dependencies with `npm ci`.

Run `npm run dev` for development. Quality checks are `npm run lint`, `npm run typecheck`, `npm run test:unit` and `npm run build`.

Run `npm run verify` from the repository root for the combined backend/frontend/controlled-browser gate. Run `npm run test:workspace:live` there for isolated real-service browser acceptance.

See [local development](../docs/LOCAL_DEVELOPMENT.md), [frontend architecture](../docs/FRONTEND_ARCHITECTURE.md) and [quality gates](../docs/QUALITY_GATES.md).
