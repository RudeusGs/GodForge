import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError, type InternalAxiosRequestConfig, type AxiosResponse } from 'axios';
import { authApi } from '../api/auth/auth.api';
import { clearAccessToken } from '../api/auth/authSession';
import { getBrowserDeviceLabel, useAuthStore } from './auth.store';
import { authRefreshCoordinator } from '../api/auth/authRefreshCoordinator';

vi.mock('../api/auth/auth.api', () => ({
    authApi: {
        login: vi.fn(),
        logout: vi.fn(),
        register: vi.fn(),
        forgotPassword: vi.fn(),
    },
}));

vi.mock('../api/auth/authRefreshCoordinator', async (importOriginal) => {
    const actual = await importOriginal<typeof import('../api/auth/authRefreshCoordinator')>();
    return {
        ...actual,
        authRefreshCoordinator: {
            ...actual.authRefreshCoordinator,
            refreshAccessToken: vi.fn(),
            publishCleared: vi.fn(),
            publishAuthenticated: vi.fn(),
            subscribe: vi.fn(),
        },
    };
});

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
        expect(localStorage.getItem('access_token')).toBeNull();
        expect(localStorage.getItem('auth_user')).toBeNull();
        expect(sessionStorage.length).toBe(0);
    });

    it('restores the in-memory session through the HttpOnly refresh cookie', async () => {
        const store = useAuthStore();
        vi.mocked(authRefreshCoordinator.refreshAccessToken).mockResolvedValueOnce(authResponse.data);

        await store.initialize();

        expect(store.initialized).toBe(true);
        expect(store.isAuthenticated).toBe(true);
        expect(store.accessToken).toBe('mock-token');
        expect(authRefreshCoordinator.refreshAccessToken).toHaveBeenCalledTimes(1);
    });

    it('definitively unauthenticates when refresh returns 401', async () => {
        const store = useAuthStore();
        const error = new AxiosError('unauthorized', 'ERR_BAD_REQUEST', {} as InternalAxiosRequestConfig, undefined, { status: 401 } as AxiosResponse);
        vi.mocked(authRefreshCoordinator.refreshAccessToken).mockRejectedValueOnce(error);

        await store.initialize();

        expect(store.initialized).toBe(true);
        expect(store.isAuthenticated).toBe(false);
        expect(store.accessToken).toBeNull();
        expect(authRefreshCoordinator.publishCleared).toHaveBeenCalledWith('session-expired');
    });

    it('enters a recoverable session-check state on transient network error, not falsely marked expired', async () => {
        const store = useAuthStore();
        vi.mocked(authRefreshCoordinator.refreshAccessToken).mockRejectedValueOnce(new Error('network offline'));

        await store.initialize();

        expect(store.initialized).toBe(false); // not set to true on transient
        expect(store.isAuthenticated).toBe(false);
        expect(store.initializationError).toBeInstanceOf(Error);
        expect(store.initializationError?.message).toBe('network offline');
        expect(authRefreshCoordinator.publishCleared).not.toHaveBeenCalled();
    });

    it('can restore auth by retrying after a transient failure', async () => {
        const store = useAuthStore();
        vi.mocked(authRefreshCoordinator.refreshAccessToken)
            .mockRejectedValueOnce(new Error('503 Service Unavailable'))
            .mockResolvedValueOnce(authResponse.data);

        await store.initialize();
        expect(store.initialized).toBe(false);

        await store.initialize();
        expect(store.initialized).toBe(true);
        expect(store.isAuthenticated).toBe(true);
        expect(store.accessToken).toBe('mock-token');
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

    it('clears local state even when server-side logout fails', async () => {
        const store = useAuthStore();
        vi.mocked(authApi.login).mockResolvedValueOnce(authResponse);
        vi.mocked(authApi.logout).mockRejectedValueOnce(new Error('redis unavailable'));
        await store.login({ email: 'test@example.com', password: 'password123' });

        await expect(store.logout()).rejects.toThrow('redis unavailable');

        expect(store.isAuthenticated).toBe(false);
        expect(store.accessToken).toBeNull();
        expect(store.user).toBeNull();
    });

    it('distinguishes account creation from automatic login failure', async () => {
        const store = useAuthStore();
        vi.mocked(authApi.register).mockResolvedValueOnce({ data: authResponse.data.user });
        vi.mocked(authApi.login).mockRejectedValueOnce(new Error('network timeout'));

        await expect(store.register({ email: 'created@example.com', displayName: 'Created', password: 'Password123', otp: '123456' }))
            .rejects.toMatchObject({
                name: 'RegistrationCompletedLoginFailedError',
                email: 'created@example.com',
            });
        expect(authApi.register).toHaveBeenCalledTimes(1);
        expect(authApi.login).toHaveBeenCalledTimes(1);
    });

    it('derives a bounded display-only browser label', () => {
        expect(getBrowserDeviceLabel('Mozilla/5.0 (Windows NT 10.0) Chrome/140.0')).toBe('Chrome on Windows');
        expect(getBrowserDeviceLabel('Mozilla/5.0 (iPhone) Version/18.0 Mobile Safari/605.1')).toBe('Safari on iPhone');
    });
});
