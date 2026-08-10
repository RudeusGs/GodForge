import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { authApi } from '../api/auth/auth.api';
import { clearAccessToken } from '../api/auth/authSession';
import { useAuthStore } from './auth.store';

vi.mock('../api/auth/auth.api', () => ({
    authApi: {
        login: vi.fn(),
        logout: vi.fn(),
        refresh: vi.fn(),
        register: vi.fn(),
        forgotPassword: vi.fn(),
    },
}));

const authResponse = {
    data: {
        accessToken: 'mock-token',
        user: {
            id: '1',
            email: 'test@example.com',
            displayName: 'Test User',
            status: 'active',
            emailVerifiedAt: '2026-08-05T00:00:00Z',
            createdAt: '2026-08-05T00:00:00Z',
            version: 1,
        },
        session: {
            id: 'session-1',
            deviceName: 'test',
            createdAt: '2026-08-05T00:00:00Z',
            lastSeenAt: '2026-08-05T00:00:00Z',
            expiresAt: '2026-09-04T00:00:00Z',
            current: true,
            revokedAt: null,
        },
        accessTokenExpiresAt: '2026-08-05T00:15:00Z',
        refreshTokenExpiresAt: '2026-09-04T00:00:00Z',
    },
    meta: { correlationId: '123' },
};

describe('Auth Store', () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        clearAccessToken();
        localStorage.clear();
        sessionStorage.clear();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it('initializes with no browser-readable credentials', () => {
        const store = useAuthStore();

        expect(store.isAuthenticated).toBe(false);
        expect(store.user).toBeNull();
        expect(store.accessToken).toBeNull();
        expect(localStorage.length).toBe(0);
        expect(sessionStorage.length).toBe(0);
    });

    it('keeps the access token in memory after login', async () => {
        const store = useAuthStore();
        vi.mocked(authApi.login).mockResolvedValueOnce(authResponse);

        await store.login({ email: 'test@example.com', password: 'password123' });

        expect(store.isAuthenticated).toBe(true);
        expect(store.accessToken).toBe('mock-token');
        expect(store.user?.email).toBe('test@example.com');
        expect(localStorage.length).toBe(0);
        expect(sessionStorage.length).toBe(0);
    });

    it('restores the in-memory session through the HttpOnly refresh cookie', async () => {
        const store = useAuthStore();
        vi.mocked(authApi.refresh).mockResolvedValueOnce(authResponse);

        await store.initialize();

        expect(store.initialized).toBe(true);
        expect(store.isAuthenticated).toBe(true);
        expect(store.accessToken).toBe('mock-token');
        expect(authApi.refresh).toHaveBeenCalledTimes(1);
    });

    it('remains unauthenticated when refresh fails', async () => {
        const store = useAuthStore();
        vi.mocked(authApi.refresh).mockRejectedValueOnce(new Error('unauthorized'));

        await store.initialize();

        expect(store.initialized).toBe(true);
        expect(store.isAuthenticated).toBe(false);
        expect(store.accessToken).toBeNull();
    });

    it('clears in-memory state on logout', async () => {
        const store = useAuthStore();
        vi.mocked(authApi.login).mockResolvedValueOnce(authResponse);
        vi.mocked(authApi.logout).mockResolvedValueOnce(undefined);
        await store.login({ email: 'test@example.com', password: 'password123' });

        await store.logout();

        expect(store.isAuthenticated).toBe(false);
        expect(store.accessToken).toBeNull();
        expect(store.user).toBeNull();
    });
});
