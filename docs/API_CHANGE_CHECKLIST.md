# API Change Checklist

- [ ] Requirement and actor identified.
- [ ] Route/method does not conflict with `SRS/05-api.md`.
- [ ] Request DTO prevents over-posting.
- [ ] Validator and size/count limits defined.
- [ ] Permission checked in Application layer.
- [ ] Response envelope and status correct.
- [ ] Stable error codes added/used.
- [ ] Pagination/idempotency/rate limit defined.
- [ ] Async work returns 202 and durable job.
- [ ] Activity/audit event defined.
- [ ] OpenAPI and integration tests updated.
