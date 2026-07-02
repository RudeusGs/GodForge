# Implementation Milestones

This document defines the strict, safe implementation order for GodForge. To ensure foundational stability, no later milestone should be started until its dependencies are met.

## Milestone Order

1. repository skeleton and quality gates;
2. backend solution structure;
3. database baseline;
4. auth/RBAC;
5. project/member management;
6. repository connection and credential references;
7. job infrastructure;
8. clone/fetch worker;
9. parser metadata baseline (server workspace parsing);
10. analyzer/health baseline;
11. scene/asset/dependency read views;
12. Git UI workflows (remote Git sync);
13. scene diff;
14. notifications/activity;
15. observability/deployment hardening.

## Future Milestones
16. Local Agent / Desktop Agent for local uncommitted change tracking.

## Agent Instructions
AI Agents are required to follow this roadmap. Do not jump ahead to implement advanced features before the baseline layers exist to support them.
