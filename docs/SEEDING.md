# Seeding Rules

## Development

Development seed data may create:

- One system administrator with a non-production credential supplied through environment configuration.
- Sample organization, projects and memberships.
- A small public Godot sample repository reference.
- Versioned health rules and analysis profiles.

## Production

- Do not create default users or passwords.
- Seed only immutable reference data such as permission definitions, rule metadata and supported parser profiles.
- Every production seed operation is idempotent and recorded.

## Restrictions

- Never seed real API keys, repository credentials or private assets.
- Never depend on seed order outside explicit versioned migrations.
