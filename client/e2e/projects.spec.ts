import { test, expect } from '@playwright/test';

test.describe('Projects', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.getByRole('button', { name: 'Acme Corp' }).click();
    await page.waitForURL(/\/dashboard/);
    await page.getByRole('link', { name: 'Projects' }).click();
    await expect(page).toHaveURL(/\/projects/);
  });

  test('projects page loads with seeded data', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Projects');
    await expect(page.locator('mat-card').first()).toBeVisible();
  });

  test('create a new project', async ({ page }) => {
    const projectName = `Test Project ${Date.now()}`;

    await page.getByRole('button', { name: 'New Project' }).click();
    await page.getByLabel('Project Name').fill(projectName);
    await page.getByLabel('Description').fill('A test project created by Playwright');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByText(projectName)).toBeVisible();
  });

  test('delete a project', async ({ page }) => {
    // First create a project to delete
    const projectName = `Delete Me ${Date.now()}`;
    await page.getByRole('button', { name: 'New Project' }).click();
    await page.getByLabel('Project Name').fill(projectName);
    await page.getByLabel('Description').fill('This project will be deleted');
    await page.getByRole('button', { name: 'Create' }).click();
    await expect(page.getByText(projectName)).toBeVisible();

    // Find the card containing our project and click its delete button
    const projectCard = page.locator('mat-card', { hasText: projectName });
    await projectCard.locator('button[color="warn"]').click();

    await expect(page.getByText(projectName)).not.toBeVisible();
  });
});
