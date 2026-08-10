import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { authApi } from '../api/auth/auth.api';
import {
    clearAccessToken,
    clearLegacyStoredAuth,
    inMemoryAccessToken,
    setAccessToken,
} from '../api/auth/authSession';
import type { LoginPayload, RegisterPayload, UserDto } from '../api/auth/auth.models';

export const useAuthStore = defineStore('auth', () => {
    clearLegacyStoredAuth();
    const user = ref<UserDto | null>(null);
    const accessToken = inMemoryAccessToken;
    const initialized = ref(false);
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

    const initialize = async (): Promise<void> => {
        if (initialized.value) {
            return;
        }
        if (initializationPromise) {
            return initializationPromise;
        }

        initializationPromise = (async () => {
            try {
                const response = await authApi.refresh();
                setAuthData(response.data.accessToken, response.data.user);
            } catch {
                clearAuthData();
            } finally {
                initialized.value = true;
                initializationPromise = null;
            }
        })();

        return initializationPromise;
    };

    const login = async (payload: LoginPayload) => {
        const response = await authApi.login(payload);
        setAuthData(response.data.accessToken, response.data.user);
    };

    const register = async (payload: RegisterPayload) => {
        await authApi.register(payload);
        await login({
            email: payload.email,
            password: payload.password,
            deviceName: 'GodForge Web',
        });
    };

    const logout = async () => {
        try {
            await authApi.logout();
        } finally {
            clearAuthData();
            initialized.value = true;
        }
    };

    const forgotPassword = async (email: string) => {
        await authApi.forgotPassword({ email });
    };

    return {
        user,
        accessToken,
        initialized,
        isAuthenticated,
        initialize,
        login,
        register,
        logout,
        forgotPassword,
        clearAuthData,
    };
});
