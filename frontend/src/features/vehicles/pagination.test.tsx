import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { VehicleListPage } from './views/VehicleListPage';
import { VehicleEditPage } from './views/VehicleEditPage';
import { vehicleKeys } from './models/vehicle';
import type { VehicleDetailsDto } from './dtos/vehicleDtos';

const fleet: VehicleDetailsDto[] = Array.from({ length: 51 }, (_, i) => ({
  registrationNumber: `TEST${String(i + 1).padStart(3, '0')}`, make: 'Toyota', model: 'Corolla',
  year: 2024, dailyRate: 450, createdDate: '2026-10-10T12:00:00Z', concurrencyStamp: `stamp-${i}`,
}));
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
function envelope(items = fleet, number = 1, size = 10) {
  const pageItems = items.slice((number - 1) * size, number * size);
  return { items: pageItems, pageNumber: number, totalPages: Math.ceil(items.length / size), count: pageItems.length };
}
function api(initial = fleet) {
  let records = [...initial];
  const fetch = vi.fn(async (url: string, options?: RequestInit) => {
    const address = new URL(url, 'https://localhost');
    const registration = decodeURIComponent(address.pathname.split('/').pop()!);
    if (options?.method === 'DELETE') { records = records.filter(v => v.registrationNumber !== registration); return new Response(null, { status: 204 }); }
    if (options?.method === 'POST') return json({ ...fleet[0], message: 'Saved vehicle.', concurrencyStamp: 'saved-stamp' }, 201);
    if (address.pathname === '/api/vehicles') return json(envelope(records, Number(address.searchParams.get('pageNumber')), Number(address.searchParams.get('pageSize'))));
    const found = records.find(v => v.registrationNumber === registration);
    return found ? json(found) : json({ title: 'Vehicle was not found.' }, 404);
  });
  vi.stubGlobal('fetch', fetch);
  return fetch;
}
function Location() { const location = useLocation(); return <output data-testid="location">{location.pathname + location.search}</output>; }
function mount(path: string, client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })) {
  render(<QueryClientProvider client={client}><MemoryRouter initialEntries={[path]}><Location /><Routes>
    <Route path="/vehicles" element={<VehicleListPage />} />
    <Route path="/vehicles/new" element={<VehicleEditPage create />} />
    <Route path="/vehicles/:registration" element={<VehicleEditPage />} />
  </Routes></MemoryRouter></QueryClientProvider>);
  return client;
}
afterEach(() => vi.unstubAllGlobals());

