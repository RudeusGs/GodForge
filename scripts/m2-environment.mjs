import { execFileSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { resolve } from 'node:path';

export const m2Project = 'godforge-m2-acceptance';
export const m2Compose = ['compose', '-p', m2Project, '-f', 'docker-compose.backend.yml', '-f', 'docker-compose.hosted-git.yml', '-f', 'docker-compose.m2-test.yml'];
const secret = () => randomBytes(32).toString('hex');
let environmentOwned = false;
export function command(program, args, options = {}) {
  try { return execFileSync(program, args, { cwd: resolve(import.meta.dirname, '..'), encoding: 'utf8', maxBuffer: 8 * 1024 * 1024, ...options }); }
  catch { throw new Error(`Isolated M2 command failed: ${program} ${args.slice(0, 2).join(' ')} (output suppressed to protect bootstrap credentials).`); }
}
export function compose(args, options) { return command('docker', [...m2Compose, ...args], options); }
export async function startProvider() {
  process.env.FORGEJO_DB_PASSWORD = secret();
  process.env.GODFORGE_M2_INSTANCE_SECRET = secret();
  process.env.Forgejo__WebhookSecret = secret();
  process.env.Forgejo__ApiToken = '';
  if (compose(['ps', '-aq']).trim()) throw new Error('M2 test project already exists; refusing to take ownership of it.');
  environmentOwned = true;
  compose(['up', '-d', '--wait', 'forgejo-db', 'forgejo'], { stdio: 'inherit' });
  process.env.GODFORGE_TEST_FORGEJO_URL = 'http://localhost:33000';
  for (const role of ['service', 'developer', 'viewer', 'revoked', 'owner']) {
    const username = `forgejo-${role}`;
    compose(['exec', '-T', 'forgejo', 'forgejo', 'admin', 'user', 'create', '--username', username,
      '--password', `M2!${secret()}`, '--email', `${username}@example.test`, '--must-change-password=false']);
    const output = compose(['exec', '-T', 'forgejo', 'forgejo', 'admin', 'user', 'generate-access-token',
      '--username', username, '--token-name', 'isolated-m2', '--scopes', role === 'service' ? 'write:user,write:repository' : 'read:user,write:repository', '--raw']);
    const token = output.trim().split(/\s+/).at(-1);
    if (!/^[a-f0-9]{40}$/.test(token)) throw new Error('Bootstrap returned an unexpected token format (redacted).');
    process.env[`GODFORGE_TEST_FORGEJO_${role.toUpperCase()}_TOKEN`] = token;
    if (role === 'service') process.env.Forgejo__ApiToken = token;
  }
  const version = await fetch('http://localhost:33000/api/v1/version').then(r => r.json());
  if (!version.version.startsWith('15.0.11')) throw new Error('Unexpected provider runtime version.');
  console.log(`Isolated Forgejo ${version.version} ready; non-admin service and four distinct user accounts provisioned.`);
}

export function stopEnvironment() { if (environmentOwned) { compose(['down', '--volumes'], { stdio: 'inherit' }); environmentOwned = false; } }
