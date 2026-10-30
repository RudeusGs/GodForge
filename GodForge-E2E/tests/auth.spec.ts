import { test, expect, type BrowserContext, type Page } from '@playwright/test';

// Controlled HTTP contracts exercise the real Vue application in Chromium.
// Real PostgreSQL HTTP tests separately verify server authorization and rotation.
async function installAuthContract(context: BrowserContext) {
  const session = {
    id: '11111111-1111-1111-1111-111111111111', deviceName: 'Browser test',
    createdAt: '2026-10-01T10:00:00Z', lastSeenAt: null,
    expiresAt: '2099-10-01T10:00:00Z', current: true, revokedAt: null,
  };
  const auth = {
    user: { id: '22222222-2222-2222-2222-222222222222', email: 'browser@example.test',
      displayName: 'Browser test', status: 'Active', emailVerifiedAt: session.createdAt,
      createdAt: session.createdAt, version: 1 },
    session, accessToken: 'browser-test-memory-token',
    accessTokenExpiresAt: session.expiresAt, refreshTokenExpiresAt: session.expiresAt,
  };
  const state = { authenticated: false, logoutFails: false, refreshFails: false,
    sessionsFail: false, refreshCalls: 0, resetPayload: null as unknown };
  await context.route('**/api/v1/**', async route => {
    const path = new URL(route.request().url()).pathname;
    const reply = (status: number, data?: unknown) => route.fulfill({
      status, contentType: 'application/json',
      body: status === 204 ? undefined : JSON.stringify({ data }),
    });
    if (path.endsWith('/auth/refresh')) {
      state.refreshCalls++;
      return reply(state.refreshFails ? 503 : state.authenticated ? 200 : 401, auth);
    }
    if (path.endsWith('/users/me/settings')) return reply(200, { theme: 'dark', notificationInApp: true, notificationEmail: true, notificationDigest: 'off', version: 1, createdAt: session.createdAt, updatedAt: session.createdAt });
    if (path.endsWith('/auth/login')) {
      state.authenticated = true;
      return reply(200, auth);
    }
    if (path.endsWith('/auth/logout')) {
      if (state.logoutFails) return reply(503);
      state.authenticated = false;
      return reply(204);
    }
    if (path.endsWith('/auth/reset-password')) {
      state.resetPayload = route.request().postDataJSON();
      state.authenticated = false;
      return reply(204);
    }
    if (path.endsWith(`/users/me/sessions/${session.id}`) && route.request().method() === 'DELETE') {
      state.authenticated = false;
      return reply(204);
    }
    if (path.endsWith('/organizations')) return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ data: [], meta: { page: 1, pageSize: 20, totalCount: 0 } }) });
    if (path.endsWith('/users/me/sessions')) return reply(state.sessionsFail ? 503 : 200, [session]);
    throw new Error(`Unexpected API request: ${route.request().method()} ${path}`);
  });
  return state;
}

async function signIn(page: Page, returnTo = '/dashboard') {
  await page.goto(`/login?returnTo=${encodeURIComponent(returnTo)}`);
  await page.getByLabel('Email address').fill('browser@example.test');
  await page.getByLabel('Password', { exact: true }).fill('Browser-test-password1');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page).toHaveURL(new RegExp(`${returnTo}$`));
}

test('TC-AUTH-BROWSER-001: login preserves the return path and keeps secrets out of storage', async ({ page, context }) => {
  await installAuthContract(context);
  await signIn(page, '/account/sessions');
  await expect(page.getByRole('heading', { name: 'Active and recent sessions' })).toBeVisible();
  const stored = await page.evaluate(() => JSON.stringify({ ...localStorage, ...sessionStorage }));
  expect(stored).not.toContain('browser-test-memory-token');
  expect(stored).not.toContain('Browser-test-password1');
});

test('TC-AUTH-BROWSER-002: reload restores the session through refresh', async ({ page, context }) => {
  const state = await installAuthContract(context);
  await signIn(page);
  const calls = state.refreshCalls;
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  expect(state.refreshCalls).toBe(calls + 1);
});

test('TC-AUTH-BROWSER-003: logout prevents resurrection on reload', async ({ page, context }) => {
  const state = await installAuthContract(context);
  await signIn(page);
  await page.locator('summary').click();
  await page.getByRole('button', { name: 'Logout' }).click();
  await expect(page).toHaveURL(/\/login\?/);
  const calls = state.refreshCalls;
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Welcome back.' })).toBeVisible();
  expect(state.refreshCalls).toBe(calls);
});

test('TC-AUTH-BROWSER-004: failed logout explains the outcome and blocks automatic restoration', async ({ page, context }) => {
  const state = await installAuthContract(context);
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  await signIn(page);
  state.logoutFails = true;
  await page.locator('summary').click();
  await page.getByRole('button', { name: 'Logout' }).click();
  await expect(page).toHaveURL(/authReason=logout-incomplete/);
  await expect(page.getByRole('status')).toContainText('server could not confirm session revocation');
  const calls = state.refreshCalls;
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Welcome back.' })).toBeVisible();
  expect(state.refreshCalls).toBe(calls);
  expect(errors).toEqual([]);
});

