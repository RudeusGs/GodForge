import { defineStore } from 'pinia';
import { ref } from 'vue';
import { authApi } from '../api/auth/auth.api';
import type { LoginPayload, RegisterPayload, UserDto } from '../api/auth/auth.models';

export const useAuthStore = defineStore('auth', () => {
    const user = ref<UserDto | null>(null);
    const getToken = (key: string) => localStorage.getItem(key) || sessionStorage.getItem(key);
    const accessToken = ref<string | null>(getToken('access_token'));
    const isAuthenticated = ref<boolean>(!!accessToken.value);

    const setAuthData = (token: string, refreshToken: string, userData: UserDto, rememberMe: boolean = true) => {
        accessToken.value = token;
        user.value = userData;
        isAuthenticated.value = true;
        
        const storage = rememberMe ? localStorage : sessionStorage;
        // Clear both just to be safe
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        sessionStorage.removeItem('access_token');
        sessionStorage.removeItem('refresh_token');
        
        storage.setItem('access_token', token);
        storage.setItem('refresh_token', refreshToken);
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

    const logout = () => {
        user.value = null;
        accessToken.value = null;
        isAuthenticated.value = false;
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        sessionStorage.removeItem('access_token');
        sessionStorage.removeItem('refresh_token');
        // If API call is needed, you could do it here, but typically handled via interceptors or components
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
        forgotPassword
    };
});
