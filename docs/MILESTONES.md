# Implementation Milestones

This document defines the strict, safe implementation order for GodForge. To ensure foundational stability, no later milestone should be started until its dependencies are met.

## Milestone Order

1. repository skeleton and quality gates;
2. backend solution structure;
3. database baseline;
4. auth/RBAC;
5. project/member management;
6. repository integration for analysis snapshots;
7. job infrastructure;
8. parser metadata baseline;
9. analyzer/health baseline;
10. scene/asset/dependency read views;
11. scene diff between snapshots;
12. dashboard and activity/notifications;
13. observability/deployment hardening.

## Future Milestones
14. optional post-MVP local companion/desktop agent for local uncommitted change tracking.

## Agent Instructions
AI Agents are required to follow this roadmap. Do not jump ahead to implement advanced features before the baseline layers exist to support them.