test('TC-AUTH-BROWSER-005: logout propagates to another open tab', async ({ page, context }) => {
  await installAuthContract(context);
  await signIn(page);
  const second = await context.newPage();
  await second.goto('/dashboard');
  await expect(second.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  await page.locator('summary').click();
  await page.getByRole('button', { name: 'Logout' }).click();
  await expect(second).toHaveURL(/\/login\?/);
  await expect(second.getByRole('heading', { name: 'Welcome back.' })).toBeVisible();
});

test('TC-AUTH-BROWSER-006: session-check outage can be retried without false logout', async ({ page, context }) => {
  const state = await installAuthContract(context);
  state.authenticated = true;
  state.refreshFails = true;
  await page.goto('/dashboard');
  await expect(page.getByText('Unable to verify session.')).toBeVisible();
  state.refreshFails = false;
  await page.getByRole('button', { name: 'Retry connection' }).click();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page).toHaveURL(/\/dashboard$/);
});

test('TC-AUTH-BROWSER-007: revoking the current session returns to sign in', async ({ page, context }) => {
  await installAuthContract(context);
  await signIn(page, '/account/sessions');
  await page.getByRole('button', { name: 'Revoke session for Browser test' }).click();
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByRole('heading', { name: 'Welcome back.' })).toBeVisible();
});

test('TC-AUTH-BROWSER-008: session-list failure has a working retry', async ({ page, context }) => {
  const state = await installAuthContract(context);
  state.sessionsFail = true;
  await signIn(page, '/account/sessions');
  await expect(page.getByRole('alert')).toContainText('Sessions could not be loaded');
  state.sessionsFail = false;
  await page.getByRole('button', { name: 'Retry', exact: true }).click();
  await expect(page.getByRole('list', { name: 'Account sessions' })).toContainText('Browser test');
  await expect(page.getByRole('alert')).toHaveCount(0);
});

test('TC-AUTH-BROWSER-009: reset link is scrubbed and reset clears another authenticated tab', async ({ page, context }) => {
  const state = await installAuthContract(context);
  await signIn(page);
  const reset = await context.newPage();
  await reset.goto('/reset-password?email=browser%40example.test&token=browser-reset-challenge');
  await expect(reset).toHaveURL(/\/reset-password$/);
  await reset.getByLabel('New password', { exact: true }).fill('New-browser-password1');
  await reset.getByLabel('Confirm password', { exact: true }).fill('New-browser-password1');
  await reset.getByRole('button', { name: 'Update password' }).click();
  await expect(reset.getByRole('heading', { name: 'Access restored.' })).toBeVisible();
  expect(state.resetPayload).toEqual({ email: 'browser@example.test', token: 'browser-reset-challenge', newPassword: 'New-browser-password1' });
  await expect(page).toHaveURL(/\/login\?/);
  await expect(page.getByRole('status')).toContainText('Your password changed');
});

test('TC-AUTH-BROWSER-010: protected navigation preserves the intended destination', async ({ page, context }) => {
  await installAuthContract(context);
  await page.goto('/account/sessions');
  await expect(page).toHaveURL(/\/login\?returnTo=\/account\/sessions$/);
  await expect(page.getByRole('heading', { name: 'Welcome back.' })).toBeVisible();
});

test('TC-AUTH-BROWSER-011: retrying an outage with an expired session protects the route', async ({ page, context }) => {
  const state = await installAuthContract(context);
  state.refreshFails = true;
  await page.goto('/dashboard');
  await expect(page.getByText('Unable to verify session.')).toBeVisible();
  state.refreshFails = false;
  await page.getByRole('button', { name: 'Retry connection' }).click();
  await expect(page).toHaveURL(/\/login\?/);
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Welcome back.' })).toBeVisible();
});

test('TC-SETTINGS-BROWSER-001: accessible preferences save, reload and retain edits on conflict', async ({ page, context }) => {
  await installAuthContract(context);
  let preferences = { theme: 'dark', notificationInApp: true, notificationEmail: true, notificationDigest: 'off', version: 1, createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z' };
  await context.route('**/api/v1/users/me/settings', async route => {
    if (route.request().method() === 'PUT') {
      const input = route.request().postDataJSON() as typeof preferences;
      if (input.version !== preferences.version) return route.fulfill({ status: 409, contentType: 'application/json', body: JSON.stringify({ error: { code: 'CONCURRENCY_CONFLICT' } }) });
      preferences = { ...preferences, ...input, version: preferences.version + 1 };
    }
    return route.fulfill({ contentType: 'application/json', body: JSON.stringify({ data: preferences }) });
  });
  await signIn(page, '/account/preferences');
  await page.getByLabel('Theme', { exact: true }).selectOption('light');
  await page.getByLabel('Notification digest', { exact: true }).selectOption('weekly');
  await page.getByRole('button', { name: 'Save preferences', exact: true }).click();
  await expect(page.getByText('Preferences saved.', { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByLabel('Notification digest', { exact: true })).toHaveValue('weekly');
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
  preferences.version++;
  await page.getByLabel('Theme', { exact: true }).selectOption('dark');
  await page.getByRole('button', { name: 'Save preferences', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('another session');
  await expect(page.getByLabel('Theme', { exact: true })).toHaveValue('dark');
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
