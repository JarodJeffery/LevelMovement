import { defineConfig } from '@playwright/test';
const port = process.env.E2E_PORT ?? '5173';
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  use: { baseURL: `https://localhost:${port}`, browserName: 'chromium', trace: 'retain-on-failure' },
  webServer: { command: `npm run dev -- --host localhost --port ${port} --strictPort`, url: `https://localhost:${port}`, reuseExistingServer: !process.env.CI },
});
