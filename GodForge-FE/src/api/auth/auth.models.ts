export interface UserDto {
    id: string;
    email: string;
    displayName: string;
    systemRole: string;
    status: string;
    avatarUrl?: string | null;
}

export interface AuthResponseDto {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
    user: UserDto;
}

export interface RefreshResponseDto {
    accessToken: string;
    refreshToken: string;
}

// Request Payloads (Separated as requested)

export interface LoginPayload {
    email: string;
    password?: string;
}

export interface RegisterPayload {
    email: string;
    displayName: string;
    password?: string;
    otp: string;
}

export interface RefreshPayload {
    refreshToken: string;
}

export interface LogoutPayload {
    refreshToken: string;
}

export interface SetupPasswordPayload {
    token: string;
    password?: string;
}

export interface ForgotPasswordPayload {
    email: string;
}
