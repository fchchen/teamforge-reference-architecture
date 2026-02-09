import { test, expect, Page } from '@playwright/test';
import * as path from 'path';

/**
 * Automated screenshot capture for TeamForge Reference Architecture.
 *
 * Prerequisites:
 *   1. Angular dev server running:  cd client && npm start        (port 4200)
 *   2. .NET API server running:     cd src && dotnet run --project TeamForge.Api  (port 5210)
 *
 * Run:
 *   cd client && npx playwright test e2e/screenshots.spec.ts
 *
 * Output:
 *   docs/screenshots/*.png
 */

const screenshotDir = path.resolve(__dirname, '../../docs/screenshots');

async function screenshot(page: Page, name: string) {
  await page.screenshot({
    path: path.join(screenshotDir, `${name}.png`),
    fullPage: true,
  });
}

async function waitForContent(page: Page) {
  // Wait for any loading spinners to disappear
  await page.waitForTimeout(500);
  const spinner = page.locator('mat-spinner');
  if (await spinner.isVisible().catch(() => false)) {
    await spinner.waitFor({ state: 'hidden', timeout: 10000 });
  }
  // Extra settle time for Angular rendering
  await page.waitForTimeout(300);
}

// ─── Public Pages ───────────────────────────────────────────────

test.describe('Screenshots — Public Pages', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
  });

  test('01 — Login page', async ({ page }) => {
    await page.goto('/login');
    await expect(page.locator('mat-card-title')).toContainText('TeamForge');
    await screenshot(page, '01-login');
  });

  test('02 — Register page', async ({ page }) => {
    await page.goto('/register');
    await expect(page.locator('mat-card-title')).toContainText('Create Your Workspace');
    await screenshot(page, '02-register');
  });
});

// ─── Acme Corp Tenant ───────────────────────────────────────────

test.describe('Screenshots — Acme Corp', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.getByRole('button', { name: 'Acme Corp' }).click();
    await page.waitForURL(/\/dashboard/);
    await waitForContent(page);
  });

  test('03 — Dashboard', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Acme Corp');
    await screenshot(page, '03-dashboard-acme');
  });

  test('04 — Projects page', async ({ page }) => {
    await page.getByRole('link', { name: 'Projects' }).click();
    await expect(page).toHaveURL(/\/projects/);
    await waitForContent(page);
    await screenshot(page, '04-projects-acme');
  });

  test('05 — Teams page', async ({ page }) => {
    await page.getByRole('link', { name: 'Teams' }).click();
    await expect(page).toHaveURL(/\/teams/);
    await waitForContent(page);
    await screenshot(page, '05-teams-acme');
  });

  test('06 — Settings page (admin)', async ({ page }) => {
    await page.locator('button[mat-icon-button]').click();
    await page.getByRole('menuitem', { name: /Settings/ }).click();
    await expect(page).toHaveURL(/\/settings/);
    await waitForContent(page);
    await screenshot(page, '06-settings-acme');
  });

  test('07 — Toolbar user menu', async ({ page }) => {
    await page.locator('button[mat-icon-button]').click();
    await page.waitForTimeout(300);
    await screenshot(page, '07-user-menu-acme');
  });
});

// ─── Pixel Studio Tenant (whitelabeling comparison) ─────────────

test.describe('Screenshots — Pixel Studio', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.getByRole('button', { name: 'Pixel Studio' }).click();
    await page.waitForURL(/\/dashboard/);
    await waitForContent(page);
  });

  test('08 — Dashboard (different branding)', async ({ page }) => {
    await screenshot(page, '08-dashboard-pixel');
  });

  test('09 — Projects page', async ({ page }) => {
    await page.getByRole('link', { name: 'Projects' }).click();
    await expect(page).toHaveURL(/\/projects/);
    await waitForContent(page);
    await screenshot(page, '09-projects-pixel');
  });
});

// ─── GreenLeaf Tenant (third whitelabeling variant) ─────────────

test.describe('Screenshots — GreenLeaf', () => {
  test('10 — Dashboard', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.getByRole('button', { name: 'GreenLeaf' }).click();
    await page.waitForURL(/\/dashboard/);
    await waitForContent(page);
    await screenshot(page, '10-dashboard-greenleaf');
  });
});

// ─── Swagger API Documentation ──────────────────────────────────

test.describe('Screenshots — Swagger', () => {
  test('11 — Swagger UI overview', async ({ page }) => {
    await page.goto('http://localhost:5210/swagger/index.html');
    await page.waitForSelector('.swagger-ui', { timeout: 10000 });
    await page.waitForTimeout(1000);
    await screenshot(page, '11-swagger-overview');
  });

  test('12 — Swagger Auth login endpoint expanded', async ({ page }) => {
    await page.goto('http://localhost:5210/swagger/index.html');
    await page.waitForSelector('.swagger-ui', { timeout: 10000 });
    await page.waitForTimeout(1000);

    // Click the POST /api/v1/Auth/login endpoint to expand it
    const loginEndpoint = page.locator('#operations-Auth-post_api_v1_Auth_login .opblock-summary');
    if (await loginEndpoint.isVisible().catch(() => false)) {
      await loginEndpoint.click();
      await page.waitForTimeout(500);
      // Scroll to the expanded endpoint
      await loginEndpoint.scrollIntoViewIfNeeded();
    }
    await screenshot(page, '12-swagger-auth-login-detail');
  });
});
