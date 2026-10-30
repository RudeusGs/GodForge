import { defineConfig, devices } from '@playwright/test';
export default defineConfig({
  testDir: './tests', testMatch: '**/*.live.spec.ts', fullyParallel: false, workers: 1,
  forbidOnly: !!process.env.CI,
  retries: 0, timeout: 90_000, reporter: 'list',
  use: { baseURL: 'http://127.0.0.1:25173', contextOptions: { reducedMotion: 'reduce' }, trace: 'off', screenshot: 'only-on-failure' },
  webServer: {
    command: 'npm --prefix ../GodForge-FE run dev -- --host 127.0.0.1 --port 25173 --strictPort',
    url: 'http://127.0.0.1:25173', reuseExistingServer: false,
    env: { VITE_API_BASE_URL: 'http://127.0.0.1:25072/api/v1' },
  },
  projects: [{ name: 'chromium-live', use: { ...devices['Desktop Chrome'] } }],
});
