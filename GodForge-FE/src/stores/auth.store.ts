import { defineStore } from 'pinia';
import { ref } from 'vue';
import { authApi } from '../api/auth/auth.api';
import type { LoginPayload, RegisterPayload, UserDto } from '../api/auth/auth.models';

const ACCESS_TOKEN_KEY = 'access_token';
const USER_KEY = 'auth_user';

function getActiveStorage(): Storage | null {
    if (localStorage.getItem(ACCESS_TOKEN_KEY)) {
        return localStorage;
    }
    if (sessionStorage.getItem(ACCESS_TOKEN_KEY)) {
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
        storage.removeItem(USER_KEY);
    }
}

export const useAuthStore = defineStore('auth', () => {
    const activeStorage = getActiveStorage();
    const initialAccessToken = activeStorage?.getItem(ACCESS_TOKEN_KEY) ?? null;

    const user = ref<UserDto | null>(readStoredUser(activeStorage));
    const accessToken = ref<string | null>(initialAccessToken);
    const isAuthenticated = ref<boolean>(Boolean(initialAccessToken && user.value));

    if (!isAuthenticated.value) {
        clearStoredAuth();
        user.value = null;
        accessToken.value = null;
    }

    const setAuthData = (
        token: string,
        userData: UserDto,
        rememberMe: boolean = true
    ) => {
        clearStoredAuth();
        const storage = rememberMe ? localStorage : sessionStorage;
        storage.setItem(ACCESS_TOKEN_KEY, token);
        storage.setItem(USER_KEY, JSON.stringify(userData));

        accessToken.value = token;
        user.value = userData;
        isAuthenticated.value = true;
    };

    const login = async (payload: LoginPayload, rememberMe: boolean = true) => {
        const response = await authApi.login(payload);
        const { accessToken: token, user: userData } = response.data;
        setAuthData(token, userData, rememberMe);
    };

    const register = async (payload: RegisterPayload) => {
        await authApi.register(payload);
        await login({
            email: payload.email,
            password: payload.password,
            deviceName: 'GodForge Web',
        }, true);
    };

    const clearAuthData = () => {
        clearStoredAuth();
        user.value = null;
        accessToken.value = null;
        isAuthenticated.value = false;
    };

    const logout = async () => {
        try {
            await authApi.logout();
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
