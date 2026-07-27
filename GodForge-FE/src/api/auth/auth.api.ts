import baseApi from '../baseApi';
import type { ApiResponse } from '../api.models';
import type {
    LoginPayload,
    RegisterPayload,
    RefreshPayload,
    LogoutPayload,
    ResetPasswordPayload,
    AuthResponseDto,
    RefreshResponseDto
} from './auth.models';

const API_PREFIX = '/auth';

export const authApi = {
    /**
     * Login with email and password
     */
    login(payload: LoginPayload): Promise<ApiResponse<AuthResponseDto>> {
        return baseApi.post<ApiResponse<AuthResponseDto>>(`${API_PREFIX}/login`, payload);
    },

    /**
     * Send OTP for registration verification
     */
    sendOtp(email: string): Promise<ApiResponse<void>> {
        return baseApi.post<ApiResponse<void>>(`${API_PREFIX}/register/send-otp`, { email });
    },

    /**
     * Register a new user
     */
    register(payload: RegisterPayload): Promise<ApiResponse<AuthResponseDto>> {
        return baseApi.post<ApiResponse<AuthResponseDto>>(`${API_PREFIX}/register`, payload);
    },

    /**
     * Refresh access and refresh tokens
     */
    refresh(payload: RefreshPayload): Promise<ApiResponse<RefreshResponseDto>> {
        return baseApi.post<ApiResponse<RefreshResponseDto>>(`${API_PREFIX}/refresh`, payload);
    },

    /**
     * Logout and invalidate refresh token
     */
    logout(payload: LogoutPayload): Promise<ApiResponse<void>> {
        return baseApi.post<ApiResponse<void>>(`${API_PREFIX}/logout`, payload);
    },

    /**
     * Reset password using the token delivered by email.
     */
    resetPassword(payload: ResetPasswordPayload): Promise<ApiResponse<void>> {
        return baseApi.post<ApiResponse<void>>(`${API_PREFIX}/reset-password`, payload);
    },

    /**
     * Request forgot password
     */
    forgotPassword(payload: { email: string }): Promise<ApiResponse<void>> {
        return baseApi.post<ApiResponse<void>>(`${API_PREFIX}/forgot-password`, payload);
    }
};

export default authApi;

