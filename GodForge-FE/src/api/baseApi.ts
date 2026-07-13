import axiosClient from './axiosClient';
import type { AxiosRequestConfig } from 'axios';

/**
 * Base API service providing standard HTTP methods wrapper.
 * This ensures all requests go through the configured axiosClient with its interceptors.
 */
export const baseApi = {
    /**
     * Perform a GET request
     */
    get<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T> {
        return axiosClient.get<T, T>(url, config);
    },

    /**
     * Perform a POST request
     */
    post<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
        return axiosClient.post<T, T>(url, data, config);
    },

    /**
     * Perform a PUT request
     */
    put<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
        return axiosClient.put<T, T>(url, data, config);
    },

    /**
     * Perform a PATCH request
     */
    patch<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
        return axiosClient.patch<T, T>(url, data, config);
    },

    /**
     * Perform a DELETE request
     */
    delete<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T> {
        return axiosClient.delete<T, T>(url, config);
    }
};

export default baseApi;
