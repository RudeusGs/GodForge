import { defineConfig, devices } from '@playwright/test';
export default defineConfig({
  testDir: './tests', testMatch: '**/*.m2.spec.ts', workers: 1, fullyParallel: false,
  forbidOnly: !!process.env.CI, retries: 0, timeout: 90_000, reporter: 'list',
  use: { baseURL: 'http://127.0.0.1:35173', trace: 'off', screenshot: 'only-on-failure', contextOptions: { reducedMotion: 'reduce' } },
  webServer: {
    command: 'npm --prefix ../GodForge-FE run dev -- --host 127.0.0.1 --port 35173 --strictPort',
    url: 'http://127.0.0.1:35173', reuseExistingServer: false,
    env: { VITE_API_BASE_URL: 'http://127.0.0.1:35072/api/v1' }
  },
  projects: [{ name: 'chromium-m2-provider', use: { ...devices['Desktop Chrome'] } }]
});
