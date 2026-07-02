# Merge Readiness

Documentation and agent rules may be merged into main when:
- SRS files exist and references are valid.
- .agents skills exist and are populated.
- DoR, milestones, quality gates, RBAC, error codes, database spec, environment rules, and ADRs are present.
- No product source code has been implemented as part of this documentation pass.

After merge:
- READY_FOR_BOOTSTRAP_IMPLEMENTATION: yes
- READY_FOR_FEATURE_IMPLEMENTATION: no
