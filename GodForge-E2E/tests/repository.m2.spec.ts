import { test, expect, type Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';

test.beforeAll(() => {
  if (process.env.GODFORGE_M2_BROWSER !== 'isolated-compose') throw new Error('Use npm.cmd run test:m2:acceptance with the owned disposable provider.');
});
async function signIn(page: Page, email: string) {
  await page.goto('/login');
  await page.getByLabel('Email address').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(process.env.GODFORGE_M2_PASSWORD!);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
}
test('M2 real provider: owner browses branches, commits, directories and Godot text', async ({ page }) => {
  await signIn(page, process.env.GODFORGE_M2_OWNER_EMAIL!);
  await page.goto(process.env.GODFORGE_M2_PROJECT_PATH!);
  await expect(page.getByText('Ready', { exact: true })).toBeVisible();
  await expect(page.getByRole('combobox')).toHaveValue('main');
  await page.getByRole('button', { name: /project.godot/ }).click();
  await expect(page.locator('pre')).toContainText('config_version=5');
  await page.getByRole('button', { name: /addons/ }).click();
  await expect(page.getByRole('button', { name: /helper.gd/ })).toBeVisible();
  await page.getByRole('button', { name: 'commits', exact: true }).click();
  await expect(page.getByText('Godot fixture', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'settings', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Protect selected branch' })).toBeVisible();
});
test('M2 real provider: hosted creation polls durable status and handles an empty repository', async ({ page, request }) => {
  const organizationId = process.env.GODFORGE_M2_PROJECT_PATH!.split('/')[2]!;
  const slug = `browser-${randomUUID().slice(0, 12)}`;
  const response = await request.post(`http://127.0.0.1:35072/api/v1/organizations/${organizationId}/projects`, {
    headers: { Authorization: `Bearer ${process.env.GODFORGE_M2_OWNER_BEARER!}` }, data: { name: slug, slug, visibility: 'private' }
  });
  expect(response.status()).toBe(201);
  const project = (await response.json()).data as { id: string };
  await signIn(page, process.env.GODFORGE_M2_OWNER_EMAIL!);
  await page.goto(`/organizations/${organizationId}/projects/${project.id}/repository`);
  await page.getByLabel('Repository name', { exact: true }).fill('browser-fixture');
  const accepted = page.waitForResponse(r => r.url().endsWith('/repository/hosted') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Create hosted repository', exact: true }).click();
  expect((await accepted).status()).toBe(202);
  await expect(page.getByText('Ready', { exact: true })).toBeVisible({ timeout: 60_000 });
  await expect(page.getByText(/No branches yet/)).toBeVisible();
  await expect(page.getByRole('button', { name: 'Copy clone URL' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Open in Forgejo' })).toHaveAttribute('href', /localhost:33000\/forgejo-service\//);
});
test('M2 real provider: Viewer reads source but cannot configure protection', async ({ page }) => {
  await signIn(page, process.env.GODFORGE_M2_VIEWER_EMAIL!);
  await page.goto(process.env.GODFORGE_M2_PROJECT_PATH!);
  await page.getByRole('button', { name: /player.gd/ }).click();
  await expect(page.locator('pre')).toContainText('extends Node');
  await page.getByRole('button', { name: 'settings', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Protect selected branch' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Synchronize repository' })).toHaveCount(0);
});
