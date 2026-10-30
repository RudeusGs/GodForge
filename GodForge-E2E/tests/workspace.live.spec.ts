import { test, expect, type APIRequestContext, type Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';

// Disposable local Compose stack only: real API, PostgreSQL, Redis and SMTP/outbox.
// No request interception, seeded identity, production service or external email.
const api = 'http://127.0.0.1:25072/api/v1';
const mailbox = 'http://127.0.0.1:28025';

test.beforeEach(() => {
  if (process.env.GODFORGE_WORKSPACE_ACCEPTANCE !== 'isolated-compose') throw new Error('Run live acceptance through the isolated workspace runner.');
  // Independent cases must not inherit the previous case's OTP/IP budget.
  // No production setting is changed; only this fixed disposable stack is addressed.
  execFileSync('docker', ['compose', '-p', 'godforge-workspace-acceptance', '-f', '../docker-compose.backend.yml', '-f', '../docker-compose.workspace-test.yml', 'exec', '-T', 'redis', 'redis-cli', 'EVAL', "for _, key in ipairs(redis.call('KEYS', 'auth:rate:*')) do redis.call('DEL', key) end return 1", '0'], { stdio: 'pipe' });
});

async function signIn(page: Page, email: string, password: string) {
  await page.goto('/login');
  await page.getByLabel('Email address').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
}

// All identities and memberships are created through the real local API and SMTP.
// Each test owns separate fixtures; Compose removes only this run's volumes.
async function projectFixture(request: APIRequestContext) {
  const suffix = randomUUID().replaceAll('-', '').slice(0, 14);
  const password = `ProjectPass1!-${suffix}`;
  const ownerEmail = `project-owner-${suffix}@example.test`;
  const memberEmail = `project-member-${suffix}@example.test`;
  await registerVerifiedUser(request, ownerEmail, password, `Owner ${suffix}`);
  await registerVerifiedUser(request, memberEmail, password, `Member ${suffix}`);
  const login = async (email: string) => {
    const response = await request.post(`${api}/auth/login`, { data: { email, password, deviceName: 'Project acceptance' } });
    expect(response.status()).toBe(200);
    return (await response.json()).data as { accessToken: string; user: { id: string } };
  };
  const owner = await login(ownerEmail);
  const member = await login(memberEmail);
  const ownerHeaders = { Authorization: `Bearer ${owner.accessToken}` };
  const memberHeaders = { Authorization: `Bearer ${member.accessToken}` };
  const response = await request.post(`${api}/organizations`, { headers: ownerHeaders, data: { name: `Project Studio ${suffix}`, slug: `project-studio-${suffix}` } });
  expect(response.status()).toBe(201);
  const orgId = (await response.json()).data.id as string;
  expect((await request.post(`${api}/organizations/${orgId}/invitations`, { headers: ownerHeaders, data: { email: memberEmail, role: 'organizationMember' } })).status()).toBe(201);
  let messageId = '';
  await expect.poll(async () => {
    const messages = (await (await request.get(`${mailbox}/api/v1/messages?limit=100`)).json()) as { messages: { ID: string; Subject: string; To: { Address: string }[] }[] };
    messageId = messages.messages.find(m => m.Subject === 'GodForge organization invitation' && m.To.some(to => to.Address === memberEmail))?.ID ?? '';
    return !!messageId;
  }, { timeout: 45_000 }).toBe(true);
  const message = (await (await request.get(`${mailbox}/api/v1/message/${messageId}`)).json()) as { HTML: string };
  const href = message.HTML.match(/href="([^"]+organization-invitations\/accept\?token=[^"]+)"/)?.[1];
  if (!href) throw new Error('Local invitation delivery did not contain an acceptance link.');
  const token = new URL(href.replaceAll('&amp;', '&')).searchParams.get('token');
  expect((await request.post(`${api}/organization-invitations/accept`, { headers: memberHeaders, data: { token } })).status()).toBe(200);
  const created = await request.post(`${api}/organizations/${orgId}/projects`, { headers: ownerHeaders, data: { name: `Project Game ${suffix}`, slug: `project-game-${suffix}`, description: 'Live project management', visibility: 'private' } });
  expect(created.status()).toBe(201);
  const project = (await created.json()).data as { id: string; slug: string; version: number };
  return { suffix, password, ownerEmail, memberEmail, ownerHeaders, memberHeaders, ownerId: owner.user.id, memberId: member.user.id, orgId, project, path: `/organizations/${orgId}/projects/${project.id}`, prefix: `${api}/projects/${project.id}` };
}

test('TC-PROJECT-LIVE-001: organization member assignment, real project role and forbidden administration', async ({ page, browser, request }) => {
  const f = await projectFixture(request);
  await signIn(page, f.ownerEmail, f.password);
  await page.goto(`${f.path}/members`);
  await page.getByRole('button', { name: 'Add member', exact: true }).click();
  await page.getByLabel('Search candidates', { exact: true }).fill(f.memberEmail);
  await page.getByRole('button', { name: 'Search candidates', exact: true }).click();
  await page.getByRole('button', { name: `Select Member ${f.suffix}`, exact: true }).click();
  await page.getByLabel('New member role').selectOption('developer');
  await page.getByRole('button', { name: 'Confirm add member', exact: true }).click();
  await expect(page.getByText('Project member added.', { exact: true })).toBeVisible();
  const persisted = (await (await request.get(`${f.prefix}/members`, { headers: f.ownerHeaders, params: { search: f.memberEmail } })).json()).data as { userId: string; role: string }[];
  expect(persisted).toEqual([expect.objectContaining({ userId: f.memberId, role: 'developer' })]);
  const context = await browser.newContext();
  try {
    const collaborator = await context.newPage(); await signIn(collaborator, f.memberEmail, f.password); await collaborator.goto(f.path);
    await expect(collaborator.getByText('developer', { exact: true })).toBeVisible();
    await expect(collaborator.getByRole('navigation', { name: 'Project sections' }).getByRole('link', { name: 'Settings', exact: true })).toHaveCount(0);
    expect((await request.post(`${f.prefix}/members`, { headers: f.memberHeaders, data: { userId: f.ownerId, role: 'projectOwner' } })).status()).toBe(403);
  } finally { await context.close(); }
});

test('TC-PROJECT-LIVE-002: role change, escalation denial and immediate revocation with organization retained', async ({ page, request }) => {
  const f = await projectFixture(request);
  expect((await request.post(`${f.prefix}/members`, { headers: f.ownerHeaders, data: { userId: f.memberId, role: 'developer' } })).status()).toBe(201);
  await signIn(page, f.ownerEmail, f.password); await page.goto(`${f.path}/members`);
  const row = page.getByRole('list', { name: 'Project members', exact: true }).getByRole('listitem').filter({ hasText: f.memberEmail });
  await row.getByRole('button', { name: 'Change role', exact: true }).click();
  await page.getByLabel('Project role', { exact: true }).selectOption('reviewer');
  await page.getByRole('button', { name: 'Confirm role change', exact: true }).click();
  await expect(row.getByText('reviewer · active', { exact: true })).toBeVisible();
  const memberResponse = await request.get(`${f.prefix}/members`, { headers: f.ownerHeaders, params: { search: f.memberEmail } });
  const target = (await memberResponse.json()).data[0] as { version: number; role: string };
  expect(target.role).toBe('reviewer');
  expect((await request.patch(`${f.prefix}/members/${f.memberId}`, { headers: f.memberHeaders, data: { role: 'projectOwner', version: target.version } })).status()).toBe(403);
  await row.getByRole('button', { name: 'Remove member', exact: true }).click(); await page.getByRole('button', { name: 'Confirm removal', exact: true }).click();
  await expect(page.getByText('Project member removed. Organization membership is retained.', { exact: true })).toBeVisible();
  expect((await request.get(f.prefix, { headers: f.memberHeaders })).status()).toBe(403);
  const organizationMembers = (await (await request.get(`${api}/organizations/${f.orgId}/members`, { headers: f.memberHeaders, params: { search: f.memberEmail } })).json()).data as { status: string }[];
  expect(organizationMembers[0]?.status).toBe('active');
});

test('TC-PROJECT-LIVE-003: ownership transfer advances project version and rejects stale transfer', async ({ page, request }) => {
  const f = await projectFixture(request);
  expect((await request.post(`${f.prefix}/members`, { headers: f.ownerHeaders, data: { userId: f.memberId, role: 'developer' } })).status()).toBe(201);
  await signIn(page, f.ownerEmail, f.password); await page.goto(`${f.path}/settings`);
  await page.getByLabel('Search candidates', { exact: true }).fill(f.memberEmail); await page.getByRole('button', { name: 'Search candidates', exact: true }).click();
  await page.getByRole('button', { name: `Select Member ${f.suffix}`, exact: true }).click(); await page.getByLabel('Your retained role').selectOption('maintainer');
  await page.getByRole('button', { name: 'Review ownership transfer', exact: true }).click(); await page.getByRole('button', { name: 'Confirm ownership', exact: true }).click();
  await expect(page.getByText('Project ownership transferred.', { exact: true })).toBeVisible();
  const members = (await (await request.get(`${f.prefix}/members`, { headers: f.memberHeaders })).json()).data as { userId: string; role: string }[];
  expect(members.find(m => m.userId === f.memberId)?.role).toBe('projectOwner'); expect(members.find(m => m.userId === f.ownerId)?.role).toBe('maintainer');
  const stale = await request.post(`${f.prefix}/transfer-ownership`, { headers: f.memberHeaders, data: { newOwnerUserId: f.ownerId, retainCurrentOwnerAs: 'maintainer', version: 1 } });
  expect(stale.status()).toBe(409); expect((await stale.json()).error.code).toBe('CONCURRENCY_CONFLICT');
  expect((await (await request.get(f.prefix, { headers: f.memberHeaders })).json()).data.version).toBe(2);
});

test('TC-PROJECT-LIVE-004: archive and restore preserve reads and removed memberships', async ({ page, request }) => {
  const f = await projectFixture(request);
  expect((await request.post(`${f.prefix}/members`, { headers: f.ownerHeaders, data: { userId: f.memberId, role: 'viewer' } })).status()).toBe(201);
  expect((await request.delete(`${f.prefix}/members/${f.memberId}`, { headers: f.ownerHeaders })).status()).toBe(204);
  await signIn(page, f.ownerEmail, f.password); await page.goto(`${f.path}/settings`);
  await page.getByRole('button', { name: 'Archive project', exact: true }).click(); await page.getByRole('button', { name: 'Confirm archive', exact: true }).click();
  await expect(page.getByText('Project archived.', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Save general settings', exact: true })).toBeDisabled();
  const rejected = await request.post(`${f.prefix}/members`, { headers: f.ownerHeaders, data: { userId: f.memberId, role: 'developer' } }); expect(rejected.status()).toBe(409); expect((await rejected.json()).error.code).toBe('PROJECT_ARCHIVED');
  await page.getByRole('button', { name: 'Restore project', exact: true }).click(); await page.getByRole('button', { name: 'Confirm restore', exact: true }).click();
  await expect(page.getByText('Project restored.', { exact: true })).toBeVisible(); expect((await request.get(f.prefix, { headers: f.memberHeaders })).status()).toBe(403);
});

test('TC-PROJECT-LIVE-005: metadata and settings use independent versions; deletion retains deleting state', async ({ page, request }) => {
  const f = await projectFixture(request);
  await signIn(page, f.ownerEmail, f.password); await page.goto(`${f.path}/settings`);
  await page.getByLabel('Project name', { exact: true }).fill(`Updated ${f.suffix}`); await page.getByLabel('Project description').fill('Updated live description');
  await page.getByRole('button', { name: 'Save general settings', exact: true }).click(); await expect(page.getByText('Project metadata saved.', { exact: true })).toBeVisible();
  const updated = (await (await request.get(f.prefix, { headers: f.ownerHeaders })).json()).data as { name: string; version: number };
  expect(updated.name).toBe(`Updated ${f.suffix}`); expect(updated.version).toBe(2);
  await page.getByLabel('Enable AI advisory preference').check(); await page.getByLabel('Default asset visibility').selectOption('internal');
  await page.getByRole('button', { name: 'Save analysis preferences', exact: true }).click(); await expect(page.getByText('Project settings saved.', { exact: true })).toBeVisible();
  const saved = (await (await request.get(`${f.prefix}/settings`, { headers: f.ownerHeaders })).json()).data as { version: number; aiAdvisoryEnabled: boolean; defaultAssetVisibility: string };
  expect(saved).toMatchObject({ version: 2, aiAdvisoryEnabled: true, defaultAssetVisibility: 'internal' });
  const stale = await request.put(`${f.prefix}/settings`, { headers: f.ownerHeaders, data: { analysisProfileKey: 'current-default-v1', aiAdvisoryEnabled: false, defaultAssetVisibility: 'private', notificationPolicyVersion: 1, version: 1 } });
  expect(stale.status()).toBe(409); expect((await stale.json()).error.code).toBe('CONCURRENCY_CONFLICT');
  await page.screenshot({ path: '../artifacts/project-settings-desktop.png', fullPage: true });
  await page.getByLabel('Confirm project slug').fill(f.project.slug); await page.getByRole('button', { name: 'Request project deletion', exact: true }).click(); await page.getByRole('button', { name: 'Confirm delete', exact: true }).click();
  await expect(page.getByText('Deletion request accepted.', { exact: false })).toBeVisible();
  expect((await (await request.get(f.prefix, { headers: f.ownerHeaders })).json()).data.status).toBe('deleting');
  await page.setViewportSize({ width: 390, height: 844 }); expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: '../artifacts/project-settings-mobile.png', fullPage: true });
});

test('TC-PROJECT-LIVE-006: tenant isolation, route mismatch and organization revocation invalidate old sessions', async ({ page, request }) => {
  const f = await projectFixture(request);
  expect((await request.post(`${f.prefix}/members`, { headers: f.ownerHeaders, data: { userId: f.memberId, role: 'developer' } })).status()).toBe(201);
  const other = await request.post(`${api}/organizations`, { headers: f.ownerHeaders, data: { name: `Other ${f.suffix}`, slug: `other-${f.suffix}` } }); expect(other.status()).toBe(201);
  const otherOrgId = (await other.json()).data.id as string;
  const otherProject = await request.post(`${api}/organizations/${otherOrgId}/projects`, { headers: f.ownerHeaders, data: { name: 'Other game', slug: 'other-game', visibility: 'private' } }); expect(otherProject.status()).toBe(201);
  const otherId = (await otherProject.json()).data.id as string;
  expect((await request.get(`${api}/projects/${otherId}`, { headers: f.memberHeaders })).status()).toBe(404);
  expect((await request.post(`${api}/projects/${otherId}/archive`, { headers: f.memberHeaders, data: { version: 1 } })).status()).toBe(404);
  await signIn(page, f.memberEmail, f.password); await page.goto(`/organizations/${otherOrgId}/projects/${f.project.id}`);
  await expect(page.getByRole('alert').filter({ hasText: 'unavailable' })).toBeVisible(); await expect(page.getByText('Live project management', { exact: true })).toHaveCount(0);
  expect((await request.delete(`${api}/organizations/${f.orgId}/members/${f.memberId}`, { headers: f.ownerHeaders })).status()).toBe(204);
  expect((await request.get(f.prefix, { headers: f.memberHeaders })).status()).toBe(404);
  await page.goto(f.path); await expect(page.getByRole('alert').filter({ hasText: 'unavailable' })).toBeVisible();
});

async function registerVerifiedUser(
  request: APIRequestContext,
  email: string,
  password: string,
  displayName: string
) {
  expect((await request.post(`${api}/auth/send-otp`, { data: { email } })).status()).toBe(202);
  let messageId = '';
  await expect
    .poll(
      async () => {
        const response = await request.get(`${mailbox}/api/v1/messages?limit=100`);
        const list = (await response.json()) as {
          messages: { ID: string; To: { Address: string }[]; Subject: string }[];
        };
        messageId =
          list.messages.find(
            (message) =>
              message.Subject === 'GodForge - Email verification' &&
              message.To.some((to) => to.Address === email)
          )?.ID ?? '';
        return !!messageId;
      },
      { timeout: 45_000 }
    )
    .toBe(true);
  const message = (await (await request.get(`${mailbox}/api/v1/message/${messageId}`)).json()) as {
    HTML: string;
  };
  const otp = message.HTML.match(/<strong>(\d{6})<\/strong>/)?.[1];
  expect(otp).toBeTruthy();
  expect(
    (
      await request.post(`${api}/auth/register`, {
        data: { email, password, displayName, otp },
      })
    ).status()
  ).toBe(201);
}

test('TC-WORKSPACE-LIVE-001: verified account signs in, creates organization/project and restores metadata on reload', async ({
  page,
  request,
}) => {
  expect((await request.get('http://127.0.0.1:25072/health/ready')).status()).toBe(200);
  const suffix = randomUUID().replaceAll('-', '').slice(0, 16);
  const email = `workspace-${suffix}@example.test`;
  const password = `Workspace1!-${suffix}`;
  await registerVerifiedUser(request, email, password, 'Workspace acceptance');

  await page.goto('/login');
  await page.getByLabel('Email address').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Create your first organization' })).toBeVisible();
  await page.getByLabel('Name', { exact: true }).fill(`Acceptance studio ${suffix}`);
  await page.getByLabel('Slug', { exact: true }).fill(`studio-${suffix}`);
  const organizationCreated = page.waitForResponse(
    (response) => response.url() === `${api}/organizations` && response.request().method() === 'POST'
  );
  await page.getByRole('button', { name: 'Create organization', exact: true }).click();
  const orgResponse = await organizationCreated;
  expect(orgResponse.status()).toBe(201);
  const org = (await orgResponse.json()).data as {
    id: string;
    currentUserRole: string;
    version: number;
  };
  expect(org.currentUserRole).toBe('organizationOwner');
  expect(org.version).toBe(1);
  await expect(page).toHaveURL(new RegExp(`/organizations/${org.id}/projects$`));
  await expect(page.getByLabel('Organization', { exact: true })).toHaveValue(org.id);
  await page.getByRole('button', { name: 'New project' }).click();
  await page.getByLabel('Name', { exact: true }).fill(`Acceptance game ${suffix}`);
  await page.getByLabel('Slug', { exact: true }).fill(`game-${suffix}`);
  await page.getByLabel('Description').fill('Real local browser-to-API workspace acceptance');
  const projectCreated = page.waitForResponse(
    (response) =>
      response.url() === `${api}/organizations/${org.id}/projects` &&
      response.request().method() === 'POST'
  );
  await page.getByRole('button', { name: 'Create project', exact: true }).click();
  const projectResponse = await projectCreated;
  expect(projectResponse.status()).toBe(201);
  const project = (await projectResponse.json()).data as {
    id: string;
    organizationId: string;
    currentUserRole: string;
    version: number;
  };
  expect(project.organizationId).toBe(org.id);
  expect(project.currentUserRole).toBe('projectOwner');
  expect(project.version).toBe(1);
  await expect(
    page.getByRole('heading', { name: `Acceptance game ${suffix}`, exact: true })
  ).toBeVisible();
  await page.reload();
  await expect(page.getByRole('status', { name: 'Loading GodForge' })).toHaveCount(0);
  await expect(page.getByText('Real local browser-to-API workspace acceptance')).toBeVisible();
  await expect(page.getByText('projectOwner', { exact: true })).toBeVisible();
  await page.screenshot({ path: '../artifacts/workspace-live-desktop.png', fullPage: true });
  // Read back authoritative membership-scoped list through the browser UI.
  await page.getByRole('navigation', { name: 'Breadcrumb' }).getByRole('link').click();
  await expect(page.getByRole('list', { name: 'Projects', exact: true }).getByRole('link')).toHaveText(
    `Acceptance game ${suffix}`
  );
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: '../artifacts/workspace-live-mobile.png', fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.locator('summary').click();
  await page.getByRole('link', { name: 'Security sessions', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Active and recent sessions' })).toBeVisible();
});

test('TC-WORKSPACE-LIVE-002: end-to-end organization invitations via Mailpit, role management, ownership transfer, member revocation, and deletion request', async ({
  browser,
  page,
  request,
}) => {
  const suffix = randomUUID().replaceAll('-', '').slice(0, 14);
  const ownerEmail = `owner-${suffix}@example.test`;
  const collabEmail = `collab-${suffix}@example.test`;
  const password = `CollabPass1!-${suffix}`;

  await registerVerifiedUser(request, ownerEmail, password, `Owner ${suffix}`);
  await registerVerifiedUser(request, collabEmail, password, `Collaborator ${suffix}`);

  // 1. Owner signs in and creates organization
  await page.goto('/login');
  await page.getByLabel('Email address').fill(ownerEmail);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Create your first organization' })).toBeVisible();

  await page.getByLabel('Name', { exact: true }).fill(`Collab Studio ${suffix}`);
  await page.getByLabel('Slug', { exact: true }).fill(`collab-${suffix}`);
  const orgCreatedPromise = page.waitForResponse(
    (r) => r.url() === `${api}/organizations` && r.request().method() === 'POST'
  );
  await page.getByRole('button', { name: 'Create organization', exact: true }).click();
  const orgCreatedRes = await orgCreatedPromise;
  expect(orgCreatedRes.status()).toBe(201);
  const orgId = ((await orgCreatedRes.json()).data as { id: string }).id;

  // 2. Owner navigates to Invitations and sends an invitation to Collaborator
  await page.getByRole('navigation', { name: 'Workspace' }).getByRole('link', { name: 'Invitations' }).click();
  await expect(page.getByRole('heading', { name: 'Invitations', exact: true })).toBeVisible();
  await page.getByLabel('Invitee email').fill(collabEmail);
  await page.getByLabel('Organization role').selectOption('organizationMember');
  await page.getByRole('button', { name: 'Send invitation' }).click();
  await expect(page.getByText(`Invitation sent to ${collabEmail}`)).toBeVisible();

  // 3. Poll Mailpit for the real invitation email sent by GodForge.Worker
  let inviteMessageId = '';
  await expect
    .poll(
      async () => {
        const res = await request.get(`${mailbox}/api/v1/messages?limit=100`);
        const list = (await res.json()) as {
          messages: { ID: string; To: { Address: string }[]; Subject: string }[];
        };
        inviteMessageId =
          list.messages.find(
            (m) =>
              m.Subject === 'GodForge organization invitation' &&
              m.To.some((to) => to.Address === collabEmail)
          )?.ID ?? '';
        return !!inviteMessageId;
      },
      { timeout: 45_000 }
    )
    .toBe(true);

  const inviteEmailBody = (await (
    await request.get(`${mailbox}/api/v1/message/${inviteMessageId}`)
  ).json()) as { HTML: string };
  const hrefMatch = inviteEmailBody.HTML.match(/href="([^"]+organization-invitations\/accept\?token=[^"]+)"/);
  expect(hrefMatch?.[1]).toBeTruthy();
  const invitationUrl = new URL(hrefMatch![1]!.replaceAll('&amp;', '&'));
  const acceptPathWithQuery = `${invitationUrl.pathname}${invitationUrl.search}`;

  // 4. Collaborator opens invitation link in isolated browser context, signs in, and accepts
  const collabContext = await browser.newContext();
  const collabPage = await collabContext.newPage();
  try {
    await collabPage.goto(acceptPathWithQuery);
    await expect(collabPage).toHaveURL(
      /\/login\?returnTo=(?:%2F|\/)organization-invitations(?:%2F|\/)accept$/
    );

    await collabPage.getByLabel('Email address').fill(collabEmail);
    await collabPage.getByLabel('Password', { exact: true }).fill(password);
    await collabPage.getByRole('button', { name: 'Sign in', exact: true }).click();

    await expect(collabPage).toHaveURL(/\/organization-invitations\/accept$/);
    const rawInviteToken = invitationUrl.searchParams.get('token')!;
    expect(rawInviteToken).toBeTruthy();
    const stored = await collabPage.evaluate(() =>
      JSON.stringify({ ...window.localStorage, ...window.sessionStorage })
    );
    expect(stored).not.toContain(rawInviteToken);
    expect(stored).not.toContain(password);

    await collabPage.getByRole('button', { name: 'Accept invitation' }).click();
    await expect(
      collabPage.getByRole('heading', { name: `Joined Collab Studio ${suffix}` })
    ).toBeVisible();
    await collabPage.getByRole('link', { name: 'Open organization projects' }).click();
    await expect(collabPage).toHaveURL(new RegExp(`/organizations/${orgId}/projects$`));

    // Collaborator is organizationMember -> no administrative controls on Members page
    await collabPage
      .getByRole('navigation', { name: 'Workspace' })
      .getByRole('link', { name: 'Members' })
      .click();
    await expect(collabPage.getByRole('button', { name: 'Manage role' })).toHaveCount(0);

    // 5. Owner promotes Collaborator to organizationAdmin
    await page
      .getByRole('navigation', { name: 'Workspace' })
      .getByRole('link', { name: 'Members' })
      .click();
    const collabMemberRow = page
      .getByRole('listitem')
      .filter({ hasText: `Collaborator ${suffix}` });
    await expect(collabMemberRow).toBeVisible();
    await collabMemberRow.getByRole('button', { name: 'Manage role' }).click();
    await collabMemberRow.locator('select[id^="role-"]').selectOption('organizationAdmin');
    await collabMemberRow.getByRole('button', { name: 'Review change' }).click();
    await collabMemberRow.getByRole('button', { name: 'Confirm update' }).click();
    await expect(page.getByText(`Updated membership for Collaborator ${suffix}.`)).toBeVisible();

    // 6. Owner updates general settings and transfers ownership to Collaborator
    await page
      .getByRole('navigation', { name: 'Workspace' })
      .getByRole('link', { name: 'Settings' })
      .click();
    const updatedSlug = `collab-renamed-${suffix}`;
    await page.getByLabel('Organization name').fill(`Renamed Studio ${suffix}`);
    await page.getByLabel('Organization slug').fill(updatedSlug);
    await page.getByRole('button', { name: 'Save general settings' }).click();
    await expect(page.getByText('Saved organization settings (version 2).')).toBeVisible();

    await page.getByLabel('Search active members').fill(`Collaborator ${suffix}`);
    await page.getByRole('button', { name: 'Search candidates' }).click();
    await page.getByLabel('Retain your membership as').selectOption('organizationAdmin');
    await page.getByRole('button', { name: 'Review ownership transfer' }).click();
    await page.getByRole('button', { name: 'Confirm ownership transfer' }).click();
    await expect(
      page.getByText(`Transferred organization ownership to Collaborator ${suffix}`)
    ).toBeVisible();
    await expect(
      page.getByText('Only an OrganizationOwner can transfer organization ownership.')
    ).toBeVisible();

    // 7. Collaborator (now sole OrganizationOwner) verifies server-enforced LAST_OWNER_REQUIRED on self-leave, then removes previous owner
    await collabPage.reload();
    await expect(
      collabPage.getByText('organizationOwner · active', { exact: true })
    ).toBeVisible();
    const collabSelfRow = collabPage
      .getByRole('listitem')
      .filter({ hasText: `Collaborator ${suffix}` });
    await collabSelfRow.getByRole('button', { name: 'Leave organization' }).click();
    await collabPage.getByRole('button', { name: 'Confirm leave' }).click();
    await expect(
      collabPage.getByText(
        'At least one active owner is required for this organization and its projects.'
      )
    ).toBeVisible();

    const ownerMemberRow = collabPage
      .getByRole('listitem')
      .filter({ hasText: `Owner ${suffix}` });
    await ownerMemberRow.getByRole('button', { name: 'Remove member' }).click();
    await collabPage.getByRole('button', { name: 'Confirm removal' }).click();
    await expect(collabPage.getByText(`Removed Owner ${suffix} from the organization.`)).toBeVisible();

    // Removed user immediately loses access to the organization
    await page.reload();
    await expect(page.getByRole('alert')).toContainText(
      'This workspace is unavailable or you no longer have access.'
    );

    // 8. Collaborator (OrganizationOwner) requests organization deletion in Settings -> Danger zone
    await collabPage
      .getByRole('navigation', { name: 'Workspace' })
      .getByRole('link', { name: 'Settings' })
      .click();
    await collabPage.getByLabel(new RegExp(`Type ${updatedSlug} to confirm`)).fill(updatedSlug);
    await collabPage.getByRole('button', { name: 'Request organization deletion' }).click();
    await collabPage.getByRole('button', { name: 'Confirm deletion request' }).click();
    await expect(collabPage.getByText('Lifecycle status is now deleting')).toBeVisible();
  } finally {
    await collabContext.close();
  }
});
