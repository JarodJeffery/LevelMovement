import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';
import { loadEnv } from 'vite';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { Agent } from 'node:https';
export default defineConfig(({ command, mode }) => {
  const environment = loadEnv(mode, process.cwd(), '');
  const target = environment.API_TARGET ?? 'https://localhost:63887';
  if (new URL(target).protocol !== 'https:') throw new Error('API_TARGET must use HTTPS.');
  const needsHttps = command === 'serve' && !process.env.VITEST;
  const certificateFile = (name: string) => readFileSync(fileURLToPath(new URL(`../backend/certs/${name}`, import.meta.url)));
  let https;
  let agent;
  if (needsHttps) {
    try {
      https = { pfx: certificateFile('localhost.pfx'), passphrase: certificateFile('password.txt').toString().trim() };
      agent = new Agent({ ca: certificateFile('localhost.pem') });
    } catch {
      throw new Error('HTTPS certificate is missing. Run backend/scripts/Setup-LocalHttps.ps1 first.');
    }
  }
  const proxy = { '/api': { target, secure: true, agent } };
  return {
    plugins: [react()],
    server: { host: 'localhost', https, proxy },
    preview: { host: 'localhost', https, proxy },
    test: { environment: 'jsdom', setupFiles: './src/test/setup.ts', include: ['src/**/*.test.ts', 'src/**/*.test.tsx'] },
  };
});
