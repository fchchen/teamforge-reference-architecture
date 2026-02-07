import { test, expect } from '@playwright/test';

test.describe('Navigation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.getByRole('button', { name: 'Acme Corp' }).click();
    await page.waitForURL(/\/dashboard/);
  });

  test('toolbar shows nav links', async ({ page }) => {
    await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Projects' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Teams' })).toBeVisible();
  });

  test('navigate to Projects page', async ({ page }) => {
    await page.getByRole('link', { name: 'Projects' }).click();
    await expect(page).toHaveURL(/\/projects/);
    await expect(page.locator('h1')).toContainText('Projects');
  });

  test('navigate to Teams page', async ({ page }) => {
    await page.getByRole('link', { name: 'Teams' }).click();
    await expect(page).toHaveURL(/\/teams/);
    await expect(page.locator('h1')).toContainText('Teams');
  });

  test('navigate to Dashboard via toolbar', async ({ page }) => {
    await page.getByRole('link', { name: 'Projects' }).click();
    await expect(page).toHaveURL(/\/projects/);
    await page.getByRole('link', { name: 'Dashboard' }).click();
    await expect(page).toHaveURL(/\/dashboard/);
    await expect(page.locator('h1')).toBeVisible();
  });

  test('logout redirects to login', async ({ page }) => {
    await page.locator('button[mat-icon-button]').click();
    await page.getByRole('menuitem', { name: /Logout/ }).click();
    await expect(page).toHaveURL(/\/login/);
  });
});
