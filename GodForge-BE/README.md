# GodForge Backend Husky Setup

This package contains a production-ready Husky setup for the GodForge backend.

## Included files

```txt
.husky/
  _/husky.sh
  pre-commit
  commit-msg
  pre-push
commitlint.config.cjs
lint-staged.config.cjs
package-scripts.patch.md
```

## Install dependencies

Run this inside your backend project:

```bash
npm install -D husky lint-staged @commitlint/cli @commitlint/config-conventional eslint prettier typescript
```

If your backend uses Jest:

```bash
npm install -D jest ts-jest @types/jest
```

## Copy files

Copy the `.husky` folder, `commitlint.config.cjs`, and `lint-staged.config.cjs` into your backend root.

## Update package.json

Open `package-scripts.patch.md` and merge the scripts into your backend `package.json`.

## Enable Husky

Run:

```bash
npm run prepare
```

Then make sure hooks are executable, especially on Linux/macOS:

```bash
chmod +x .husky/pre-commit .husky/commit-msg .husky/pre-push .husky/_/husky.sh
```

## Test

Invalid commit message, expected to fail:

```bash
git commit -m "update code"
```

Valid commit message, expected to pass:

```bash
git commit -m "chore(config): setup backend husky hooks"
```

## Hook policy

- `pre-commit`: runs `lint-staged` on staged files only.
- `commit-msg`: enforces Conventional Commit format.
- `pre-push`: runs full backend validation: lint, format check, typecheck, test, and build.
