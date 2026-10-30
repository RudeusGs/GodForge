import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';

// Only the isolated local development stack is targeted. No external email is sent.
const api = 'http://127.0.0.1:15072/api/v1';
const mailbox = 'http://127.0.0.1:18025';
const suffix = randomUUID().replaceAll('-', '');
const password = `Local-smoke1!-${suffix}`;
let checks = 0;

async function request(method, path, status, body, auth, extraHeaders = {}) {
  const response = await fetch(`${api}${path}`, {
    method, headers: { 'Content-Type': 'application/json', ...extraHeaders,
      ...(auth?.token ? { Authorization: `Bearer ${auth.token}` } : {}),
      ...(auth?.cookie ? { Cookie: auth.cookie } : {}),
    }, body: body === undefined ? undefined : JSON.stringify(body), signal: AbortSignal.timeout(15_000),
  });
  assert.equal(response.status, status, `${method} ${path}: unexpected HTTP status`);
  checks++;
  const json = response.status === 204 ? null : await response.json();
  return { response, json, data: json?.data };
}

async function waitForEmail(address, subject) {
  const deadline = Date.now() + 45_000;
  while (Date.now() < deadline) {
    const list = await fetch(`${mailbox}/api/v1/messages?limit=100`, { signal: AbortSignal.timeout(5_000) }).then(r => r.json());
    const message = list.messages.find(m => m.Subject === subject && m.To.some(to => to.Address === address));
    if (message) return fetch(`${mailbox}/api/v1/message/${message.ID}`, { signal: AbortSignal.timeout(5_000) }).then(r => r.json());
    await new Promise(resolve => setTimeout(resolve, 500));
  }
  throw new Error(`Timed out waiting for local SMTP delivery: ${subject}`);
}

function emailToken(message) {
  const match = message.HTML.match(/href="([^"]+)"/);
  assert.ok(match, 'Local challenge email must contain a link');
  const token = new URL(match[1].replaceAll('&amp;', '&')).searchParams.get('token');
  assert.ok(token, 'Local challenge email must contain a token');
  return token;
}

async function login(email, secret = password, deviceName = 'Backend smoke') {
  const result = await request('POST', '/auth/login', 200, { email, password: secret, deviceName });
  const cookie = result.response.headers.get('set-cookie');
  assert.ok(cookie?.includes('httponly') || cookie?.includes('HttpOnly'), 'Refresh cookie must be HttpOnly');
  assert.ok(!('refreshToken' in result.data), 'Raw refresh token must not appear in JSON');
  return { token: result.data.accessToken, cookie: cookie.split(';')[0], userId: result.data.user.id, sessionId: result.data.session.id };
}

async function register(label) {
  const email = `${label}-${suffix}@example.test`;
  await request('POST', '/auth/send-otp', 202, { email });
  const message = await waitForEmail(email, 'GodForge - Email verification');
  const otp = message.HTML.match(/<strong>(\d{6})<\/strong>/)?.[1];
  assert.ok(otp, 'OTP must be delivered by the local outbox/SMTP path');
  await request('POST', '/auth/register', 201, { email, password, displayName: label, otp });
  return { email, ...await login(email) };
}

