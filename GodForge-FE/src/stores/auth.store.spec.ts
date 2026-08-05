import { setActivePinia, createPinia } from 'pinia';
import { useAuthStore } from './auth.store';
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { authApi } from '../api/auth/auth.api';

vi.mock('../api/auth/auth.api', () => ({
  authApi: {
    login: vi.fn(),
    logout: vi.fn(),
  }
}));

describe('Auth Store', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    localStorage.clear();
    sessionStorage.clear();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('initializes with default values', () => {
    const store = useAuthStore();
    expect(store.isAuthenticated).toBe(false);
    expect(store.user).toBeNull();
    expect(store.accessToken).toBeNull();
  });

  it('updates state upon successful login with rememberMe=true', async () => {
    const store = useAuthStore();

    // Mock the API response
    const mockResponse = {
      data: {
        accessToken: 'mock-token',
        user: { id: '1', email: 'test@example.com', displayName: 'Test User', status: 'active', emailVerifiedAt: '2026-08-05T00:00:00Z', createdAt: '2026-08-05T00:00:00Z', version: 1 },
        session: { id: 'session-1', deviceName: 'test', createdAt: '2026-08-05T00:00:00Z', lastSeenAt: '2026-08-05T00:00:00Z', expiresAt: '2026-09-04T00:00:00Z', current: true, revokedAt: null },
        accessTokenExpiresAt: '2026-08-05T00:15:00Z',
        refreshTokenExpiresAt: '2026-09-04T00:00:00Z'
      },
      meta: { correlationId: '123' }
    };

    // @ts-expect-error - mocking axios response
    vi.mocked(authApi.login).mockResolvedValueOnce(mockResponse);

    await store.login({ email: 'test@example.com', password: 'password123' }, true);

    expect(store.isAuthenticated).toBe(true);
    expect(store.accessToken).toBe('mock-token');
    expect(store.user?.email).toBe('test@example.com');
    expect(localStorage.getItem('access_token')).toBe('mock-token');
    expect(sessionStorage.getItem('access_token')).toBeNull();
    expect(localStorage.getItem('refresh_token')).toBeNull();
    expect(sessionStorage.getItem('refresh_token')).toBeNull();
  });

  it('updates state upon successful login with rememberMe=false', async () => {
    const store = useAuthStore();

    // Mock the API response
    const mockResponse = {
      data: {
        accessToken: 'mock-token-session',
        user: { id: '1', email: 'session@example.com', displayName: 'Session User', status: 'active', emailVerifiedAt: '2026-08-05T00:00:00Z', createdAt: '2026-08-05T00:00:00Z', version: 1 },
        session: { id: 'session-2', deviceName: 'test', createdAt: '2026-08-05T00:00:00Z', lastSeenAt: '2026-08-05T00:00:00Z', expiresAt: '2026-09-04T00:00:00Z', current: true, revokedAt: null },
        accessTokenExpiresAt: '2026-08-05T00:15:00Z',
        refreshTokenExpiresAt: '2026-09-04T00:00:00Z'
      },
      meta: { correlationId: '123' }
    };

    // @ts-expect-error - mocking axios response
    vi.mocked(authApi.login).mockResolvedValueOnce(mockResponse);

    await store.login({ email: 'session@example.com', password: 'password123' }, false);

    expect(store.isAuthenticated).toBe(true);
    expect(store.accessToken).toBe('mock-token-session');
    expect(localStorage.getItem('access_token')).toBeNull();
    expect(sessionStorage.getItem('access_token')).toBe('mock-token-session');
    expect(localStorage.getItem('refresh_token')).toBeNull();
    expect(sessionStorage.getItem('refresh_token')).toBeNull();
  });

  it('clears state on logout', async () => {
    const store = useAuthStore();

    // Setup initial state
    localStorage.setItem('access_token', 'token');
    sessionStorage.setItem('access_token', 'session_token');
    await store.logout();

    expect(store.isAuthenticated).toBe(false);
    expect(store.accessToken).toBeNull();
    expect(store.user).toBeNull();
    expect(localStorage.getItem('access_token')).toBeNull();
    expect(sessionStorage.getItem('access_token')).toBeNull();
  });
});
