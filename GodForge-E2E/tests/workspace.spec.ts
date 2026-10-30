import { test, expect, type BrowserContext } from '@playwright/test';

const stamp = '2026-10-01T00:00:00Z';
const first = {
  id: 'a',
  name: 'Studio A',
  slug: 'studio-a',
  currentUserRole: 'organizationOwner',
  status: 'active',
  createdAt: stamp,
  updatedAt: stamp,
  version: 1,
};
const second = {
  ...first,
  id: 'b',
  name: 'Studio B',
  slug: 'studio-b',
  currentUserRole: 'organizationMember',
};

async function contract(context: BrowserContext, onboarding = false) {
  const state = {
    organizations: onboarding ? ([] as typeof first[]) : [{ ...first }, { ...second }],
    projects: [] as Record<string, unknown>[],
    members: {
      a: [
        {
          userId: 'user',
          email: 'workspace@example.test',
          displayName: 'Workspace user',
          role: 'organizationOwner',
          status: 'active',
          joinedAt: stamp,
          version: 1,
        },
        {
          userId: 'member-2',
          email: 'collaborator@example.test',
          displayName: 'Collaborator Two',
          role: 'organizationMember',
          status: 'active',
          joinedAt: stamp,
          version: 1,
        },
      ],
      b: [
        {
          userId: 'user',
          email: 'workspace@example.test',
          displayName: 'Workspace user',
          role: 'organizationMember',
          status: 'active',
          joinedAt: stamp,
          version: 1,
        },
      ],
    } as Record<string, Array<Record<string, unknown>>>,
    invitations: [
      {
        id: 'inv-1',
        email: 'existing-invite@example.test',
        role: 'organizationMember',
        status: 'pending',
        expiresAt: '2026-10-08T00:00:00Z',
        invitedByUserId: 'user',
        createdAt: stamp,
        version: 1,
      },
    ] as Array<Record<string, unknown>>,
    validInviteToken: 'valid-invitation-token-123',
    fail: false,
    conflict: false,
  };

  await context.route('**/api/v1/**', async (route) => {
    const url = new URL(route.request().url());
    const path = url.pathname.replace('/api/v1', '');
    const method = route.request().method();
    if (path === '/users/me/settings') return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ data: { theme: 'dark', notificationInApp: true, notificationEmail: true, notificationDigest: 'off', version: 1, createdAt: stamp, updatedAt: stamp } }) });
    if (/^\/organizations\/[^/]+\/settings$/.test(path)) return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ data: { organizationId: path.split('/')[2], aiAdvisoryAllowed: true, platformAiAdvisoryAllowed: false, version: 1, createdAt: stamp, updatedAt: stamp } }) });
    const reply = (data: unknown, status = 200, meta?: unknown) =>
      route.fulfill({
        status,
        contentType: 'application/json',
        body: JSON.stringify({ data, meta }),
      });
    const page = (data: unknown[]) =>
      reply(data, 200, { page: 1, pageSize: 20, totalCount: data.length });

    if (path === '/auth/refresh') {
      return reply({
        user: {
          id: 'user',
          email: 'workspace@example.test',
          displayName: 'Workspace user',
          status: 'active',
          emailVerifiedAt: stamp,
          createdAt: stamp,
          version: 1,
        },
        accessToken: 'controlled-memory-token',
        accessTokenExpiresAt: '2099-01-01T00:00:00Z',
        refreshTokenExpiresAt: '2099-01-01T00:00:00Z',
        session: {
          id: 'session',
          expiresAt: '2099-01-01T00:00:00Z',
          createdAt: stamp,
          current: true,
        },
      });
    }

    if (path === '/organizations' && method === 'GET') return page(state.organizations);
    if (path === '/organizations' && method === 'POST') {
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      state.organizations = [{ ...first, ...route.request().postDataJSON() }];
      return reply(state.organizations[0], 201);
    }

    if (path === '/projects' && method === 'GET') {
      if (state.fail) return reply(undefined, 503);
      return page(
        state.projects.filter((p) => p.organizationId === url.searchParams.get('organizationId'))
      );
    }

    if (path === '/organizations/a/projects' && method === 'POST') {
      if (state.conflict) {
        return route.fulfill({
          status: 409,
          contentType: 'application/json',
          body: JSON.stringify({ error: { code: 'CONCURRENCY_CONFLICT' } }),
        });
      }
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      const project = {
        ...route.request().postDataJSON(),
        id: 'p',
        organizationId: 'a',
        currentUserRole: 'projectOwner',
        status: 'active',
        createdAt: stamp,
        updatedAt: stamp,
        version: 1,
      };
      state.projects.push(project);
      return reply(project, 201);
    }

    if (path === '/organizations/a/projects') {
      return page([
        {
          id: 'other',
          organizationId: 'a',
          name: 'Catalog only',
          slug: 'catalog',
          status: 'active',
          memberCount: 2,
          ownerCount: 1,
          createdAt: stamp,
          version: 1,
        },
      ]);
    }

    if (path === '/projects/p') return reply(state.projects.find((p) => p.id === 'p'));

    // Organization Members endpoints
    if ((path === '/organizations/a/members' || path === '/organizations/b/members') && method === 'GET') {
      const orgId = path.includes('/organizations/a/') ? 'a' : 'b';
      const list = state.members[orgId] ?? [];
      const roleParam = url.searchParams.get('role');
      const statusParam = url.searchParams.get('status');
      const searchParam = url.searchParams.get('search')?.toLowerCase();
      const filtered = list.filter((m) => {
        if (roleParam && m.role !== roleParam) return false;
        if (statusParam && m.status !== statusParam) return false;
        if (!statusParam && m.status === 'removed') return false;
        if (
          searchParam &&
          !String(m.displayName).toLowerCase().includes(searchParam) &&
          !String(m.email).toLowerCase().includes(searchParam)
        ) {
          return false;
        }
        return true;
      });
      return page(filtered);
    }

    if (path.startsWith('/organizations/a/members/') && method === 'PATCH') {
      if (state.conflict) {
        return route.fulfill({
          status: 409,
          contentType: 'application/json',
          body: JSON.stringify({ error: { code: 'CONCURRENCY_CONFLICT' } }),
        });
      }
      const userId = path.split('/').pop()!;
      const body = route.request().postDataJSON() as {
        role: string;
        status: string;
        version: number;
      };
      const target = state.members.a?.find((m) => m.userId === userId);
      if (!target) {
        return route.fulfill({
          status: 404,
          contentType: 'application/json',
          body: JSON.stringify({ error: { code: 'MEMBERSHIP_NOT_FOUND' } }),
        });
      }
      target.role = body.role;
      target.status = body.status;
      target.version = Number(target.version) + 1;
      return reply(target);
    }

    if (path.startsWith('/organizations/a/members/') && method === 'DELETE') {
      const userId = path.split('/').pop()!;
      const target = state.members.a?.find((m) => m.userId === userId);
      if (target) target.status = 'removed';
      return route.fulfill({ status: 204 });
    }

    // Organization Invitations endpoints
    if (path === '/organizations/a/invitations' && method === 'GET') {
      const statusParam = url.searchParams.get('status');
      const filtered = state.invitations.filter((i) => !statusParam || i.status === statusParam);
      return page(filtered);
    }

    if (path === '/organizations/a/invitations' && method === 'POST') {
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      const body = route.request().postDataJSON() as { email: string; role: string };
      const created = {
        id: `inv-${state.invitations.length + 1}`,
        email: body.email,
        role: body.role,
        status: 'pending',
        expiresAt: '2026-10-08T00:00:00Z',
        invitedByUserId: 'user',
        createdAt: stamp,
        version: 1,
      };
      state.invitations.unshift(created);
      return reply(created, 201);
    }

    if (path.startsWith('/organizations/a/invitations/') && method === 'DELETE') {
      const invId = path.split('/').pop()!;
      const inv = state.invitations.find((i) => i.id === invId);
      if (inv) inv.status = 'revoked';
      return route.fulfill({ status: 204 });
    }

    if (path === '/organization-invitations/accept' && method === 'POST') {
      const body = route.request().postDataJSON() as { token: string };
      if (body.token !== state.validInviteToken) {
        return route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ error: { code: 'INVITE_INVALID_OR_EXPIRED' } }),
        });
      }
      state.validInviteToken = ''; // one-time token consumption
      const joinedOrg = {
        id: 'c',
        name: 'Invited Studio C',
        slug: 'invited-studio-c',
        currentUserRole: 'organizationMember',
        status: 'active',
        createdAt: stamp,
        updatedAt: stamp,
        version: 1,
      };
      state.organizations.push(joinedOrg);
      return reply({
        organization: joinedOrg,
        membership: {
          userId: 'user',
          email: 'workspace@example.test',
          displayName: 'Workspace user',
          role: 'organizationMember',
          status: 'active',
          joinedAt: stamp,
          version: 1,
        },
      });
    }

    // Organization settings, ownership transfer, and deletion
    if (path === '/organizations/a' && method === 'PATCH') {
      if (state.conflict) {
        return route.fulfill({
          status: 409,
          contentType: 'application/json',
          body: JSON.stringify({ error: { code: 'CONCURRENCY_CONFLICT' } }),
        });
      }
      const body = route.request().postDataJSON() as { name: string; slug: string; version: number };
      const org = state.organizations.find((o) => o.id === 'a')!;
      org.name = body.name;
      org.slug = body.slug;
      org.version += 1;
      return reply(org);
    }

    if (path === '/organizations/a/transfer-ownership' && method === 'POST') {
      if (state.conflict) {
        return route.fulfill({
          status: 409,
          contentType: 'application/json',
          body: JSON.stringify({ error: { code: 'CONCURRENCY_CONFLICT' } }),
        });
      }
      const body = route.request().postDataJSON() as {
        newOwnerUserId: string;
        retainCurrentOwnerAs: string;
      };
      const org = state.organizations.find((o) => o.id === 'a')!;
      org.currentUserRole = body.retainCurrentOwnerAs;
      org.version += 1;
      const previousOwner = state.members.a!.find((m) => m.userId === 'user')!;
      const newOwner = state.members.a!.find((m) => m.userId === body.newOwnerUserId)!;
      previousOwner.role = body.retainCurrentOwnerAs;
      newOwner.role = 'organizationOwner';
      return reply({ previousOwner, newOwner });
    }

    if (path === '/organizations/a' && method === 'DELETE') {
      const org = state.organizations.find((o) => o.id === 'a')!;
      org.status = 'deleting';
      org.version += 1;
      return reply({ resourceId: 'a', status: 'deleting', version: org.version }, 202);
    }

    const org = state.organizations.find((o) => path === `/organizations/${o.id}`);
    if (org) return reply(org);

    return route.fulfill({
      status: 404,
      contentType: 'application/json',
      body: JSON.stringify({ error: { code: 'PROJECT_NOT_FOUND' } }),
    });
  });
  return state;
}

