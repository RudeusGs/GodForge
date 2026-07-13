import axios, { type AxiosError, type InternalAxiosRequestConfig, type AxiosResponse } from 'axios';

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5072/api/v1';

const axiosClient = axios.create({
    baseURL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 30000,
});

// Request Interceptor: Attach JWT Token
axiosClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const token = localStorage.getItem('access_token') || sessionStorage.getItem('access_token');
        if (token && config.headers) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error: AxiosError) => {
        return Promise.reject(error);
    }
);

let isRefreshing = false;
let refreshSubscribers: ((token: string) => void)[] = [];

function onRefreshed(token: string) {
    refreshSubscribers.forEach(cb => cb(token));
    refreshSubscribers = [];
}

function addRefreshSubscriber(cb: (token: string) => void) {
    refreshSubscribers.push(cb);
}

// Response Interceptor: Handle 401/403 and unwrap data
axiosClient.interceptors.response.use(
    (response: AxiosResponse) => {
        return response.data;
    },
    async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;
            
            const isLocal = !!localStorage.getItem('refresh_token');
            const refreshToken = localStorage.getItem('refresh_token') || sessionStorage.getItem('refresh_token');
            if (!refreshToken) {
                localStorage.removeItem('access_token');
                sessionStorage.removeItem('access_token');
                window.location.href = '/login';
                return Promise.reject(error);
            }

            if (!isRefreshing) {
                isRefreshing = true;
                try {
                    const response = await axios.post(`${baseURL}/auth/refresh`, { refreshToken });
                    const newAccessToken = response.data.data.accessToken;
                    const newRefreshToken = response.data.data.refreshToken;
                    const storage = isLocal ? localStorage : sessionStorage;
                    storage.setItem('access_token', newAccessToken);
                    storage.setItem('refresh_token', newRefreshToken);
                    isRefreshing = false;
                    onRefreshed(newAccessToken);
                    originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
                    return axiosClient(originalRequest);
                } catch (refreshError) {
                    isRefreshing = false;
                    refreshSubscribers = [];
                    localStorage.removeItem('access_token');
                    localStorage.removeItem('refresh_token');
                    sessionStorage.removeItem('access_token');
                    sessionStorage.removeItem('refresh_token');
                    window.location.href = '/login';
                    return Promise.reject(refreshError);
                }
            } else {
                return new Promise(resolve => {
                    addRefreshSubscriber(token => {
                        if (originalRequest.headers) {
                            originalRequest.headers.Authorization = `Bearer ${token}`;
                        }
                        resolve(axiosClient(originalRequest));
                    });
                });
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
