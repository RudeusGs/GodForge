# Public Landing Experience

## Purpose

Present GodForge's documented product purpose and provide a clear public entry point to authentication without exposing tenant or repository data.

## Actors

Anonymous visitor and authenticated user. This is a Core public UI route with no new backend capability.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| `FR-00.1` | Public product landing page with authentication and workspace entry actions | Must |

## Main flow

1. A visitor opens `/` and sees the GodForge product statement, primary differentiators and bounded workflow summary.
2. Anonymous visitors can navigate to registration or login.
3. Authenticated users can navigate to their dashboard.
4. Decorative 3D presentation responds to fine-pointer input while remaining non-essential to navigation and content.

## Error and edge cases

- JavaScript or route chunks are still loading: the branded application loader remains visible.
- Reduced-motion preference, coarse pointer or narrow viewport: the experience remains complete without pointer-driven 3D motion.
- Authentication restoration fails: the landing page remains public and login/register actions remain available.

## Authorization and security

- Authentication state is used only to choose between public auth actions and the dashboard action; server authorization remains authoritative.
- No tokens, credentials, tenant data or route query values are logged or displayed.
- Product claims must remain consistent with `PRODUCT_VISION.md` and must not imply that optional AI is authoritative.
- Pointer effects are requestAnimationFrame-throttled, listeners are removed on unmount, and `prefers-reduced-motion` disables non-essential motion.

## Async processing and idempotency

- Vue route is lazy-loaded and contains bounded static product content.
- No landing-specific API, background job, durable message, idempotency or concurrency behavior is introduced.

## Acceptance criteria

- `AC-FR-00.1-01`: `/` renders the documented GodForge positioning and working login/register calls to action for an anonymous visitor.
- `AC-FR-00.1-02`: An authenticated user receives a dashboard call to action without the landing page treating client state as authorization evidence.
- `AC-FR-00.1-03`: Essential content and navigation remain available at mobile widths and with reduced motion enabled.
- `AC-FR-00.1-04`: The 3D scene is bounded static markup, performs no continuous canvas rendering and cleans up pointer listeners on unmount.

## Related API

None. Landing navigation targets existing frontend routes only.

## Related data

None. The page does not read database, cache, object storage, tenant or repository data.

## Tests and observability

- `TC-LANDING-001`: anonymous calls to action and product content.
- `TC-LANDING-002`: authenticated dashboard action.
- `TC-LANDING-003`: semantic landmarks and decorative-scene accessibility isolation.
- Build, typecheck, lint and production bundle checks cover route integration.
- No landing-specific logs, metrics or alerts are required; normal frontend availability monitoring applies.
