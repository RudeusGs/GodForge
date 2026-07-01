# Package scripts to add/update in backend/package.json

Add these scripts to your backend `package.json`.

```json
{
  "scripts": {
    "prepare": "husky",
    "lint": "eslint \"src/**/*.ts\"",
    "lint:fix": "eslint \"src/**/*.ts\" --fix",
    "format": "prettier --write \"src/**/*.{ts,json}\" \"test/**/*.{ts,json}\"",
    "format:check": "prettier --check \"src/**/*.{ts,json}\" \"test/**/*.{ts,json}\"",
    "typecheck": "tsc --noEmit",
    "test": "jest",
    "test:unit": "jest --passWithNoTests",
    "test:ci": "jest --ci --runInBand --passWithNoTests",
    "build": "tsc -p tsconfig.json",
    "validate": "npm run lint && npm run format:check && npm run typecheck && npm run test:ci && npm run build"
  }
}
```

For NestJS, replace `build` with:

```json
{
  "build": "nest build"
}
```
