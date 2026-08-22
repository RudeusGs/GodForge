import baseApi from '../baseApi';
import type { ApiResponse } from '../api.models';
import type {
    AuthResponseDto,
    ChallengeAcceptedDto,
    ForgotPasswordPayload,
    LoginPayload,
    RegisterPayload,
    ResetPasswordPayload,
    UserDto,
    SessionDto,
} from './auth.models';
import { authRefreshCoordinator } from './authRefreshCoordinator';

const API_PREFIX = '/auth';

export const authApi = {
    login(payload: LoginPayload): Promise<ApiResponse<AuthResponseDto>> {
        return baseApi.post<ApiResponse<AuthResponseDto>>(`${API_PREFIX}/login`, payload);
    },

    sendOtp(email: string): Promise<ApiResponse<ChallengeAcceptedDto>> {
        return baseApi.post<ApiResponse<ChallengeAcceptedDto>>(`${API_PREFIX}/send-otp`, { email });
    },

    register(payload: RegisterPayload): Promise<ApiResponse<UserDto>> {
        return baseApi.post<ApiResponse<UserDto>>(`${API_PREFIX}/register`, payload);
    },

    refresh(): Promise<AuthResponseDto> {
        return authRefreshCoordinator.refreshAccessToken();
    },

    logout(): Promise<void> {
        return baseApi.post<void>(`${API_PREFIX}/logout`);
    },

    resetPassword(payload: ResetPasswordPayload): Promise<void> {
        return baseApi.post<void>(`${API_PREFIX}/reset-password`, payload);
    },

    forgotPassword(payload: ForgotPasswordPayload): Promise<ApiResponse<ChallengeAcceptedDto>> {
        return baseApi.post<ApiResponse<ChallengeAcceptedDto>>(`${API_PREFIX}/forgot-password`, payload);
    },

    getMe(): Promise<ApiResponse<UserDto>> {
        return baseApi.get<ApiResponse<UserDto>>('/users/me');
    },

    getSessions(): Promise<ApiResponse<SessionDto[]>> {
        return baseApi.get<ApiResponse<SessionDto[]>>('/users/me/sessions');
    },

    revokeSession(sessionId: string): Promise<void> {
        return baseApi.delete<void>(`/users/me/sessions/${encodeURIComponent(sessionId)}`);
    },
};

export default authApi;