describe('Vehicle pagination', () => {
  it('reads a reloaded URL, changes pages and resets the page when size changes', async () => {
    const fetch = api();
    mount('/vehicles?pageNumber=2&pageSize=10');
    const user = userEvent.setup();
    await screen.findByText('Showing 11–20');
    expect(screen.getByText('Page 2 of 6')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'View / edit TEST001' })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Next' }));
    await screen.findByText('Showing 21–30');
    expect(screen.getByTestId('location')).toHaveTextContent('/vehicles?pageNumber=3&pageSize=10');
    await user.click(screen.getByRole('button', { name: 'Previous' }));
    await screen.findByText('Showing 11–20');
    await user.selectOptions(screen.getByLabelText('Rows per page'), '25');
    await screen.findByText('Showing 1–25');
    expect(screen.getByTestId('location')).toHaveTextContent('/vehicles?pageNumber=1&pageSize=25');
    expect(fetch.mock.calls.some(([url]) => url.endsWith('pageNumber=1&pageSize=25'))).toBe(true);
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
    await user.selectOptions(screen.getByLabelText('Rows per page'), '50');
    await screen.findByText('Showing 1–50');
    expect(screen.getByText('Page 1 of 2')).toBeInTheDocument();
    expect(screen.getByText('50 vehicles on this page')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Next' }));
    await screen.findByText('Showing 51–51');
    expect(screen.getByText('1 vehicle on this page')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
  });

  it.each(['', '?pageNumber=-1&pageSize=101', '?pageNumber=foo&pageSize=0', '?pageNumber=2147483648&pageSize=NaN', '?pageNumber=1.5&pageSize=1.5'])(
    'defaults missing or invalid paging in %s', async query => {
      const fetch = api();
      mount('/vehicles' + query);
      await screen.findByText('Showing 1–10');
      expect(fetch.mock.calls[0][0]).toBe('/api/vehicles?pageNumber=1&pageSize=10');
    });

  it('supports valid custom sizes from the URL', async () => {
    api(); mount('/vehicles?pageNumber=2&pageSize=3');
    await screen.findByText('Showing 4–6');
    expect(screen.getByLabelText('Rows per page')).toHaveValue('3');
  });

  it('preserves the full list URL for Back, Cancel and creation', async () => {
    api(); mount('/vehicles?pageNumber=2&pageSize=25');
    const user = userEvent.setup();
    await user.click(await screen.findByRole('link', { name: 'View / edit TEST026' }));
    await screen.findByLabelText('Model');
    expect(screen.getByRole('link', { name: /Back to vehicles/ })).toHaveAttribute('href', '/vehicles?pageNumber=2&pageSize=25');
    await user.click(screen.getByRole('link', { name: 'Cancel' }));
    await screen.findByText('Showing 26–50');
    await user.click(screen.getByRole('link', { name: 'Register vehicle' }));
    expect(screen.getByRole('link', { name: 'Cancel' })).toHaveAttribute('href', '/vehicles?pageNumber=2&pageSize=25');
    await user.type(screen.getByLabelText('Registration number'), 'NEW001');
    await user.type(screen.getByLabelText('Make'), 'Toyota');
    await user.type(screen.getByLabelText('Model'), 'Corolla');
    await user.clear(screen.getByLabelText('Daily rate (R)'));
    await user.type(screen.getByLabelText('Daily rate (R)'), '450');
    await user.click(screen.getByRole('button', { name: 'Register vehicle' }));
    await screen.findByLabelText('Model');
    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('/vehicles/TEST001'));
    expect(screen.getByRole('link', { name: /Back to vehicles/ })).toHaveAttribute('href', '/vehicles?pageNumber=2&pageSize=25');
  });

  it('clears every cached list and detail after deletion, then falls back from the last page', async () => {
    api(fleet.slice(0, 11));
    const client = mount('/vehicles?pageNumber=2&pageSize=10');
    const user = userEvent.setup();
    await user.click(await screen.findByRole('link', { name: 'View / edit TEST011' }));
    await screen.findByLabelText('Model');
    client.setQueryData(vehicleKeys.list(1, 25), envelope(fleet.slice(0, 11), 1, 25));
    await user.click(screen.getByRole('button', { name: 'Delete vehicle' }));
    await user.click(screen.getByRole('button', { name: 'Confirm delete' }));
    await screen.findByText('Showing 1–10');
    expect(screen.getByTestId('location')).toHaveTextContent('/vehicles?pageNumber=1&pageSize=10');
    expect(client.getQueryData(vehicleKeys.list(1, 25))).toBeUndefined();
    expect(client.getQueryData(vehicleKeys.details('TEST011'))).toBeUndefined();
    expect(screen.queryByRole('link', { name: 'View / edit TEST011' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
  });

  it('returns an empty fleet to page 1 and disables paging', async () => {
    api([]); mount('/vehicles?pageNumber=6&pageSize=10');
    await screen.findByText('Your fleet starts here');
    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('/vehicles?pageNumber=1&pageSize=10'));
    expect(await screen.findByText('Showing 0–0')).toBeInTheDocument();
    expect(screen.getByText('Page 0 of 0')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
  });

  it('shows a friendly detail 404, clears stale pages and returns to a refreshed list', async () => {
    let missing = false;
    const fetch = vi.fn(async (url: string) => url.includes('/vehicles/')
      ? json({ title: 'Vehicle was not found.' }, 404)
      : json(envelope(missing ? [] : fleet.slice(0, 1))));
    vi.stubGlobal('fetch', fetch);
    const client = mount('/vehicles');
    const user = userEvent.setup();
    const link = await screen.findByRole('link', { name: 'View / edit TEST001' });
    client.setQueryData(vehicleKeys.list(2, 25), envelope(fleet, 2, 25));
    missing = true;
    await user.click(link);
    await screen.findByRole('heading', { name: 'This vehicle is no longer available' });
    await waitFor(() => expect(client.getQueriesData({ queryKey: vehicleKeys.lists })).toEqual([]));
    expect(screen.queryByRole('button', { name: 'Delete vehicle' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save changes' })).not.toBeInTheDocument();
    await user.click(screen.getByRole('link', { name: /Back to vehicles/ }));
    await screen.findByText('Your fleet starts here');
    expect(screen.queryByRole('link', { name: 'View / edit TEST001' })).not.toBeInTheDocument();
  });

  it('invalidates every list page after an edit without discarding the form stamp', async () => {
    const fetch = api(); const client = mount('/vehicles/TEST001');
    await screen.findByLabelText('Model');
    client.setQueryData(vehicleKeys.list(1, 10), envelope());
    client.setQueryData(vehicleKeys.list(2, 25), envelope(fleet, 2, 25));
    await userEvent.setup().click(screen.getByRole('button', { name: 'Save changes' }));
    await screen.findByText('Saved vehicle.');
    await waitFor(() => expect(client.getQueryState(vehicleKeys.list(2, 25))?.isInvalidated).toBe(true));
    expect(client.getQueryState(vehicleKeys.list(1, 10))?.isInvalidated).toBe(true);
    expect(JSON.parse(fetch.mock.calls.find(([, options]) => options?.method === 'POST')![1]!.body as string).concurrencyStamp).toBe('stamp-0');
    await act(async () => {});
  });
});
