import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test.beforeEach(async ({ page }) => {
    await page.evaluate(() => localStorage.clear());
  });

  test('redirects to login when unauthenticated', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('mat-card-title')).toContainText('TeamForge');
  });

  test('demo button authenticates and redirects to /dashboard', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('button', { name: 'Acme Corp' }).click();
    await page.waitForURL(/\/dashboard/);
    await expect(page.locator('h1')).toContainText('Acme Corp');
  });

  test('login page has three demo tenant buttons', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByRole('button', { name: 'Acme Corp' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Pixel Studio' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'GreenLeaf' })).toBeVisible();
  });

  test('register link navigates to register page', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('link', { name: /Create a new workspace/ }).click();
    await expect(page).toHaveURL(/\/register/);
  });
});

test.describe('Whitelabeling', () => {
  test('different tenants have different primary colors', async ({ page }) => {
    // Login as Acme Corp
    await page.goto('/login');
    await page.getByRole('button', { name: 'Acme Corp' }).click();
    await page.waitForURL(/\/dashboard/);

    const acmePrimary = await page.evaluate(() =>
      getComputedStyle(document.documentElement).getPropertyValue('--tf-primary').trim()
    );

    // Logout
    await page.evaluate(() => localStorage.clear());

    // Login as Pixel Studio
    await page.goto('/login');
    await page.getByRole('button', { name: 'Pixel Studio' }).click();
    await page.waitForURL(/\/dashboard/);

    const pixelPrimary = await page.evaluate(() =>
      getComputedStyle(document.documentElement).getPropertyValue('--tf-primary').trim()
    );

    expect(acmePrimary).not.toBe(pixelPrimary);
  });
});