test('TC-PROJECT-BROWSER-001: project navigation, paginated member search and role confirmation', async ({ page, context }) => {
  const state = await contract(context);
  state.projects.push({ id: 'p', organizationId: 'a', name: 'Managed game', slug: 'managed', description: 'Real metadata', visibility: 'private', currentUserRole: 'projectOwner', status: 'active', createdAt: stamp, updatedAt: stamp, version: 7 });
  let memberRole = 'developer';
  await context.route('**/api/v1/projects/p/members**', async route => {
    if (route.request().method() === 'PATCH') {
      expect(route.request().postDataJSON()).toEqual({ role: 'reviewer', version: 4 }); memberRole = 'reviewer';
    }
    await route.fulfill({ contentType: 'application/json', body: JSON.stringify({ data: route.request().method() === 'PATCH' ? {} : [{ userId: 'member-2', displayName: 'Collaborator Two', email: 'collaborator@example.test', role: memberRole, status: 'active', joinedAt: stamp, version: 4 }], meta: { page: 1, pageSize: 20, totalCount: 121 } }) });
  });
  await page.goto('/organizations/a/projects/p');
  await page.getByRole('navigation', { name: 'Project sections' }).getByRole('link', { name: 'Members', exact: true }).click();
  await expect(page.getByRole('navigation', { name: 'Project sections' }).getByRole('link', { name: 'Members', exact: true })).toHaveAttribute('aria-current', 'page');
  const nextRequest = page.waitForRequest(r => r.url().includes('/projects/p/members?') && new URL(r.url()).searchParams.get('page') === '2');
  await page.getByRole('button', { name: 'Next', exact: true }).click(); await nextRequest;
  await page.getByLabel('Search members', { exact: true }).fill('collaborator');
  const searchRequest = page.waitForRequest(r => new URL(r.url()).searchParams.get('search') === 'collaborator');
  await page.getByRole('button', { name: 'Search', exact: true }).click(); await searchRequest;
  await page.getByRole('button', { name: 'Change role', exact: true }).click(); await page.getByLabel('Project role', { exact: true }).selectOption('reviewer');
  await page.getByRole('button', { name: 'Confirm role change', exact: true }).click(); await expect(page.getByText('Project role updated.', { exact: true })).toBeVisible();
});

