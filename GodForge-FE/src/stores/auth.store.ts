import { defineStore } from 'pinia';
import { ref } from 'vue';
import { authApi } from '../api/auth/auth.api';
import type { LoginPayload, RegisterPayload, UserDto } from '../api/auth/auth.models';

const ACCESS_TOKEN_KEY = 'access_token';
const REFRESH_TOKEN_KEY = 'refresh_token';
const USER_KEY = 'auth_user';

function getStoredValue(key: string): string | null {
    return localStorage.getItem(key) || sessionStorage.getItem(key);
}

function getActiveStorage(): Storage | null {
    if (localStorage.getItem(REFRESH_TOKEN_KEY)) {
        return localStorage;
    }
    if (sessionStorage.getItem(REFRESH_TOKEN_KEY)) {
        return sessionStorage;
    }
    return null;
}

function readStoredUser(storage: Storage | null): UserDto | null {
    const raw = storage?.getItem(USER_KEY) ?? null;
    if (!raw) {
        return null;
    }

    try {
        return JSON.parse(raw) as UserDto;
    } catch {
        return null;
    }
}

function clearStoredAuth(): void {
    for (const storage of [localStorage, sessionStorage]) {
        storage.removeItem(ACCESS_TOKEN_KEY);
        storage.removeItem(REFRESH_TOKEN_KEY);
        storage.removeItem(USER_KEY);
    }
}

export const useAuthStore = defineStore('auth', () => {
    const activeStorage = getActiveStorage();
    const initialAccessToken = activeStorage?.getItem(ACCESS_TOKEN_KEY) ?? null;
    const initialRefreshToken = activeStorage?.getItem(REFRESH_TOKEN_KEY) ?? null;

    const user = ref<UserDto | null>(readStoredUser(activeStorage));
    const accessToken = ref<string | null>(initialAccessToken);
    const isAuthenticated = ref<boolean>(Boolean(initialAccessToken && initialRefreshToken && user.value));

    if (!isAuthenticated.value) {
        clearStoredAuth();
        user.value = null;
        accessToken.value = null;
    }

    const setAuthData = (
        token: string,
        refreshToken: string,
        userData: UserDto,
        rememberMe: boolean = true
    ) => {
        clearStoredAuth();
        const storage = rememberMe ? localStorage : sessionStorage;
        storage.setItem(ACCESS_TOKEN_KEY, token);
        storage.setItem(REFRESH_TOKEN_KEY, refreshToken);
        storage.setItem(USER_KEY, JSON.stringify(userData));

        accessToken.value = token;
        user.value = userData;
        isAuthenticated.value = true;
    };

    const login = async (payload: LoginPayload, rememberMe: boolean = true) => {
        const response = await authApi.login(payload);
        const { accessToken: token, refreshToken, user: userData } = response.data;
        setAuthData(token, refreshToken, userData, rememberMe);
    };

    const register = async (payload: RegisterPayload) => {
        const response = await authApi.register(payload);
        const { accessToken: token, refreshToken, user: userData } = response.data;
        setAuthData(token, refreshToken, userData, true);
    };

    const clearAuthData = () => {
        clearStoredAuth();
        user.value = null;
        accessToken.value = null;
        isAuthenticated.value = false;
    };

    const logout = async () => {
        const refreshToken = getStoredValue(REFRESH_TOKEN_KEY);
        try {
            await authApi.logout({ refreshToken });
        } finally {
            clearAuthData();
        }
    };

    const forgotPassword = async (email: string) => {
        await authApi.forgotPassword({ email });
    };

    return {
        user,
        accessToken,
        isAuthenticated,
        login,
        register,
        logout,
        forgotPassword,
        clearAuthData,
    };
});
