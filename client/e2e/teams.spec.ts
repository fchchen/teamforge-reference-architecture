import { test, expect } from '@playwright/test';

test.describe('Teams', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.getByRole('button', { name: 'Acme Corp' }).click();
    await page.waitForURL(/\/dashboard/);
    await page.getByRole('link', { name: 'Teams' }).click();
    await expect(page).toHaveURL(/\/teams/);
  });

  test('teams page loads with seeded data', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Teams');
    await expect(page.locator('mat-card').first()).toBeVisible();
  });

  test('create a new team', async ({ page }) => {
    const teamName = `Test Team ${Date.now()}`;

    await page.getByRole('button', { name: 'New Team' }).click();
    await page.getByLabel('Team Name').fill(teamName);
    await page.getByLabel('Description').fill('A test team created by Playwright');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByText(teamName)).toBeVisible();
  });
});
