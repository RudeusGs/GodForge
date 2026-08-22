import 'fake-indexeddb/auto';
import axios, { AxiosError, type AxiosRequestConfig, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { clearAccessToken, getAccessToken, setAccessToken } from './auth/authSession';
import axiosClient from './axiosClient';
import { authRefreshCoordinator, AuthRefreshPeerFailedError, AuthRefreshCoordinationUnavailableError } from './auth/authRefreshCoordinator';

const refreshedAuth = {
    accessToken: 'new-access-token',
    accessTokenExpiresAt: '2026-08-22T01:15:00Z',
    refreshTokenExpiresAt: '2026-09-22T01:00:00Z',
    user: { id: 'u1', email: 'a@example.com', displayName: 'A', status: 'active', emailVerifiedAt: null, createdAt: '2026-08-22T01:00:00Z', version: 1 },
    session: { id: 's1', deviceName: 'Chrome', createdAt: '2026-08-22T01:00:00Z', lastSeenAt: '2026-08-22T01:00:00Z', expiresAt: '2026-09-22T01:00:00Z', current: true, revokedAt: null },
};

describe('axios authentication interceptor', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        clearAccessToken();
    });

    it('turns ten simultaneous 401 responses into one refresh and retries each request once', async () => {
        setAccessToken('expired-access-token');
        const refreshTransport = vi.spyOn(axios, 'post').mockResolvedValue({ data: { data: refreshedAuth } });
        const adapter = vi.fn(async (config: InternalAxiosRequestConfig): Promise<AxiosResponse> => {
            if (!(config as InternalAxiosRequestConfig & { _retry?: boolean })._retry) {
                const response: AxiosResponse = { data: {}, status: 401, statusText: 'Unauthorized', headers: {}, config };
                throw new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, undefined, response);
            }
            return { data: { ok: true, url: config.url }, status: 200, statusText: 'OK', headers: {}, config };
        });
        const previousAdapter = axiosClient.defaults.adapter;
        axiosClient.defaults.adapter = adapter as AxiosRequestConfig['adapter'];

        try {
            const responses = await Promise.all(Array.from({ length: 10 }, (_, index) => axiosClient.get(`/resource/${index}`)));
            expect(responses).toHaveLength(10);
            expect(responses.every(response => (response as unknown as { ok: boolean }).ok)).toBe(true);
            expect(refreshTransport).toHaveBeenCalledTimes(1);
            expect(adapter).toHaveBeenCalledTimes(20);
        } finally {
            axiosClient.defaults.adapter = previousAdapter;
        }
    });

    describe('refresh failure classification', () => {
        let publishClearedSpy: ReturnType<typeof vi.spyOn>;
        let adapter: ReturnType<typeof vi.fn>;

        beforeEach(() => {
            setAccessToken('expired');
            publishClearedSpy = vi.spyOn(authRefreshCoordinator, 'publishCleared').mockImplementation(() => {});
            adapter = vi.fn(async (config: InternalAxiosRequestConfig): Promise<AxiosResponse> => {
                if (!(config as InternalAxiosRequestConfig & { _retry?: boolean })._retry) {
                    const response: AxiosResponse = { data: {}, status: 401, statusText: 'Unauthorized', headers: {}, config };
                    throw new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, undefined, response);
                }
                return { data: { ok: true }, status: 200, statusText: 'OK', headers: {}, config };
            });
            axiosClient.defaults.adapter = adapter as AxiosRequestConfig['adapter'];
            
            // Mock window.location.assign
            Object.defineProperty(window, 'location', {
                value: { pathname: '/', assign: vi.fn() },
                writable: true,
            });
        });

        it('HTTP 401: clears local auth, publishes session-expired, redirects to login', async () => {
            vi.spyOn(authRefreshCoordinator, 'refreshAccessToken').mockRejectedValue(
                new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', {} as InternalAxiosRequestConfig, undefined, { status: 401 } as AxiosResponse)
            );

            await expect(axiosClient.get('/resource')).rejects.toThrow('Unauthorized');
            expect(getAccessToken()).toBeNull();
            expect(publishClearedSpy).toHaveBeenCalledWith('session-expired');
            expect(window.location.assign).toHaveBeenCalledWith('/login');
        });

        it('network error/no response: NO auth clear, NO redirect, recoverable error', async () => {
            vi.spyOn(authRefreshCoordinator, 'refreshAccessToken').mockRejectedValue(
                new AxiosError('Network Error', 'ERR_NETWORK', {} as InternalAxiosRequestConfig)
            );

            await expect(axiosClient.get('/resource')).rejects.toThrow('Network Error');
            expect(getAccessToken()).toBe('expired');
            expect(publishClearedSpy).not.toHaveBeenCalled();
            expect(window.location.assign).not.toHaveBeenCalled();
        });

        it('timeout: NO auth clear, NO redirect, recoverable error', async () => {
            vi.spyOn(authRefreshCoordinator, 'refreshAccessToken').mockRejectedValue(
                new AxiosError('Timeout', 'ECONNABORTED', {} as InternalAxiosRequestConfig)
            );

            await expect(axiosClient.get('/resource')).rejects.toThrow('Timeout');
            expect(getAccessToken()).toBe('expired');
            expect(publishClearedSpy).not.toHaveBeenCalled();
            expect(window.location.assign).not.toHaveBeenCalled();
        });

        it('HTTP 503: NO auth clear, NO redirect, recoverable error', async () => {
            vi.spyOn(authRefreshCoordinator, 'refreshAccessToken').mockRejectedValue(
                new AxiosError('Service Unavailable', 'ERR_BAD_RESPONSE', {} as InternalAxiosRequestConfig, undefined, { status: 503 } as AxiosResponse)
            );

            await expect(axiosClient.get('/resource')).rejects.toThrow('Service Unavailable');
            expect(getAccessToken()).toBe('expired');
            expect(publishClearedSpy).not.toHaveBeenCalled();
            expect(window.location.assign).not.toHaveBeenCalled();
        });

        it('HTTP 429: NO auth clear, NO redirect, recoverable error', async () => {
            vi.spyOn(authRefreshCoordinator, 'refreshAccessToken').mockRejectedValue(
                new AxiosError('Too Many Requests', 'ERR_BAD_RESPONSE', {} as InternalAxiosRequestConfig, undefined, { status: 429 } as AxiosResponse)
            );

            await expect(axiosClient.get('/resource')).rejects.toThrow('Too Many Requests');
            expect(getAccessToken()).toBe('expired');
            expect(publishClearedSpy).not.toHaveBeenCalled();
            expect(window.location.assign).not.toHaveBeenCalled();
        });

        it('AuthRefreshPeerFailedError: NO auth clear, NO redirect', async () => {
            vi.spyOn(authRefreshCoordinator, 'refreshAccessToken').mockRejectedValue(new AuthRefreshPeerFailedError());

            await expect(axiosClient.get('/resource')).rejects.toThrow(AuthRefreshPeerFailedError);
            expect(getAccessToken()).toBe('expired');
            expect(publishClearedSpy).not.toHaveBeenCalled();
            expect(window.location.assign).not.toHaveBeenCalled();
        });

        it('AuthRefreshCoordinationUnavailableError: NO auth clear, NO redirect', async () => {
            vi.spyOn(authRefreshCoordinator, 'refreshAccessToken').mockRejectedValue(new AuthRefreshCoordinationUnavailableError());

            await expect(axiosClient.get('/resource')).rejects.toThrow(AuthRefreshCoordinationUnavailableError);
            expect(getAccessToken()).toBe('expired');
            expect(publishClearedSpy).not.toHaveBeenCalled();
            expect(window.location.assign).not.toHaveBeenCalled();
        });
    });
});
