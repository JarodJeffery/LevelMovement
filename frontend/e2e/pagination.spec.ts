import { test, expect } from '@playwright/test';

const records = Array.from({ length: 11 }, (_, index) => ({
  registrationNumber: `PAGE${String(index + 1).padStart(3, '0')}`, make: 'Toyota', model: 'Corolla',
  year: 2024, dailyRate: 450, createdDate: '2026-10-10T12:00:00Z', concurrencyStamp: 'test-stamp',
}));

test('browse and reload pages, change size and preserve the return page with the live API', async ({ page, request }) => {
  const response = await request.get('/api/vehicles?pageNumber=2&pageSize=10');
  expect(response.status()).toBe(200);
  const data = await response.json();
  expect(data.count).toBe(data.items.length);
  expect(data.totalPages).toBeGreaterThan(1);
  expect(Object.keys(data).sort()).toEqual(['count', 'items', 'pageNumber', 'totalPages']);
  const first25 = await (await request.get('/api/vehicles?pageNumber=1&pageSize=25')).json();
  const second25 = await (await request.get('/api/vehicles?pageNumber=2&pageSize=25')).json();
  const first50 = await (await request.get('/api/vehicles?pageNumber=1&pageSize=50')).json();
  await page.goto('/vehicles?pageNumber=2&pageSize=10');
  await expect(page.getByText(`Showing 11–${10 + data.count}`, { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByText(`Page 2 of ${data.totalPages}`, { exact: true })).toBeVisible();
  await page.getByRole('link', { name: /View \/ edit/ }).first().click();
  await expect(page.getByLabel('Model', { exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Back to vehicles' })).toHaveAttribute('href', '/vehicles?pageNumber=2&pageSize=10');
  await page.getByRole('link', { name: 'Cancel', exact: true }).click();
  await expect(page).toHaveURL(/\/vehicles\?pageNumber=2&pageSize=10$/);
  await page.getByLabel('Rows per page').selectOption('25');
  await expect(page).toHaveURL(/\/vehicles\?pageNumber=1&pageSize=25$/);
  await expect(page.getByRole('button', { name: 'Previous', exact: true })).toBeDisabled();
  await expect(page.getByText(`Showing 1–${first25.count}`, { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Next', exact: true }).click();
  await expect(page.getByText(`Showing 26–${25 + second25.count}`, { exact: true })).toBeVisible();
  await page.getByLabel('Rows per page').selectOption('50');
  await expect(page).toHaveURL(/\/vehicles\?pageNumber=1&pageSize=50$/);
  await expect(page.getByText(`Page 1 of ${first50.totalPages}`, { exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: /View \/ edit/ })).toHaveCount(first50.count);
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByLabel('Rows per page')).toBeVisible();
  await page.screenshot({ path: 'test-results/pagination-mobile.png', fullPage: true });
});

test('deleting the only row on the last page refreshes caches and returns to the previous page', async ({ page }) => {
  let active = [...records];
  await page.route(/\/api\/vehicles(?:\/|\?|$)/, async route => {
    const request = route.request();
    const url = new URL(request.url());
    if (request.method() === 'DELETE') {
      active = active.filter(item => !url.pathname.endsWith(item.registrationNumber));
      await route.fulfill({ status: 204 });
    } else if (url.pathname === '/api/vehicles') {
      const pageNumber = Number(url.searchParams.get('pageNumber') ?? 1);
      const pageSize = Number(url.searchParams.get('pageSize') ?? 10);
      const items = active.slice((pageNumber - 1) * pageSize, pageNumber * pageSize);
      await route.fulfill({ json: { items, pageNumber, totalPages: Math.ceil(active.length / pageSize), count: items.length } });
    } else {
      await route.fulfill({ json: active.find(item => url.pathname.endsWith(item.registrationNumber)) });
    }
  });
  await page.goto('/vehicles?pageNumber=2&pageSize=10');
  await page.getByRole('link', { name: 'View / edit PAGE011', exact: true }).click();
  await page.getByRole('button', { name: 'Delete vehicle', exact: true }).click();
  await page.getByRole('button', { name: 'Confirm delete', exact: true }).click();
  await expect(page).toHaveURL(/\/vehicles\?pageNumber=1&pageSize=10$/);
  await expect(page.getByText('Showing 1–10', { exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: 'View / edit PAGE011', exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Next', exact: true })).toBeDisabled();
});

test('a stale detail link shows a friendly message and returns to a refreshed list', async ({ page }) => {
  let missing = false;
  await page.route(/\/api\/vehicles(?:\/|\?|$)/, async route => {
    const url = new URL(route.request().url());
    if (url.pathname === '/api/vehicles') {
      await route.fulfill({ json: { items: missing ? [] : records.slice(0, 1), pageNumber: 1, totalPages: missing ? 0 : 1, count: missing ? 0 : 1 } });
    } else {
      missing = true;
      await route.fulfill({ status: 404, json: { title: 'Vehicle was not found.' } });
    }
  });
  await page.goto('/vehicles');
  await page.getByRole('link', { name: 'View / edit PAGE001', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'This vehicle is no longer available' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Save changes' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Delete vehicle', exact: true })).toHaveCount(0);
  await page.getByRole('link', { name: 'Back to vehicles' }).click();
  await expect(page.getByText('Your fleet starts here')).toBeVisible();
  await expect(page.getByText('Showing 0–0', { exact: true })).toBeVisible();
});
