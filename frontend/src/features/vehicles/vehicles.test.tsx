import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { VehicleListPage } from './views/VehicleListPage';
import { VehicleEditPage } from './views/VehicleEditPage';
import { vehiclesApi } from './api/vehiclesApi';
import type { VehicleDetailsDto } from './dtos/vehicleDtos';
const vehicle: VehicleDetailsDto = { registrationNumber: 'CA 123-456', make: 'Toyota', model: 'Corolla', year: 2024, dailyRate: 450, createdDate: '2026-10-10T12:00:00Z', concurrencyStamp: 'first-stamp' };
function mount(path: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  render(<QueryClientProvider client={client}><MemoryRouter initialEntries={[path]}><Routes><Route path="/vehicles" element={<VehicleListPage />} /><Route path="/vehicles/new" element={<VehicleEditPage create />} /><Route path="/vehicles/:registration" element={<VehicleEditPage />} /></Routes></MemoryRouter></QueryClientProvider>);
  return client;
}
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
afterEach(() => vi.unstubAllGlobals());
describe('Vehicle workspace', () => {
  it('shows an empty fleet and validates creation fields before requesting a save', async () => {
    const fetch = vi.fn().mockResolvedValue(json([]));
    vi.stubGlobal('fetch', fetch);
    mount('/vehicles');
    expect(await screen.findByText('Your fleet starts here')).toBeInTheDocument();
    const user = userEvent.setup();
    await user.click(screen.getByRole('link', { name: 'Register vehicle' }));
    await user.click(screen.getByRole('button', { name: 'Register vehicle' }));
    expect(await screen.findByText('Enter the registration number.')).toBeInTheDocument();
    expect(fetch).toHaveBeenCalledTimes(1);
  });
  it('creates normalized details without a stamp and navigates to the edit page', async () => {
    const fetch = vi.fn(async (url: string, options?: RequestInit) => options?.method === 'POST'
      ? json({ ...vehicle, message: 'Registered vehicle CA 123-456, make Toyota, model Corolla.' }, 201)
      : json(url.includes('/vehicles/') ? vehicle : [vehicle]));
    vi.stubGlobal('fetch', fetch);
    mount('/vehicles/new');
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Registration number'), ' ca 123-456 ');
    await user.type(screen.getByLabelText('Make'), 'Toyota');
    await user.type(screen.getByLabelText('Model'), 'Corolla');
    await user.click(screen.getByRole('button', { name: 'Register vehicle' }));
    expect(await screen.findByRole('heading', { name: 'CA 123-456' })).toBeInTheDocument();
    const call = fetch.mock.calls.find(([, options]) => options?.method === 'POST')!;
    const body = JSON.parse(call[1]!.body as string);
    expect(body.registrationNumber).toBe('CA 123-456');
    expect(body).not.toHaveProperty('concurrencyStamp');
    expect(body).not.toHaveProperty('id');
  });
  it('allows correcting a duplicate registration without offering detail reload', async () => {
    const fetch = vi.fn().mockResolvedValue(json({ title: 'Registration number already exists.', detail: 'Use a different registration number.', traceId: 'duplicate-trace' }, 409));
    vi.stubGlobal('fetch', fetch);
    mount('/vehicles/new');
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Registration number'), 'CA 123-456');
    await user.type(screen.getByLabelText('Make'), 'Toyota');
    await user.type(screen.getByLabelText('Model'), 'Corolla');
    await user.click(screen.getByRole('button', { name: 'Register vehicle' }));
    await screen.findByText('Trace ID: duplicate-trace');
    expect(screen.queryByRole('button', { name: 'Reload latest details' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Register vehicle' })).toBeEnabled();
    await user.clear(screen.getByLabelText('Registration number'));
    await user.type(screen.getByLabelText('Registration number'), 'NEW-REG');
    await user.click(screen.getByRole('button', { name: 'Register vehicle' }));
    await waitFor(() => expect(fetch).toHaveBeenCalledTimes(2));
  });
  it('uses the detail stamp and keeps edits until explicit reload on conflict', async () => {
    const fetch = vi.fn(async (_url: string, options?: RequestInit) => options?.method === 'POST'
      ? json({ title: 'Vehicle has changed.', detail: 'Reload the vehicle.', traceId: 'trace-conflict' }, 409)
      : json(vehicle));
    vi.stubGlobal('fetch', fetch);
    mount('/vehicles/CA%20123-456');
    const user = userEvent.setup();
    const model = await screen.findByLabelText('Model');
    expect(model).toHaveValue('Corolla');
    expect(screen.getByLabelText('Registration number')).toHaveAttribute('readonly');
    await user.clear(model);
    await user.type(model, 'My unsaved edit');
    await user.click(screen.getByRole('button', { name: 'Save changes' }));
    expect(await screen.findByText('Trace ID: trace-conflict')).toBeInTheDocument();
    expect(model).toHaveValue('My unsaved edit');
    const call = fetch.mock.calls.find(([, options]) => options?.method === 'POST')!;
    expect(JSON.parse(call[1]!.body as string).concurrencyStamp).toBe('first-stamp');
    await user.click(screen.getByRole('button', { name: 'Reload latest details' }));
    await waitFor(() => expect(model).toHaveValue('Corolla'));
  });
  it('refreshes the saved stamp and uses it for a confirmed deletion', async () => {
    let saved = false;
    const fetch = vi.fn(async (url: string, options?: RequestInit) => {
      if (options?.method === 'POST') { saved = true; return json({ ...vehicle, concurrencyStamp: 'second-stamp', message: 'Updated vehicle.' }); }
      if (options?.method === 'DELETE') return new Response(null, { status: 204 });
      return json(url.includes('/vehicles/') ? { ...vehicle, concurrencyStamp: saved ? 'second-stamp' : 'first-stamp' } : []);
    });
    vi.stubGlobal('fetch', fetch);
    mount('/vehicles/CA%20123-456');
    const user = userEvent.setup();
    await screen.findByLabelText('Model');
    await user.click(screen.getByRole('button', { name: 'Save changes' }));
    await screen.findByText('Updated vehicle.');
    await waitFor(() => expect(screen.getByRole('button', { name: 'Save changes' })).toBeEnabled());
    await user.click(screen.getByRole('button', { name: 'Delete vehicle' }));
    expect(fetch.mock.calls.some(([, options]) => options?.method === 'DELETE')).toBe(false);
    await user.click(screen.getByRole('button', { name: 'Confirm delete' }));
    expect(await screen.findByText(/Deleted vehicle CA 123-456/)).toBeInTheDocument();
    const call = fetch.mock.calls.find(([, options]) => options?.method === 'DELETE')!;
    expect(JSON.parse(call[1]!.body as string)).toEqual({ concurrencyStamp: 'second-stamp' });
  });
  it('keeps the form snapshot stamp when the detail cache refreshes in the background', async () => {
    let refreshed = false;
    const fetch = vi.fn(async (_url: string, options?: RequestInit) => options?.method === 'POST'
      ? json({ title: 'Vehicle has changed.', traceId: 'background-conflict' }, 409)
      : json({ ...vehicle, model: refreshed ? 'Server edit' : 'Corolla', concurrencyStamp: refreshed ? 'new-server-stamp' : 'first-stamp' }));
    vi.stubGlobal('fetch', fetch);
    const client = mount('/vehicles/CA%20123-456');
    const user = userEvent.setup();
    const model = await screen.findByLabelText('Model');
    await user.clear(model);
    await user.type(model, 'Unsaved local edit');
    refreshed = true;
    await client.invalidateQueries({ queryKey: ['vehicle', 'CA 123-456'] });
    expect(model).toHaveValue('Unsaved local edit');
    await user.click(screen.getByRole('button', { name: 'Save changes' }));
    await screen.findByText('Trace ID: background-conflict');
    const call = fetch.mock.calls.find(([, options]) => options?.method === 'POST')!;
    expect(JSON.parse(call[1]!.body as string).concurrencyStamp).toBe('first-stamp');
  });
  it('shows booking rejection with a trace ID', async () => {
    const fetch = vi.fn(async (_url: string, options?: RequestInit) => options?.method === 'POST'
      ? json({ title: 'Vehicle cannot be booked.', detail: 'Deleted vehicles cannot be booked.', traceId: 'trace-booking' }, 409)
      : json([]));
    vi.stubGlobal('fetch', fetch);
    mount('/vehicles');
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Registration to check'), 'CA 123-456');
    await user.click(screen.getByRole('button', { name: 'Check eligibility' }));
    expect(await screen.findByText('Deleted vehicles cannot be booked.')).toBeInTheDocument();
    expect(screen.getByText('Trace ID: trace-booking')).toBeInTheDocument();
  });
  it('returns a useful API error for failed connections', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('fetch failed')));
    await expect(vehiclesApi.list()).rejects.toThrow('Cannot reach the API.');
  });
});
