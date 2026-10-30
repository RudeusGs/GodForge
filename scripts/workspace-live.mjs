import { spawnSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const compose = ['compose', '-p', 'godforge-workspace-acceptance', '-f', 'docker-compose.backend.yml', '-f', 'docker-compose.workspace-test.yml'];
function run(command, args, cwd = root) {
  const npmEntry = process.env.npm_execpath;
  const windowsNpm = process.platform === 'win32' && command === 'npm';
  if (windowsNpm && !npmEntry) throw new Error('Run through npm.cmd run test:workspace:live on Windows.');
  const result = spawnSync(windowsNpm ? process.execPath : command, windowsNpm ? [npmEntry, ...args] : args, { cwd, stdio: 'inherit', env: process.env });
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`${command} exited with ${result.status}`);
}
let started = false;
try {
  // Never tear down another invocation's environment.
  const existing = spawnSync('docker', [...compose, 'ps', '-aq'], { cwd: root, encoding: 'utf8' });
  if (existing.error || existing.status !== 0) throw new Error('Docker Compose is unavailable.');
  if (existing.stdout.trim()) throw new Error('Workspace acceptance project is already running. Stop that run before starting another.');
  started = true;
  run('docker', [...compose, 'up', '-d', '--build', '--wait']);
  run('npm', ['exec', '--', 'playwright', 'install', 'chromium'], resolve(root, 'GodForge-E2E'));
  process.env.GODFORGE_WORKSPACE_ACCEPTANCE = 'isolated-compose';
  run('npm', ['test', '--', '--config', 'playwright.live.config.ts'], resolve(root, 'GodForge-E2E'));
} catch (error) {
  console.error(error.message); process.exitCode = 1;
} finally {
  // This fixed project owns only disposable local fixtures and project-scoped volumes.
  if (started) {
    try { run('docker', [...compose, 'down', '--volumes']); }
    catch (error) { console.error(error.message); process.exitCode = 1; }
  }
}