test('TC-PROJECT-BROWSER-002: metadata conflict retains edits and archived project restores with current version', async ({ page, context }) => {
  const state = await contract(context);
  const project = { id: 'p', organizationId: 'a', name: 'Managed game', slug: 'managed', description: '', visibility: 'private', currentUserRole: 'projectOwner', status: 'active', createdAt: stamp, updatedAt: stamp, version: 7 };
  state.projects.push(project);
  await context.route('**/api/v1/projects/p/settings', route => route.fulfill({ contentType: 'application/json', body: JSON.stringify({ data: { projectId: 'p', analysisProfileKey: 'current-default-v1', aiAdvisoryEnabled: false, defaultAssetVisibility: 'private', notificationPolicyVersion: 1, sourceScope: 'project', version: 2 } }) }));
  await context.route('**/api/v1/projects/p/members**', route => route.fulfill({ contentType: 'application/json', body: JSON.stringify({ data: [], meta: { page: 1, pageSize: 20, totalCount: 0 } }) }));
  await context.route('**/api/v1/projects/p', async route => {
    if (route.request().method() === 'PATCH') {
      project.version = 8;
      await route.fulfill({ status: 409, contentType: 'application/json', body: JSON.stringify({ error: { code: 'CONCURRENCY_CONFLICT' } }) }); return;
    }
    await route.fulfill({ contentType: 'application/json', body: JSON.stringify({ data: project }) });
  });
  await context.route('**/api/v1/projects/p/restore', async route => {
    expect(route.request().postDataJSON()).toEqual({ version: 8 }); project.status = 'active'; project.version = 9;
    await route.fulfill({ contentType: 'application/json', body: JSON.stringify({ data: project }) });
  });
  await page.goto('/organizations/a/projects/p/settings'); await page.getByLabel('Project name', { exact: true }).fill('Preserved local edit');
  await page.getByRole('button', { name: 'Save general settings', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('submit again deliberately'); await expect(page.getByLabel('Project name', { exact: true })).toHaveValue('Preserved local edit');
  project.status = 'archived'; await page.reload(); await expect(page.getByRole('button', { name: 'Save general settings', exact: true })).toBeDisabled();
  await page.getByRole('button', { name: 'Restore project', exact: true }).click(); await page.getByRole('button', { name: 'Confirm restore', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Save general settings', exact: true })).toBeEnabled(); await expect(page.getByText('Project restored.', { exact: true })).toBeVisible();
});

test('TC-WORKSPACE-BROWSER-001: onboarding creates organization then project with actual metadata', async ({
  page,
  context,
}) => {
  await contract(context, true);
  await page.goto('/dashboard');
  await expect(page.getByRole('heading', { name: 'Create your first organization' })).toBeVisible();
  await page.getByLabel('Name', { exact: true }).fill('My Studio');
  await page.getByLabel('Slug', { exact: true }).fill('my-studio');
  await page.getByRole('button', { name: 'Create organization', exact: true }).click();
  await expect(page).toHaveURL(/\/organizations\/a\/projects$/);
  await page.getByRole('button', { name: 'New project' }).click();
  await page.getByLabel('Name', { exact: true }).fill('My Game');
  await page.getByLabel('Slug', { exact: true }).fill('my-game');
  await page.getByLabel('Description').fill('A Godot project');
  await page.getByRole('button', { name: 'Create project', exact: true }).click();
  await expect(page).toHaveURL(/\/organizations\/a\/projects\/p$/);
  await expect(page.getByRole('heading', { name: 'My Game', exact: true })).toBeVisible();
  await expect(page.getByText('A Godot project')).toBeVisible();
  await page.reload();
  await expect(page.getByText('projectOwner', { exact: true })).toBeVisible();
});

test('TC-WORKSPACE-BROWSER-002: organization switch clears context and respects member role', async ({
  page,
  context,
}) => {
  await contract(context);
  await page.goto('/organizations/a/projects');
  await expect(page.getByRole('button', { name: 'New project' })).toBeVisible();
  await page.getByLabel('Organization', { exact: true }).selectOption('b');
  await expect(page).toHaveURL(/\/organizations\/b\/projects$/);
  await expect(page.getByText('Studio B / studio-b')).toBeVisible();
  await expect(page.getByRole('button', { name: 'New project' })).toHaveCount(0);
  await expect(page.getByText('Ask a project owner')).toBeVisible();
});

test('TC-WORKSPACE-BROWSER-003: catalog metadata has no content link; errors retry', async ({
  page,
  context,
}) => {
  const state = await contract(context);
  state.fail = true;
  await page.goto('/organizations/a/projects');
  await expect(page.getByRole('alert')).toContainText('retry');
  state.fail = false;
  await page.getByRole('button', { name: 'Retry', exact: true }).click();
  await page.getByRole('button', { name: 'Organization catalog' }).click();
  await expect(page.getByText('Catalog only', { exact: true })).toBeVisible();
  await expect(page.getByRole('list', { name: 'Projects', exact: true }).getByRole('link')).toHaveCount(
    0
  );
});

test('TC-WORKSPACE-BROWSER-004: concurrency conflict preserves form; masked detail hides content', async ({
  page,
  context,
}) => {
  const state = await contract(context);
  state.conflict = true;
  await page.goto('/organizations/a/projects');
  await page.getByRole('button', { name: 'New project' }).click();
  await page.getByLabel('Name', { exact: true }).fill('Game');
  await page.getByLabel('Slug', { exact: true }).fill('game');
  await page.getByRole('button', { name: 'Create project', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('workspace changed');
  await expect(page.getByLabel('Name', { exact: true })).toHaveValue('Game');
  await page.goto('/organizations/a/projects/unknown');
  await expect(page.getByRole('alert')).toContainText('unavailable');
  await expect(page.getByRole('heading', { name: 'Project overview' })).toHaveCount(0);
});

test('TC-WORKSPACE-BROWSER-005: mobile navigation and keyboard organization selection', async ({
  page,
  context,
}) => {
  await contract(context);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/dashboard');
  await expect(page.getByLabel('Organization', { exact: true })).toBeHidden();
  await page.getByRole('button', { name: 'Navigation' }).click();
  const switcher = page.getByLabel('Organization', { exact: true });
  await switcher.focus();
  await switcher.selectOption('a');
  await expect(page).toHaveURL(/\/organizations\/a\/projects$/);
  await expect(page.getByLabel('Organization', { exact: true })).toBeHidden();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});

test('TC-WORKSPACE-BROWSER-006: organization members role management, removal, and tenant switching', async ({
  page,
  context,
}) => {
  await contract(context);
  await page.goto('/organizations/a/members');
  await expect(page.getByRole('heading', { name: 'Members', exact: true })).toBeVisible();
  await expect(page.getByText('Collaborator Two')).toBeVisible();

  // Promote Collaborator Two to organizationAdmin
  await page.getByRole('button', { name: 'Manage role' }).last().click();
  await page.locator('#role-member-2').selectOption('organizationAdmin');
  await page.getByRole('button', { name: 'Review change' }).click();
  await page.getByRole('button', { name: 'Confirm update' }).click();
  await expect(page.getByText('Updated membership for Collaborator Two.')).toBeVisible();

  // Remove Collaborator Two
  await page.getByRole('button', { name: 'Remove member' }).click();
  await page.getByRole('button', { name: 'Confirm removal' }).click();
  await expect(page.getByText('Removed Collaborator Two from the organization.')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Collaborator Two' })).toHaveCount(0);

  // Switch organization from 'a' to 'b' while on Members view
  await page.getByLabel('Organization', { exact: true }).selectOption('b');
  await expect(page).toHaveURL(/\/organizations\/b\/members$/);
  await expect(page.getByText('Studio B / studio-b')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Manage role' })).toHaveCount(0);
});

test('TC-WORKSPACE-BROWSER-007: invitations creation, revocation, acceptance, and invalid token handling', async ({
  page,
  context,
}) => {
  await contract(context);
  await page.goto('/organizations/a/invitations');
  await expect(page.getByRole('heading', { name: 'Invitations', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'existing-invite@example.test' })).toBeVisible();

  // Create new invitation
  await page.getByLabel('Invitee email').fill('new-dev@example.test');
  await page.getByLabel('Organization role').selectOption('organizationAdmin');
  await page.getByRole('button', { name: 'Send invitation' }).click();
  await expect(page.getByText('Invitation sent to new-dev@example.test (organizationAdmin).')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'new-dev@example.test' })).toBeVisible();

  // Revoke existing invitation
  await page.getByRole('button', { name: 'Revoke invitation' }).last().click();
  await page.getByRole('button', { name: 'Confirm revoke' }).click();
  await expect(
    page.getByText('Revoked invitation for existing-invite@example.test.')
  ).toBeVisible();

  // Accept valid invitation and verify token is scrubbed from URL
  await page.goto('/organization-invitations/accept?token=valid-invitation-token-123');
  await expect(page).toHaveURL(/\/organization-invitations\/accept$/);
  await page.getByRole('button', { name: 'Accept invitation' }).click();
  await expect(page.getByRole('heading', { name: 'Joined Invited Studio C' })).toBeVisible();

  // Reused or invalid token fails cleanly
  await page.goto('/organization-invitations/accept?token=valid-invitation-token-123');
  await page.getByRole('button', { name: 'Accept invitation' }).click();
  await expect(page.getByRole('alert')).toContainText('invalid, expired, revoked');
});

test('TC-WORKSPACE-BROWSER-008: settings update, ownership transfer, deletion request, and member permission gating', async ({
  page,
  context,
}) => {
  await contract(context);
  await page.goto('/organizations/a/settings');

  // Verify candidate search in Ownership transfer section
  await page.getByLabel('Search active members').fill('Collaborator Two');
  await page.getByRole('button', { name: 'Search candidates' }).click();
  await expect(page.getByLabel('New organization owner')).toHaveValue('member-2');

  // Update general settings (including 1-char boundary then standard studio name)
  await page.getByLabel('Organization name').fill('A');
  await page.getByLabel('Organization slug').fill('a');
  await page.getByRole('button', { name: 'Save general settings' }).click();
  await expect(page.getByText('Saved organization settings (version 2).')).toBeVisible();

  await page.getByLabel('Organization name').fill('Studio A Prime');
  await page.getByLabel('Organization slug').fill('studio-a-prime');
  await page.getByRole('button', { name: 'Save general settings' }).click();
  await expect(page.getByText('Saved organization settings (version 3).')).toBeVisible();

  // Request organization deletion
  await page.getByLabel(/Type studio-a-prime to confirm/).fill('studio-a-prime');
  await page.getByRole('button', { name: 'Request organization deletion' }).click();
  await page.getByRole('button', { name: 'Confirm deletion request' }).click();
  await expect(page.getByText('Lifecycle status is now deleting')).toBeVisible();

  // Direct route navigation to settings/invitations as an OrganizationMember on Studio B shows permission required
  await page.goto('/organizations/b/settings');
  await expect(page.getByRole('alert')).toContainText(
    'Only an OrganizationOwner or OrganizationAdmin can access organization settings.'
  );
  await page.goto('/organizations/b/invitations');
  await expect(page.getByRole('alert')).toContainText(
    'Only an OrganizationOwner or OrganizationAdmin can view and manage organization invitations.'
  );
});
