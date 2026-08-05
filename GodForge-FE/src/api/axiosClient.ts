import axios, { type AxiosError, type InternalAxiosRequestConfig, type AxiosResponse } from 'axios';

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5072/api/v1';

const axiosClient = axios.create({
    baseURL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 30000,
    withCredentials: true,
});

function getStoredToken(key: string): string | null {
    return localStorage.getItem(key) || sessionStorage.getItem(key);
}

function clearStoredAuth(): void {
    for (const storage of [localStorage, sessionStorage]) {
        storage.removeItem('access_token');
        storage.removeItem('auth_user');
    }
}

function getActiveStorage(): Storage {
    return localStorage.getItem('access_token') ? localStorage : sessionStorage;
}

function redirectToLogin(): void {
    if (window.location.pathname !== '/login') {
        window.location.assign('/login');
    }
}

axiosClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const token = getStoredToken('access_token');
        if (token && config.headers) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error: AxiosError) => Promise.reject(error)
);

type RefreshSubscriber = {
    resolve: (token: string) => void;
    reject: (error: unknown) => void;
};

let isRefreshing = false;
let refreshSubscribers: RefreshSubscriber[] = [];

function resolveRefreshSubscribers(token: string): void {
    const subscribers = refreshSubscribers;
    refreshSubscribers = [];
    subscribers.forEach(subscriber => subscriber.resolve(token));
}

function rejectRefreshSubscribers(error: unknown): void {
    const subscribers = refreshSubscribers;
    refreshSubscribers = [];
    subscribers.forEach(subscriber => subscriber.reject(error));
}

axiosClient.interceptors.response.use(
    (response: AxiosResponse) => response.data,
    async (error: AxiosError) => {
        const originalRequest = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;
        const isRefreshRequest = originalRequest?.url?.endsWith('/auth/refresh') ?? false;

        if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isRefreshRequest) {
            originalRequest._retry = true;

            if (isRefreshing) {
                return new Promise((resolve, reject) => {
                    refreshSubscribers.push({
                        resolve: token => {
                            originalRequest.headers.Authorization = `Bearer ${token}`;
                            resolve(axiosClient(originalRequest));
                        },
                        reject,
                    });
                });
            }

            isRefreshing = true;
            try {
                const response = await axios.post(
                    `${baseURL}/auth/refresh`,
                    undefined,
                    { timeout: 30000, withCredentials: true }
                );
                const newAccessToken = response.data.data.accessToken as string;
                const storage = getActiveStorage();

                storage.setItem('access_token', newAccessToken);
                originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
                resolveRefreshSubscribers(newAccessToken);
                return axiosClient(originalRequest);
            } catch (refreshError) {
                rejectRefreshSubscribers(refreshError);
                clearStoredAuth();
                redirectToLogin();
                return Promise.reject(refreshError);
            } finally {
                isRefreshing = false;
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
