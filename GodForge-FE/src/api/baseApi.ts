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
    async get<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T> {
        return (await axiosClient.get<T>(url, config)).data;
    },

    /**
     * Perform a POST request
     */
    async post<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
        return (await axiosClient.post<T>(url, data, config)).data;
    },

    /**
     * Perform a PUT request
     */
    async put<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
        return (await axiosClient.put<T>(url, data, config)).data;
    },

    /**
     * Perform a PATCH request
     */
    async patch<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
        return (await axiosClient.patch<T>(url, data, config)).data;
    },

    /**
     * Perform a DELETE request
     */
    async delete<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T> {
        return (await axiosClient.delete<T>(url, config)).data;
    }
};

export default baseApi;
