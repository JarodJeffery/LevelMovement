import { afterEach, describe, expect, it, vi } from 'vitest';
import { request } from './client';
import { vehiclesApi } from '../../features/vehicles/api/vehiclesApi';
afterEach(() => vi.unstubAllGlobals());
describe('defensive API handling', () => {
  it('rejects malformed successful JSON', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('<html>wrong server</html>', { status: 200 })));
    await expect(request('/vehicles')).rejects.toThrow('The API returned an invalid response.');
  });
  it('rejects missing detail stamps instead of enabling unstamped updates', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ registrationNumber: 'TEST', make: 'Toyota', model: 'Corolla', year: 2024, dailyRate: 100, createdDate: '2026-10-10T12:00:00Z' }))));
    await expect(vehiclesApi.details('TEST')).rejects.toThrow('The API returned incomplete vehicle details.');
  });
  it('handles non-JSON error responses safely', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('bad gateway', { status: 502 })));
    await expect(request('/vehicles')).rejects.toThrow('The request failed.');
  });
  it('filters malformed validation messages and diagnostic extensions', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: 'Invalid input', traceId: 'internal-trace', errors: { dailyRate: ['Enter a valid rate.', 123], model: 'invalid type' } }), { status: 400 })));
    await expect(request('/vehicles')).rejects.toMatchObject({ problem: { title: 'Invalid input', errors: { dailyRate: ['Enter a valid rate.'] } } });
  });
  it('uses a timeout and refuses redirects', async () => {
    const fetch = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetch);
    await request('/vehicles/TEST', { method: 'DELETE' });
    expect(fetch.mock.calls[0][1]).toMatchObject({ redirect: 'error', signal: expect.any(AbortSignal) });
  });
});
