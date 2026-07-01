# Package scripts for backend tooling

The backend is an ASP.NET Core .NET 9 codebase. Node.js is used only for Git hooks, Commitlint, lint-staged, and optional Prettier formatting for Markdown/YAML/JSON files.

The tracked `package.json` should keep scripts like these:

```json
{
    "scripts": {
        "prepare": "husky",
        "format:docs": "prettier --write \"**/*.{json,md,yml,yaml}\"",
        "format:docs:check": "prettier --check \"**/*.{json,md,yml,yaml}\"",
        "dotnet:restore": "dotnet restore GodForge.Backend.sln",
        "dotnet:build": "dotnet build GodForge.Backend.sln --no-restore",
        "dotnet:test": "dotnet test GodForge.Backend.sln --no-build",
        "dotnet:format:check": "dotnet format GodForge.Backend.sln --verify-no-changes --no-restore",
        "validate": "npm run dotnet:restore && npm run dotnet:build && npm run dotnet:test && npm run dotnet:format:check"
    }
}
```

Useful backend commands:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
dotnet ef migrations add <Name> --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
dotnet ef database update --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
```

Required Node dev dependencies for hooks:

```bash
npm install -D husky lint-staged prettier @commitlint/cli @commitlint/config-conventional
npm run prepare
```
