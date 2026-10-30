import { spawnSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const backendOnly = process.argv.includes('--backend');
const env = { ...process.env,
  GODFORGE_TEST_POSTGRES: 'Host=localhost;Port=15433;Database=godforge_test;Username=godforge;Password=godforge_verification_only',
  GODFORGE_TEST_REDIS: 'localhost:16379',
};
function run(command, args, directory = '.') {
  console.log(`\n[${directory}] ${command} ${args.join(' ')}`);
  // npm exposes its JavaScript entry point when this runner is invoked by npm.
  // Calling it through Node avoids shell evaluation and Windows .cmd handling.
  const npmEntry = process.env.npm_execpath;
  const isWindowsNpm = process.platform === 'win32' && command === 'npm';
  if (isWindowsNpm && !npmEntry) throw new Error('On Windows, invoke this gate with npm.cmd run verify.');
  const result = spawnSync(isWindowsNpm ? process.execPath : command,
    isWindowsNpm ? [npmEntry, ...args] : args,
    { cwd: resolve(root, directory), env, stdio: 'inherit' });
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`${command} exited with ${result.status}`);
}
const compose = ['compose', '-f', 'docker-compose.test.yml'];
let started = false;
try {
  run(process.execPath, ['scripts/check-doc-links.mjs']);
  started = true;
  run('docker', [...compose, 'up', '-d', '--wait']);
  const backendArtifacts = resolve(root, 'artifacts/verification-backend');
  for (const args of [['restore'], ['format', '--verify-no-changes', '--no-restore'],
    ['build', '--artifacts-path', backendArtifacts], ['test', '--no-build', '--artifacts-path', backendArtifacts]]) run('dotnet', args, 'GodForge-BE');
  run(process.execPath, ['scripts/check-dotnet-vulnerabilities.mjs']);
  if (!backendOnly) {
  run('npm', ['ci', '--no-audit', '--no-fund'], 'GodForge-FE');
  run('npm', ['audit', '--audit-level=high'], 'GodForge-FE');
  for (const gate of ['lint', 'typecheck', 'test:unit', 'build']) run('npm', ['run', gate], 'GodForge-FE');
  run('npm', ['ci', '--no-audit', '--no-fund'], 'GodForge-E2E');
  run('npm', ['audit', '--audit-level=high'], 'GodForge-E2E');
  run(process.execPath, ['../GodForge-FE/node_modules/typescript/bin/tsc', '--noEmit', '--ignoreDeprecations', '6.0'], 'GodForge-E2E');
  run('npm', ['exec', '--', 'playwright', 'install', 'chromium'], 'GodForge-E2E');
  run('npm', ['test'], 'GodForge-E2E');
  }
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
} finally {
  if (started) {
    try { run('docker', [...compose, 'down']); }
    catch (error) { console.error(error.message); process.exitCode = 1; }
  }
}
