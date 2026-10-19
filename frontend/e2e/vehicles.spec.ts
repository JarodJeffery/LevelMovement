import { test, expect } from '@playwright/test';
import type { APIRequestContext } from '@playwright/test';

async function findVehiclePage(request: APIRequestContext, registration: string) {
  for (let pageNumber = 1; ; pageNumber++) {
    const page = await (await request.get(`/api/vehicles?pageNumber=${pageNumber}&pageSize=10`)).json();
    const vehicle = page.items.find((item: { registrationNumber: string }) => item.registrationNumber === registration);
    if (vehicle) return { vehicle, pageNumber };
    if (pageNumber >= page.totalPages) throw new Error(`Vehicle ${registration} was missing from all active pages.`);
  }
}
test('register, edit, reject stale stamps, delete, and reject booking of the deleted vehicle', async ({ page, request }) => {
  const registration = `E2E-${Date.now().toString(36).toUpperCase()}`;
  await page.goto('/vehicles/new');
  await page.getByLabel('Registration number', { exact: true }).fill(registration);
  await page.getByLabel('Make', { exact: true }).fill('Toyota');
  await page.getByLabel('Model', { exact: true }).fill('Corolla');
  await page.getByLabel('Year', { exact: true }).fill('2024');
  await page.getByLabel('Daily rate (R)', { exact: true }).fill('10.125');
  await page.getByRole('button', { name: 'Register vehicle', exact: true }).click();
  await expect(page.getByRole('heading', { name: registration, exact: true })).toBeVisible();
  await expect(page.getByLabel('Daily rate (R)', { exact: true })).toHaveValue('10.12');
  const details = await (await request.get(`/api/vehicles/${registration}`)).json();
  const duplicate = await request.post('/api/vehicles', { data: { registrationNumber: registration, make: 'Toyota', model: 'Corolla', year: 2024, dailyRate: 10 } });
  expect(duplicate.status()).toBe(409);
  expect(await duplicate.json()).not.toHaveProperty("traceId");
  const { vehicle: listed, pageNumber } = await findVehiclePage(request, registration);
  expect(listed).not.toHaveProperty('id');
  expect(listed).not.toHaveProperty('concurrencyStamp');
  await page.getByLabel('Model', { exact: true }).fill('Yaris');
  await page.getByRole('button', { name: 'Save changes', exact: true }).click();
  await expect(page.getByRole('status')).toContainText(`Updated vehicle ${registration}`);
  await expect(page.getByRole('button', { name: 'Save changes', exact: true })).toBeEnabled();
  const updated = await (await request.get(`/api/vehicles/${registration}`)).json();
  expect(updated.createdDate).toBe(details.createdDate);
  expect(updated.concurrencyStamp).not.toBe(details.concurrencyStamp);
  const stale = await request.delete(`/api/vehicles/${registration}`, { data: { concurrencyStamp: details.concurrencyStamp } });
  expect(stale.status()).toBe(409);
  expect(await stale.json()).not.toHaveProperty("traceId");
  await page.getByRole('link', { name: 'Back to vehicles' }).click();
  await page.goto('/vehicles?pageNumber=' + pageNumber + '&pageSize=10');
  await page.getByLabel('Registration to check').fill(registration);
  await page.getByRole('button', { name: 'Check eligibility' }).click();
  await expect(page.getByRole('status')).toContainText('eligible for booking');
  await page.getByRole('link', { name: `View / edit ${registration}` }).click();
  await page.getByRole('button', { name: 'Delete vehicle', exact: true }).click();
  await page.getByRole('button', { name: 'Confirm delete', exact: true }).click();
  await expect(page.getByRole('status').filter({ hasText: `Deleted vehicle ${registration}` })).toBeVisible();
  await expect(page.getByRole('link', { name: `View / edit ${registration}` })).toHaveCount(0);
  await page.getByLabel('Registration to check').fill(registration);
  await page.getByRole('button', { name: 'Check eligibility' }).click();
  await expect(page.getByRole('alert')).toContainText('Vehicle has been deleted.');
  await expect(page.getByText('Trace ID:', { exact: false })).toHaveCount(0);
  expect((await request.get(`/api/vehicles/${registration}`)).status()).toBe(404);
  const duplicateDeleted = await request.post('/api/vehicles', { data: { registrationNumber: registration, make: 'Toyota', model: 'Corolla', year: 2024, dailyRate: 10 } });
  expect(duplicateDeleted.status()).toBe(409);
  await page.screenshot({ path: 'test-results/vehicles-workspace.png', fullPage: true });
});

test('preserve edits on a real concurrent update and explicitly reload before deleting', async ({ page, request }) => {
  const registration = `RACE-${Date.now().toString(36).toUpperCase()}`;
  const inserted = await request.post('/api/vehicles', { data: { registrationNumber: registration, make: 'Ford', model: 'Fiesta', year: 2024, dailyRate: 100 } });
  expect(inserted.status()).toBe(201);
  const created = await inserted.json();
  await page.goto(`/vehicles/${registration}`);
  await page.getByLabel('Model', { exact: true }).fill('Local unsaved edit');
  const concurrent = await request.post('/api/vehicles', { data: { registrationNumber: registration, make: 'Ford', model: 'Server edit', year: 2024, dailyRate: 100, concurrencyStamp: created.concurrencyStamp } });
  expect(concurrent.status()).toBe(200);
  await page.getByRole('button', { name: 'Save changes', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Vehicle has changed.');
  await expect(page.getByLabel('Model', { exact: true })).toHaveValue('Local unsaved edit');
  await page.getByRole('button', { name: 'Reload latest details', exact: true }).click();
  await expect(page.getByLabel('Model', { exact: true })).toHaveValue('Server edit');
  await page.getByRole('button', { name: 'Delete vehicle', exact: true }).click();
  await page.getByRole('button', { name: 'Confirm delete', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Vehicles', exact: true })).toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: 'test-results/vehicles-mobile.png', fullPage: true });
});

test('invalid fields are highlighted and diagnostics are not displayed', async ({ page }) => {
  await page.goto('/vehicles/new');
  await page.getByLabel('Daily rate (R)', { exact: true }).fill('-10');
  await page.getByRole('button', { name: 'Register vehicle', exact: true }).click();
  const rate = page.getByLabel('Daily rate (R)', { exact: true });
  await expect(rate).toHaveAttribute('aria-invalid', 'true');
  await expect(rate).toHaveCSS('border-top-color', 'rgb(181, 60, 48)');
  await expect(page.getByText('Daily rate must be at least R0.01.')).toBeVisible();
  await expect(page.getByText('Trace ID:', { exact: false })).toHaveCount(0);
  await page.screenshot({ path: 'test-results/validation-fields.png', fullPage: true });
});
