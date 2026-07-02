# Seeding Rules

## Development Seeding
- We require a mechanism to seed a local database with realistic test data (Users, Projects, Repositories).
- Seed data scripts should be located in the `Infrastructure` layer.

## Production Seeding
- Only strict system dependencies (e.g., predefined System Roles, global System Settings) should be seeded in production.
- Production seeding must occur automatically during the `OnModelCreating` EF Core configuration or via explicitly controlled idempotent SQL migration scripts.

## Agent Restrictions
- Do not hardcode test users or default passwords directly into production migrations.
- Use explicit `#if DEBUG` or specific Development environment checks when invoking test data seeders.
