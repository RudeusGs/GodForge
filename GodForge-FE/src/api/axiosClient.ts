import axios, { type AxiosError, type InternalAxiosRequestConfig, type AxiosResponse } from 'axios';
import { clearAccessToken, getAccessToken, setAccessToken } from './auth/authSession';
import { authRefreshCoordinator, isDefinitiveAuthInvalidation } from './auth/authRefreshCoordinator';

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5072/api/v1';

const axiosClient = axios.create({
    baseURL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 30000,
    withCredentials: true,
});

function redirectToLogin(): void {
    if (window.location.pathname !== '/login') {
        window.location.assign('/login');
    }
}

axiosClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const token = getAccessToken();
        if (token && config.headers) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error: AxiosError) => Promise.reject(error)
);

axiosClient.interceptors.response.use(
    (response: AxiosResponse) => response.data,
    async (error: AxiosError) => {
        const originalRequest = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;
        const isRefreshRequest = originalRequest?.url?.endsWith('/auth/refresh') ?? false;
        const isLoginRequest = originalRequest?.url?.endsWith('/auth/login') ?? false;

        if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isRefreshRequest && !isLoginRequest) {
            originalRequest._retry = true;

            try {
                const auth = await authRefreshCoordinator.refreshAccessToken();
                setAccessToken(auth.accessToken);
                originalRequest.headers.Authorization = `Bearer ${auth.accessToken}`;
                return axiosClient(originalRequest);
            } catch (refreshError) {
                if (isDefinitiveAuthInvalidation(refreshError)) {
                    clearAccessToken();
                    if (axios.isAxiosError(refreshError) && refreshError.response?.status === 401) {
                        authRefreshCoordinator.publishCleared('session-expired');
                    }
                    redirectToLogin();
                }
                return Promise.reject(refreshError);
            }
        }

        if (error.response?.status === 403) {
            console.error('RBAC Error: You do not have permission for this action.');
        }

        if (error.response?.status === 404) {
            console.error('Not Found: The requested resource does not exist.');
        }

        return Promise.reject(error);
    }
);

export default axiosClient;
