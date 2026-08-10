import { ref } from 'vue';

export const inMemoryAccessToken = ref<string | null>(null);

export function getAccessToken(): string | null {
    return inMemoryAccessToken.value;
}

export function setAccessToken(token: string): void {
    inMemoryAccessToken.value = token;
}

export function clearAccessToken(): void {
    inMemoryAccessToken.value = null;
}

export function clearLegacyStoredAuth(): void {
    if (typeof window === 'undefined') {
        return;
    }

    for (const storageName of ['localStorage', 'sessionStorage'] as const) {
        try {
            const storage = window[storageName];
            storage.removeItem('access_token');
            storage.removeItem('auth_user');
        } catch {
            // Storage may be disabled by the browser; credentials are never written there.
        }
    }
}