try {
  assert.equal((await fetch('http://127.0.0.1:15072/health/ready')).status, 200);
  const owner = await register('owner');
  const member = await register('member');
  console.log('PASS: registration OTP, real outbox/SMTP delivery, registration and secure login');

  const organizationBody = { name: `Backend smoke ${suffix}`, slug: `smoke-${suffix}` };
  const key = randomUUID();
  const organization = (await request('POST', '/organizations', 201, organizationBody, owner, { 'Idempotency-Key': key })).data;
  const replay = (await request('POST', '/organizations', 201, organizationBody, owner, { 'Idempotency-Key': key })).data;
  assert.equal(replay.id, organization.id, 'Idempotent creation must return the same organization');
  const org = `/organizations/${organization.id}`;
  await request('GET', '/organizations', 200, undefined, owner);
  const project = (await request('POST', `${org}/projects`, 201, {
    name: `Godot smoke ${suffix}`, slug: `godot-${suffix}`, description: 'Local backend acceptance fixture', visibility: 'private'
  }, owner)).data;
  const proj = `/projects/${project.id}`;
  await request('GET', proj, 404, undefined, member);
  await request('POST', `${org}/invitations`, 201, { email: member.email, role: 'organizationMember' }, owner);
  const invite = emailToken(await waitForEmail(member.email, 'GodForge organization invitation'));
  await request('POST', '/organization-invitations/accept', 200, { token: invite }, member);
  await request('GET', proj, 403, undefined, member);
  await request('POST', `${proj}/members`, 201, { userId: member.userId, role: 'developer' }, owner);
  await request('GET', proj, 200, undefined, member);
  console.log('PASS: organization/project creation, idempotency, invitation acceptance and tenant isolation');

  await request('POST', `${proj}/transfer-ownership`, 200, { newOwnerUserId: member.userId, retainCurrentOwnerAs: 'maintainer', version: 1 }, owner);
  await request('POST', `${proj}/transfer-ownership`, 409, { newOwnerUserId: owner.userId, retainCurrentOwnerAs: 'maintainer', version: 1 }, member);
  await request('POST', `${proj}/transfer-ownership`, 200, { newOwnerUserId: owner.userId, retainCurrentOwnerAs: 'developer', version: 2 }, member);
  await request('POST', `${org}/transfer-ownership`, 200, { newOwnerUserId: member.userId, retainCurrentOwnerAs: 'organizationAdmin', version: 1 }, owner);
  await request('POST', `${org}/transfer-ownership`, 409, { newOwnerUserId: owner.userId, retainCurrentOwnerAs: 'organizationMember', version: 1 }, member);
  await request('POST', `${org}/transfer-ownership`, 200, { newOwnerUserId: owner.userId, retainCurrentOwnerAs: 'organizationMember', version: 2 }, member);
  await request('POST', `${proj}/archive`, 200, { version: 3 }, owner);
  await request('GET', proj, 200, undefined, member);
  await request('PATCH', proj, 409, { name: 'Changed', slug: 'changed', description: null, visibility: 'private', version: 4 }, owner);
  await request('POST', `${proj}/members`, 409, { userId: member.userId, role: 'viewer' }, owner);
  await request('POST', `${proj}/restore`, 409, { version: 3 }, owner);
  await request('POST', `${proj}/restore`, 200, { version: 4 }, owner);
  await request('DELETE', `${org}/members/${member.userId}`, 204, undefined, owner);
  await request('GET', proj, 404, undefined, member);
  console.log('PASS: versioned ownership transfers, archive/restore and immediate organization/project revocation');

  const second = await login(owner.email, password, 'Second smoke device');
  await request('GET', '/users/me/sessions', 200, undefined, owner);
  await request('DELETE', `/users/me/sessions/${second.sessionId}`, 204, undefined, owner);
  await request('GET', '/users/me', 401, undefined, second);
  await request('POST', '/auth/refresh', 200, undefined, owner);
  await request('POST', '/auth/forgot-password', 202, { email: owner.email });
  const reset = emailToken(await waitForEmail(owner.email, 'GodForge - Password reset'));
  const newPassword = `Changed-smoke2!-${suffix}`;
  await request('POST', '/auth/reset-password', 204, { email: owner.email, token: reset, newPassword });
  await request('GET', '/users/me', 401, undefined, owner);
  const finalSession = await login(owner.email, newPassword);
  await request('POST', '/auth/logout', 204, undefined, finalSession);
  await request('POST', '/auth/refresh', 401, undefined, finalSession);
  console.log('PASS: multi-device revocation, refresh, password-reset delivery/invalidation and logout');
  console.log(`Backend smoke passed (${checks} HTTP assertions). Fixtures remain in the local Compose database for inspection.`);
} catch (error) {
  console.error(`Backend smoke failed: ${error.message}`);
  process.exitCode = 1;
}
