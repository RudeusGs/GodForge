import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import axios from 'axios';
import { authApi } from '../api/auth/auth.api';
import {
    clearAccessToken,
    clearLegacyStoredAuth,
    inMemoryAccessToken,
    setAccessToken,
} from '../api/auth/authSession';
import type { LoginPayload, RegisterPayload, UserDto } from '../api/auth/auth.models';
import { authRefreshCoordinator, isDefinitiveAuthInvalidation } from '../api/auth/authRefreshCoordinator';

export class RegistrationCompletedLoginFailedError extends Error {
    public readonly email: string;

    constructor(email: string, options?: ErrorOptions) {
        super('The account was created, but automatic sign-in failed.', options);
        this.email = email;
        this.name = 'RegistrationCompletedLoginFailedError';
    }
}

export const useAuthStore = defineStore('auth', () => {
    clearLegacyStoredAuth();
    const user = ref<UserDto | null>(null);
    const accessToken = inMemoryAccessToken;
    const initialized = ref(false);
    const initializationError = ref<Error | null>(null);
    const lastAuthClearReason = ref<string | null>(null);
    const isAuthenticated = computed(() => Boolean(accessToken.value && user.value));
    let initializationPromise: Promise<void> | null = null;

    const setAuthData = (token: string, userData: UserDto) => {
        setAccessToken(token);
        user.value = userData;
        initialized.value = true;
    };

    const clearAuthData = () => {
        clearAccessToken();
        user.value = null;
    };

    authRefreshCoordinator.subscribe(event => {
        if (event.kind === 'authenticated' && event.auth) {
            lastAuthClearReason.value = null;
            setAuthData(event.auth.accessToken, event.auth.user);
        } else if (event.kind === 'cleared') {
            lastAuthClearReason.value = event.reason ?? 'session-revoked';
            clearAuthData();
            initialized.value = true;
        }
    });

    const initialize = async (): Promise<void> => {
        if (initialized.value) {
            return;
        }
        if (initializationPromise) {
            return initializationPromise;
        }

        initializationError.value = null;

        initializationPromise = (async () => {
            try {
                const auth = await authRefreshCoordinator.refreshAccessToken();
                setAuthData(auth.accessToken, auth.user);
                initialized.value = true;
            } catch (error) {
                if (isDefinitiveAuthInvalidation(error)) {
                    clearAuthData();
                    if (axios.isAxiosError(error) && error.response?.status === 401) {
                        authRefreshCoordinator.publishCleared('session-expired');
                    }
                    initialized.value = true;
                } else {
                    initializationError.value = error instanceof Error ? error : new Error('Unable to verify session.');
                }
            } finally {
                initializationPromise = null;
            }
        })();

        return initializationPromise;
    };

    const login = async (payload: LoginPayload) => {
        const response = await authApi.login(payload);
        setAuthData(response.data.accessToken, response.data.user);
        authRefreshCoordinator.publishAuthenticated(response.data);
    };

    const register = async (payload: RegisterPayload) => {
        await authApi.register(payload);
        try {
            await login({
                email: payload.email,
                password: payload.password,
                deviceName: getBrowserDeviceLabel(),
            });
        } catch (error) {
            throw new RegistrationCompletedLoginFailedError(payload.email, { cause: error });
        }
    };

    const logout = async () => {
        try {
            await authApi.logout();
        } finally {
            clearAuthData();
            initialized.value = true;
            authRefreshCoordinator.publishCleared('logout');
        }
    };

    const forgotPassword = async (email: string) => {
        await authApi.forgotPassword({ email });
    };

    const invalidateAuthentication = (reason: string) => {
        lastAuthClearReason.value = reason;
        clearAuthData();
        initialized.value = true;
        authRefreshCoordinator.publishCleared(reason);
    };

    return {
        user,
        accessToken,
        initialized,
        initializationError,
        lastAuthClearReason,
        isAuthenticated,
        initialize,
        login,
        register,
        logout,
        forgotPassword,
        clearAuthData,
        invalidateAuthentication,
    };
});

export function getBrowserDeviceLabel(userAgent = typeof navigator === 'undefined' ? '' : navigator.userAgent): string {
    const browser = /Edg\//.test(userAgent) ? 'Edge'
        : /Firefox\//.test(userAgent) ? 'Firefox'
            : /Chrome\//.test(userAgent) ? 'Chrome'
                : /Safari\//.test(userAgent) ? 'Safari'
                    : 'Web browser';
    const platform = /iPhone|iPad/.test(userAgent) ? (/iPhone/.test(userAgent) ? 'iPhone' : 'iPad')
        : /Windows/.test(userAgent) ? 'Windows'
            : /Android/.test(userAgent) ? 'Android'
                : /Macintosh|Mac OS X/.test(userAgent) ? 'macOS'
                    : /Linux/.test(userAgent) ? 'Linux'
                        : '';
    return platform ? `${browser} on ${platform}` : browser;
}
