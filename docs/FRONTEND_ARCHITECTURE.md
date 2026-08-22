# Frontend Architecture

## Stack

- Vue 3 Composition API.
- TypeScript with strict mode.
- Vite.
- Vue Router.
- Pinia.
- Axios or equivalent typed API client.
- Cytoscape.js or an approved graph library for dependency visualization.

## Structure target

```text
src/
├── api/                 # typed transport models and clients
├── components/          # reusable presentational components
├── composables/         # reusable stateful behavior
├── features/            # module-specific views/components/store
├── router/
├── stores/              # session and global UI state only
├── types/
├── utils/
└── views/
```

## State rules

- Server state is re-fetchable and must not exist only in Pinia.
- Job progress may use polling and optional SignalR, but REST job state is authoritative.
- Tokens are handled according to the chosen session strategy; never log or expose them in URLs.
- Project context includes organization ID, project ID and current revision.

## Authentication coordination

- Access tokens remain tab/runtime memory only; refresh tokens remain in the server-managed HttpOnly cookie.
- All silent refresh entry points use one coordinator. Same-tab calls share one promise.
- Cross-tab refresh prefers Web Locks and propagates authentication/invalidation with BroadcastChannel.
- If Web Locks are unavailable, the coordinator may use an atomic IndexedDB lease containing only owner/expiry metadata. A non-atomic localStorage lease is not an acceptable mutual-exclusion primitive for refresh rotation.
- Lease ownership is bounded and renewed while a leader is active so a crashed leader cannot deadlock other tabs indefinitely.
- A leader refresh failure is propagated to tabs already waiting on that attempt; waiters do not immediately create a sequential refresh storm.
- Logout/password reset/current-session revocation invalidation must win over an in-flight stale refresh result.

## Required UI states

Every screen must implement:

- Loading skeleton.
- Empty state.
- Permission-denied or masked-not-found state.
- Recoverable error state.
- Degraded AI state when deterministic output remains available.
- Stale/retry state for jobs.

## Performance

- Lazy-load feature routes.
- Virtualize large trees and tables.
- Never render an unbounded dependency graph; apply server/client limits and filters.
- Paginate commit, activity, finding and asset lists.
- Escape repository text and sanitize rendered Markdown/HTML.

## Accessibility

- Keyboard navigation for major workflows.
- Visible focus state.
- Semantic labels for graph controls, forms and status indicators.
- Do not encode severity by color alone.