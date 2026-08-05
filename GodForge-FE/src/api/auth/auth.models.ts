export interface UserDto {
    id: string;
    email: string;
    displayName: string;
    status: string;
    emailVerifiedAt: string | null;
    createdAt: string;
    version: number;
}

export interface SessionDto {
    id: string;
    deviceName: string | null;
    createdAt: string;
    lastSeenAt: string | null;
    expiresAt: string;
    current: boolean;
    revokedAt: string | null;
}

export interface AuthResponseDto {
    user: UserDto;
    session: SessionDto;
    accessToken: string;
    accessTokenExpiresAt: string;
    refreshTokenExpiresAt: string;
}

export interface ChallengeAcceptedDto {
    requestAccepted: boolean;
    resendAfterSeconds: number;
}

export interface LoginPayload {
    email: string;
    password: string;
    deviceName?: string | null;
}

export interface RegisterPayload {
    email: string;
    displayName: string;
    password: string;
    otp: string;
}

export interface ResetPasswordPayload {
    email: string;
    token: string;
    newPassword: string;
}

export interface ForgotPasswordPayload {
    email: string;
}
