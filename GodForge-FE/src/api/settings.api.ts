import baseApi from './baseApi';
import type { ApiResponse } from './api.models';

export interface UserPreferencesInput {
    theme: 'light' | 'dark';
    notificationInApp: boolean;
    notificationEmail: boolean;
    notificationDigest: 'off' | 'daily' | 'weekly';
    version: number;
}
export interface UserPreferences extends UserPreferencesInput { createdAt: string; updatedAt: string }
export interface OrganizationPolicy {
    organizationId: string;
    aiAdvisoryAllowed: boolean;
    platformAiAdvisoryAllowed: boolean;
    version: number;
    createdAt: string;
    updatedAt: string;
}
export const settingsApi = {
    getPreferences: (): Promise<ApiResponse<UserPreferences>> => baseApi.get('/users/me/settings'),
    updatePreferences: (input: UserPreferencesInput): Promise<ApiResponse<UserPreferences>> => baseApi.put('/users/me/settings', input),
    getPolicy: (id: string): Promise<ApiResponse<OrganizationPolicy>> => baseApi.get(`/organizations/${encodeURIComponent(id)}/settings`),
    updatePolicy: (id: string, input: { aiAdvisoryAllowed: boolean; version: number }): Promise<ApiResponse<OrganizationPolicy>> => baseApi.put(`/organizations/${encodeURIComponent(id)}/settings`, input),
};
