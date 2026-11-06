import assert from 'node:assert/strict';
import { randomUUID, createHmac } from 'node:crypto';
import { mkdtempSync, writeFileSync, mkdirSync, rmSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { startProvider, stopEnvironment, command, compose } from './m2-environment.mjs';
import { configureLinkedFixture } from './m2-linked-fixture.mjs';

const api = 'http://127.0.0.1:35072/api/v1';
const provider = 'http://localhost:33000/api/v1';
const mailbox = 'http://127.0.0.1:38025';
const suffix = randomUUID().replaceAll('-', '').slice(0, 16);
const password = `M2-Acceptance!-${suffix}`;
let assertions = 0;
export async function eventually(label, operation, timeout = 60000) {
  const deadline = Date.now() + timeout; let error;
  while (Date.now() < deadline) {
    try { const value = await operation(); if (value) return value; } catch (cause) { error = cause; }
    await new Promise(resolve => setTimeout(resolve, 250));
  }
  throw new Error(`Timed out: ${label}${error ? ` (${error.message})` : ''}`);
}
async function http(method, path, status, data, actor) {
  const response = await fetch(api + path, { method, headers: { 'Content-Type': 'application/json', ...(actor ? { Authorization: `Bearer ${actor.token}` } : {}) },
    body: data === undefined ? undefined : JSON.stringify(data), signal: AbortSignal.timeout(15000) });
  const body = response.status === 204 ? {} : await response.json();
  assert.equal(response.status, status, `${method} ${path}: ${body.error?.code ?? response.status}`); assertions++;
  return body.data;
}
async function providerRequest(method, path, data, token = process.env.GODFORGE_TEST_FORGEJO_SERVICE_TOKEN) {
  const response = await fetch(provider + path, { method, headers: { 'Content-Type': 'application/json', Authorization: `token ${token}` },
    body: data === undefined ? undefined : JSON.stringify(data), signal: AbortSignal.timeout(15000) });
  assert.ok(response.ok, `Provider ${method} ${path}: ${response.status}`); assertions++;
  return response.status === 204 ? undefined : response.json();
}
async function email(address, subject) {
  return eventually(`Mailpit ${subject}`, async () => {
    const list = await fetch(mailbox + '/api/v1/messages?limit=100').then(r => r.json());
    const message = list.messages.find(m => m.Subject === subject && m.To.some(t => t.Address === address));
    return message ? fetch(mailbox + `/api/v1/message/${message.ID}`).then(r => r.json()) : undefined;
  });
}
async function register(label) {
  // Four isolated identities share one NAT address. Reset only this disposable
  // stack's auth limiter between fixture registrations, as workspace-live does.
  compose(['exec', '-T', 'redis', 'redis-cli', 'EVAL', "for _, key in ipairs(redis.call('KEYS', 'auth:rate:*')) do redis.call('DEL', key) end return 1", '0']);
  const address = `${label}-${suffix}@example.test`;
  await http('POST', '/auth/send-otp', 202, { email: address });
  const delivered = await email(address, 'GodForge - Email verification');
  const otp = delivered.HTML.match(/<strong>(\d{6})<\/strong>/)?.[1]; assert.ok(otp);
  await http('POST', '/auth/register', 201, { email: address, password, displayName: label, otp });
  const login = await http('POST', '/auth/login', 200, { email: address, password, deviceName: 'Isolated M2' });
  return { email: address, token: login.accessToken, id: login.user.id };
}
function sql(statement, service = 'postgres') {
  return compose(['exec', '-T', service, 'psql', '-U', service === 'postgres' ? 'godforge' : 'forgejo', '-d', service === 'postgres' ? 'godforge' : 'forgejo', '-tA', '-c', statement]).trim();
}
function git(directory, role, args, expected = 0) {
  const env = { ...process.env, GIT_TERMINAL_PROMPT: '0', GIT_CONFIG_NOSYSTEM: '1', GIT_CONFIG_GLOBAL: process.platform === 'win32' ? 'NUL' : '/dev/null',
    GIT_CONFIG_COUNT: '4', GIT_CONFIG_KEY_0: 'http.extraHeader', GIT_CONFIG_VALUE_0: 'Authorization: Basic ' + Buffer.from(`forgejo-${role}:${process.env[`GODFORGE_TEST_FORGEJO_${role.toUpperCase()}_TOKEN`]}`).toString('base64'),
    GIT_CONFIG_KEY_1: 'http.followRedirects', GIT_CONFIG_VALUE_1: 'false', GIT_CONFIG_KEY_2: 'credential.helper', GIT_CONFIG_VALUE_2: '',
    GIT_CONFIG_KEY_3: 'core.hooksPath', GIT_CONFIG_VALUE_3: join(directory, 'disabled-hooks') };
  try { const output = command('git', args, { cwd: directory, env, timeout: 60000 }); assert.equal(expected, 0, 'Forbidden Git operation unexpectedly succeeded'); assertions++; return output.trim(); }
  catch (error) { if (expected !== 0 && !error.message.includes('unexpectedly succeeded')) { assertions++; return ''; } throw error; }
}
async function confirmed(projectId, actor) {
  return eventually('current provider permission generation confirmed', async () => {
    const repo = await http('GET', `/projects/${projectId}/repository`, 200, undefined, actor);
    return !repo.accessErrorCode && repo.accessGeneration > 0 && repo.accessConfirmedGeneration === repo.accessGeneration ? repo : undefined;
  });
}
async function operation(projectId, jobId, actor) {
  return eventually('durable job completion', async () => {
    const job = await http('GET', `/projects/${projectId}/jobs/${jobId}`, 200, undefined, actor);
    if (['DeadLettered', 'Failed'].includes(job.status)) throw new Error(job.errorCode);
    return job.status === 'Completed' ? job : undefined;
  });
}
let owned = false;
const workspace = mkdtempSync(join(tmpdir(), 'godforge-m2-acceptance-'));
try {
  await startProvider(); owned = true;
  const tls = join(workspace, 'tls'); mkdirSync(tls); configureLinkedFixture(tls);
  command('dotnet', ['test', 'GodForge-BE/tests/GodForge.IntegrationTests', '--filter', 'FullyQualifiedName~RealForgejoTests', '--logger', 'console;verbosity=minimal'], { stdio: 'inherit' });
  compose(['up', '-d', '--build', '--wait', 'api', 'worker', 'linked-tls'], { stdio: 'inherit' });
  const openapi = await fetch('http://127.0.0.1:35072/swagger/v1/swagger.json').then(r => r.json());
  for (const route of ['/api/v1/projects/{projectId}/repository/hosted', '/api/v1/projects/{projectId}/repository/sync',
    '/api/v1/projects/{projectId}/jobs/{jobId}', '/api/v1/webhooks/forgejo']) assert.ok(openapi.paths[route], `OpenAPI route missing: ${route}`);
  const owner = await register('owner'); const developer = await register('developer'); const viewer = await register('viewer'); const revoked = await register('revoked');
  const org = await http('POST', '/organizations', 201, { name: `M2 ${suffix}`, slug: `m2-${suffix}` }, owner);
  for (const [actor, role] of [[owner, 'owner'], [developer, 'developer'], [viewer, 'viewer'], [revoked, 'revoked']]) {
    if (actor !== owner) {
    await http('POST', `/organizations/${org.id}/invitations`, 201, { email: actor.email, role: 'organizationMember' }, owner);
    const invitation = await email(actor.email, 'GodForge organization invitation');
    const link = invitation.HTML.match(/href="([^"]+)"/)?.[1]; assert.ok(link);
    await http('POST', '/organization-invitations/accept', 200, { token: new URL(link.replaceAll('&amp;', '&')).searchParams.get('token') }, actor);
    }
    await http('POST', '/users/me/forgejo-identity', 200, { personalToken: process.env[`GODFORGE_TEST_FORGEJO_${role.toUpperCase()}_TOKEN`] }, actor);
  }
  const project = await http('POST', `/organizations/${org.id}/projects`, 201, { name: `Godot ${suffix}`, slug: `godot-${suffix}`, visibility: 'private' }, owner);
  const prefix = `/projects/${project.id}`;
  for (const [actor, role] of [[developer, 'developer'], [viewer, 'viewer'], [revoked, 'developer']])
    await http('POST', `${prefix}/members`, 201, { userId: actor.id, role }, owner);
  const request = { name: 'godot-fixture', defaultBranch: 'main', isPrivate: true, autoAnalyzeEnabled: false };
  const accepted = await http('POST', `${prefix}/repository/hosted`, 202, request, owner);
  const repeated = await http('POST', `${prefix}/repository/hosted`, 202, request, owner); assert.equal(accepted.jobId, repeated.jobId);
  await operation(project.id, accepted.jobId, owner);
  let repo = await confirmed(project.id, owner);
  assert.ok(repo.webhookConfirmedAt); assert.equal(repo.status, 'Ready');
  const repository = new URL(repo.cloneUrl).pathname.replace(/^\//, '').replace(/\.git$/, '');
  const hooks = await providerRequest('GET', `/repos/${repository}/hooks`); assert.equal(hooks.length, 1); assert.equal(hooks[0].active, true);
  assert.ok(!JSON.stringify(hooks).includes(process.env.Forgejo__WebhookSecret), 'Provider hook read-back must not disclose the signing secret');
  assert.equal(sql(`select count(*) from ops.outbox_messages where aggregate_id='${accepted.jobId}' and status='processed'`), '1');
  console.log('PASS M2 application provisioning: HTTP 202 → PostgreSQL/outbox → RabbitMQ → Worker → Forgejo + managed webhook.');

  const sshKey = join(workspace, 'developer-key');
  command('ssh-keygen', ['-q', '-t', 'ed25519', '-N', '', '-f', sshKey]);
  const keyToken = compose(['exec', '-T', 'forgejo', 'forgejo', 'admin', 'user', 'generate-access-token',
    '--username', 'forgejo-developer', '--token-name', 'isolated-ssh-key-setup', '--scopes', 'write:user', '--raw']).trim().split(/\s+/).at(-1);
  await providerRequest('POST', '/user/keys', { title: 'Isolated M2 SSH', key: readFileSync(sshKey + '.pub', 'utf8') }, keyToken);
  // TOFU is confined to a freshly created, owned loopback-only test server.
  process.env.GIT_SSH_COMMAND = `ssh -i "${sshKey.replaceAll('\\', '/')}" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new -o UserKnownHostsFile="${join(workspace, 'known_hosts').replaceAll('\\', '/')}"`;
  const providerRepo = await providerRequest('GET', `/repos/${repository}`);
  for (const [role, permission] of [['owner', 'admin'], ['developer', 'write'], ['viewer', 'read']])
    assert.equal((await providerRequest('GET', `/repos/${repository}/collaborators/forgejo-${role}/permission`)).permission, permission);
  const unrelated = await providerRequest('POST', '/user/repos', { name: `unrelated-${suffix}`, private: true, auto_init: false });
  const isolatedOwner = await fetch(provider + `/repos/forgejo-service/${unrelated.name}`, {
    headers: { Authorization: `token ${process.env.GODFORGE_TEST_FORGEJO_OWNER_TOKEN}` } });
  assert.equal(isolatedOwner.status, 404, 'ProjectOwner must not access unrelated service-owned private repositories'); assertions++;
  git(workspace, 'developer', ['ls-remote', providerRepo.ssh_url]);

  const developerPath = join(workspace, 'developer');
  git(workspace, 'developer', ['clone', repo.cloneUrl, developerPath]);
  writeFileSync(join(developerPath, 'project.godot'), 'config_version=5\n[application]\nconfig/name="M2 fixture"\n');
  writeFileSync(join(developerPath, 'player.gd'), 'extends Node\nvar health = 100\n');
  mkdirSync(join(developerPath, 'addons')); writeFileSync(join(developerPath, 'addons', 'helper.gd'), 'extends Resource\n');
  writeFileSync(join(developerPath, 'binary.bin'), Buffer.from([0, 1, 2, 255]));
  git(developerPath, 'developer', ['add', '.']);
  git(developerPath, 'developer', ['-c', 'user.name=M2 Developer', '-c', 'user.email=developer@example.test', 'commit', '-m', 'Godot fixture']);
  const initialSha = git(developerPath, 'developer', ['rev-parse', 'HEAD']);
  git(developerPath, 'developer', ['push', 'origin', 'HEAD:main']);
  git(developerPath, 'developer', ['push', providerRepo.ssh_url, 'HEAD:ssh-acceptance']);
  await eventually('actual provider push webhook and Worker revision sync', async () => (await http('GET', `${prefix}/repository`, 200, undefined, owner)).currentCommitHash === initialSha);
  assert.equal(sql(`select count(*) from repo.git_commits where repository_id='${repo.id}' and hash='${initialSha}'`), '1');
  assert.ok(Number(sql(`select count(*) from repo.webhook_events where repository_id='${repo.id}'`)) > 0);
  const branches = await http('GET', `${prefix}/repository/branches`, 200, undefined, developer); assert.ok(branches.some(b => b.name === 'main'));
  assert.equal((await http('GET', `${prefix}/repository/commits?branch=main`, 200, undefined, developer))[0].sha, initialSha);
  const tree = await http('GET', `${prefix}/repository/tree?commitSha=${initialSha}&path=addons`, 200, undefined, developer); assert.equal(tree.entries[0].name, 'helper.gd');
  assert.match((await http('GET', `${prefix}/repository/blob?commitSha=${initialSha}&path=project.godot`, 200, undefined, developer)).content, /config_version=5/);
  assert.match((await http('GET', `${prefix}/repository/blob?commitSha=${initialSha}&path=player.gd`, 200, undefined, viewer)).content, /extends Node/);
  for (const path of ['../secret', '%2e%2e/secret', 'addons\\..\\secret'])
    await http('GET', `${prefix}/repository/blob?commitSha=${initialSha}&path=${encodeURIComponent(path)}`, 400, undefined, owner);
  await http('GET', `${prefix}/repository/blob?commitSha=${initialSha}&path=binary.bin`, 400, undefined, owner);
  console.log('PASS M2 automatic signed push delivery, revision persistence, branches/commits/tree/text and hostile-path/binary rejection.');

  const delivery = JSON.parse(sql(`select json_build_object('uuid',uuid,'payload',payload_content) from hook_task where hook_id=${hooks[0].id} and event_type='push' order by id limit 1`, 'forgejo-db'));
  const revisionJobs = sql(`select count(*) from ops.jobs where repository_id='${repo.id}' and type='FetchRepository'`);
  const webhookCount = sql(`select count(*) from repo.webhook_events where repository_id='${repo.id}'`);
  const signature = createHmac('sha256', process.env.Forgejo__WebhookSecret).update(delivery.payload).digest('hex');
  for (const deliveryId of [delivery.uuid, randomUUID()]) {
    const replay = await fetch(api + '/webhooks/forgejo', { method: 'POST', body: delivery.payload,
      headers: { 'Content-Type': 'application/json', 'X-Forgejo-Delivery': deliveryId, 'X-Forgejo-Event': 'push', 'X-Forgejo-Signature': signature } });
    assert.equal(replay.status, 202); assertions++;
  }
  assert.equal(sql(`select count(*) from ops.jobs where repository_id='${repo.id}' and type='FetchRepository'`), revisionJobs);
  assert.equal(sql(`select count(*) from repo.webhook_events where repository_id='${repo.id}'`), String(Number(webhookCount) + 1));
  assert.equal(sql(`select count(*) from repo.git_commits where repository_id='${repo.id}' and hash='${initialSha}'`), '1');
  console.log('PASS original automatic webhook replay and a second delivery of the same revision create no duplicate sync job or commit.');

  const linkedProject = await http('POST', `/organizations/${org.id}/projects`, 201,
    { name: `Linked ${suffix}`, slug: `linked-${suffix}`, visibility: 'private' }, owner);
  const linkedPrefix = `/projects/${linkedProject.id}/repository`;
  await http('POST', linkedPrefix + '/link', 201, { remoteUrl: `https://linked-tls:3443/${repository}.git`,
    provider: 'Generic', defaultBranch: 'main', autoAnalyzeEnabled: false }, owner);
  const linkedSync = await http('POST', linkedPrefix + '/sync', 202, undefined, owner);
  await operation(linkedProject.id, linkedSync.jobId, owner);
  assert.equal((await http('GET', linkedPrefix, 200, undefined, owner)).currentCommitHash, initialSha);
  assert.equal((await http('GET', linkedPrefix + '/branches', 200, undefined, owner))[0].name, 'main');
  assert.equal((await http('GET', linkedPrefix + '/commits?branch=main', 200, undefined, owner))[0].sha, initialSha);
  assert.equal((await http('GET', linkedPrefix + `/tree?commitSha=${initialSha}&path=addons`, 200, undefined, owner)).entries[0].name, 'helper.gd');
  assert.match((await http('GET', linkedPrefix + `/blob?commitSha=${initialSha}&path=player.gd`, 200, undefined, owner)).content, /extends Node/);
  console.log('PASS linked anonymous HTTPS fetch, durable synchronization and cached branches/commits/tree/blob browsing.');

  const viewerPath = join(workspace, 'viewer'); git(workspace, 'viewer', ['clone', repo.cloneUrl, viewerPath]);
  writeFileSync(join(viewerPath, 'viewer.txt'), 'forbidden'); git(viewerPath, 'viewer', ['add', '.']);
  git(viewerPath, 'viewer', ['-c', 'user.name=M2 Viewer', '-c', 'user.email=viewer@example.test', 'commit', '-m', 'Viewer change']);
  git(viewerPath, 'viewer', ['push', 'origin', 'HEAD:viewer-branch'], 1);
  await http('PUT', `${prefix}/repository/branch-protection?branch=main`, 200, { enabled: true }, owner);
  assert.equal((await http('GET', `${prefix}/repository/branch-protection?branch=main`, 200, undefined, viewer)).enabled, true);
  writeFileSync(join(developerPath, 'normal.txt'), 'normal push'); git(developerPath, 'developer', ['add', '.']);
  git(developerPath, 'developer', ['-c', 'user.name=M2 Developer', '-c', 'user.email=developer@example.test', 'commit', '-m', 'Normal push']);
  git(developerPath, 'developer', ['push', 'origin', 'HEAD:main']);
  git(developerPath, 'developer', ['push', '--force', 'origin', `${initialSha}:main`], 1);
  git(developerPath, 'developer', ['push', 'origin', ':main'], 1);
  await http('PUT', `${prefix}/repository/branch-protection?branch=main`, 200, { enabled: false }, owner);
  git(developerPath, 'developer', ['push', '--force', 'origin', `${initialSha}:main`]);
  git(developerPath, 'developer', ['push', 'origin', 'HEAD:main']);
  git(developerPath, 'developer', ['push', 'origin', 'HEAD:protection-check']);
  await http('PUT', `${prefix}/repository/branch-protection?branch=protection-check`, 200, { enabled: true }, owner);
  git(developerPath, 'developer', ['push', 'origin', ':protection-check'], 1);
  await http('PUT', `${prefix}/repository/branch-protection?branch=protection-check`, 200, { enabled: false }, owner);
  git(developerPath, 'developer', ['push', 'origin', ':protection-check']);
  console.log('PASS real Git protection: ordinary Developer push allowed; force push and deletion denied.');

  let member = (await http('GET', `${prefix}/members?search=${encodeURIComponent(developer.email)}`, 200, undefined, owner))[0];
  compose(['pause', 'forgejo']);
  try {
    await http('PATCH', `${prefix}/members/${developer.id}`, 200, { role: 'viewer', version: member.version }, owner);
    await eventually('first role reconciliation running while provider is paused', () =>
      Number(sql(`select count(*) from ops.jobs where repository_id='${repo.id}' and type='ReconcileHostedRepository' and status='Running'`)) > 0);
    member = (await http('GET', `${prefix}/members?search=${encodeURIComponent(developer.email)}`, 200, undefined, owner))[0];
    await http('PATCH', `${prefix}/members/${developer.id}`, 200, { role: 'developer', version: member.version }, owner);
  } finally { compose(['unpause', 'forgejo']); }
  await confirmed(project.id, owner);
  git(developerPath, 'developer', ['push', 'origin', 'HEAD:latest-authoritative-role']);
  console.log('PASS overlapping provider reconciliation and membership mutation converge to the latest role.');
  member = (await http('GET', `${prefix}/members?search=${encodeURIComponent(developer.email)}`, 200, undefined, owner))[0];
  await http('PATCH', `${prefix}/members/${developer.id}`, 200, { role: 'viewer', version: member.version }, owner);
  await confirmed(project.id, owner);
  git(workspace, 'developer', ['ls-remote', repo.cloneUrl]); git(developerPath, 'developer', ['push', 'origin', 'HEAD:downgraded'], 1);
  member = (await http('GET', `${prefix}/members?search=${encodeURIComponent(developer.email)}`, 200, undefined, owner))[0];
  await http('PATCH', `${prefix}/members/${developer.id}`, 200, { role: 'developer', version: member.version }, owner); await confirmed(project.id, owner);
  await http('DELETE', `${prefix}/members/${developer.id}`, 204, undefined, owner);
  await http('GET', `${prefix}/repository/branches`, 404, undefined, developer);
  await confirmed(project.id, owner); git(workspace, 'developer', ['ls-remote', repo.cloneUrl], 1); git(developerPath, 'developer', ['push', 'origin', 'HEAD:removed'], 1);
  git(workspace, 'developer', ['ls-remote', providerRepo.ssh_url], 1);
  git(developerPath, 'developer', ['push', providerRepo.ssh_url, 'HEAD:ssh-removed'], 1);
  console.log('PASS GodForge membership downgrade/removal → real Worker reconciliation → actual HTTPS Git denial.');

  // Provider outage cannot block the authoritative membership transaction.
  compose(['stop', 'forgejo']);
  const start = Date.now();
  await http('DELETE', `/organizations/${org.id}/members/${revoked.id}`, 204, undefined, owner);
  assert.ok(Date.now() - start < 5000, 'Membership mutation must not wait for provider networking');
  await http('GET', `${prefix}/repository/branches`, 404, undefined, revoked);
  assert.ok(Number(sql(`select count(*) from ops.jobs where repository_id='${repo.id}' and type='ReconcileHostedRepository' and status in ('Queued','Running','Retrying')`)) > 0);
  const retry = await eventually('provider outage persisted with delayed retry', () => {
    const value = sql(`select json_build_object('attempt',attempt_count,'delay',extract(epoch from available_at-now())) from ops.jobs where repository_id='${repo.id}' and type='ReconcileHostedRepository' and status='Retrying' order by created_at desc limit 1`);
    return value && JSON.parse(value);
  });
  assert.ok(retry.attempt >= 1 && retry.attempt <= 5); assert.ok(retry.delay > 1, 'Provider retry must have durable backoff');
  compose(['start', 'forgejo']);
  await eventually('provider restored', () => fetch('http://localhost:33000/api/v1/version').then(r => r.ok));
  await confirmed(project.id, owner); git(workspace, 'revoked', ['ls-remote', repo.cloneUrl], 1);
  compose(['restart', 'worker']);
  const afterRestart = await http('POST', `${prefix}/repository/hosted`, 202, request, owner); assert.equal(afterRestart.jobId, accepted.jobId);
  assert.equal((await providerRequest('GET', '/user/repos')).filter(r => r.name.endsWith(repo.id.replaceAll('-', ''))).length, 1);
  console.log('PASS provider outage recovery, organization revocation, Worker restart and provisioning idempotency.');

  // Browser coverage runs against the same live application/provider, never mocked HTTP.
  process.env.GODFORGE_M2_BROWSER = 'isolated-compose';
  process.env.GODFORGE_M2_OWNER_EMAIL = owner.email; process.env.GODFORGE_M2_PASSWORD = password;
  process.env.GODFORGE_M2_OWNER_BEARER = owner.token; process.env.GODFORGE_M2_VIEWER_EMAIL = viewer.email;
  process.env.GODFORGE_M2_PROJECT_PATH = `/organizations/${org.id}/projects/${project.id}/repository`;
  command(process.execPath, [resolve('GodForge-E2E/node_modules/@playwright/test/cli.js'), 'test', '--config', 'playwright.m2.config.ts'],
    { cwd: resolve('GodForge-E2E'), stdio: 'inherit' });
  console.log(`M2 full application acceptance passed (${assertions} HTTP/Git assertions; separate provider and browser results above).`);
} catch (error) {
  console.error(`M2 acceptance failed: ${error.stack ?? error.message}`); process.exitCode = 1;
  if (owned) {
    try { console.error(compose(['logs', '--tail=60', 'worker'])); } catch { /* Original failure remains authoritative. */ }
  }
} finally {
  stopEnvironment();
  const full = resolve(workspace);
  if (!full.startsWith(resolve(tmpdir())) || !full.split(/[\\/]/).at(-1).startsWith('godforge-m2-acceptance-')) throw new Error('Unsafe fixture cleanup path.');
  rmSync(full, { recursive: true, force: true });
}
