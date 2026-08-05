# 0. Overview

## 0.1 Product

GodForge is a production-oriented Git and project-intelligence platform specialized for Godot Engine. It supports hosted and linked repositories, deterministic project analysis, dependency visualization, AI advisory and separately permissioned assets.

## 0.2 Objectives

- Make complex Godot projects understandable through structured metadata and graphs.
- Detect missing references, maintainability problems, security risks and waste.
- Support team repository workflows without reimplementing Git protocols.
- Allow source visibility and asset visibility to be controlled independently.
- Demonstrate enterprise engineering through security, async processing, observability, recovery and measurable performance.

## 0.3 Stakeholders

- Developer, Reviewer, Project Maintainer and Project Owner.
- Organization Owner/Admin.
- System Administrator and operations team.
- Graduation-project supervisor and evaluation committee.
- External providers: Forgejo, linked Git providers, Gemini, SMTP and object storage.

## 0.4 System context

```text
User -> Vue Web UI -> ASP.NET Core API
                         |  |  |  |  |
                    PostgreSQL Redis RabbitMQ MinIO Forgejo
                                      |
                                      v
                               GodForge Worker -> Gemini
```

## 0.5 Core invariants

1. Git is source-code truth; PostgreSQL is GodForge business-state truth.
2. Analysis is attached to an immutable commit SHA.
3. Parser/rule-engine output is authoritative; AI output is advisory.
4. Heavy work is asynchronous and durable.
5. Repository content is untrusted and is not executed.
6. Every resource is authorized by organization/project scope.
7. Private assets are not committed to a public repository history.
