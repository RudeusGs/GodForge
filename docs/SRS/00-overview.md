# SRS Overview — GodForge Blueprint

GodForge is a Git-first web platform for managing and analyzing Godot projects. Users link an external repository in the MVP and may create an internally hosted repository through Forgejo in a later phase. Each commit SHA becomes an immutable analysis revision.

The product differentiator is deterministic Godot metadata, health findings, dependency visualization and scene-aware review. Gemini is an optional advisory layer and is never the authoritative source of repository metadata or health score.

Core components: Vue frontend, ASP.NET Core API, PostgreSQL, Redis, RabbitMQ, MinIO, .NET worker and optional Forgejo/Gemini adapters.
