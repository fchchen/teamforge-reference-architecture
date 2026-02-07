import { test, expect } from '@playwright/test';

test.describe('Register', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/register');
    await page.evaluate(() => localStorage.clear());
  });

  test('register page has required fields', async ({ page }) => {
    await expect(page.getByLabel('Company Name')).toBeVisible();
    await expect(page.getByLabel('Your Name')).toBeVisible();
    await expect(page.getByLabel('Email')).toBeVisible();
    await expect(page.getByLabel('Password')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Create Workspace' })).toBeVisible();
  });

  test('successful registration redirects', async ({ page }) => {
    const unique = Date.now();
    await page.getByLabel('Company Name').fill(`Test Co ${unique}`);
    await page.getByLabel('Your Name').fill('Test User');
    await page.getByLabel('Email').fill(`test${unique}@example.com`);
    await page.getByLabel('Password').fill('SecurePass123!');
    await page.getByRole('button', { name: 'Create Workspace' }).click();

    await expect(page).toHaveURL(/\/(onboarding|dashboard)/);
  });
});
