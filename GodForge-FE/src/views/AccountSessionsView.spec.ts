import { createPinia } from 'pinia';
import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { authApi } from '@/api/auth/auth.api';
import AccountSessionsView from './AccountSessionsView.vue';

const routerPush = vi.fn();

vi.mock('vue-router', async importOriginal => {
    const actual = await importOriginal<typeof import('vue-router')>();
    return {
        ...actual,
        useRouter: () => ({ push: routerPush }),
    };
});

vi.mock('@/api/auth/auth.api', () => ({
    authApi: {
        getSessions: vi.fn(),
        revokeSession: vi.fn(),
    },
}));

const currentSession = {
    id: '11111111-1111-1111-1111-111111111111',
    deviceName: 'Chrome on Windows',
    createdAt: '2026-08-10T10:00:00Z',
    lastSeenAt: '2026-08-10T11:00:00Z',
    expiresAt: '2026-09-10T10:00:00Z',
    current: true,
    revokedAt: null,
};

const otherSession = {
    ...currentSession,
    id: '22222222-2222-2222-2222-222222222222',
    deviceName: 'Firefox on Linux',
    current: false,
};

describe('AccountSessionsView', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('loads sessions and labels the current session', async () => {
        vi.mocked(authApi.getSessions).mockResolvedValue({ data: [currentSession, otherSession] });

        const wrapper = mount(AccountSessionsView, {
            global: {
                plugins: [createPinia()],
                stubs: { RouterLink: { template: '<a><slot /></a>' } },
            },
        });
        await flushPromises();

        expect(wrapper.text()).toContain('Chrome on Windows');
        expect(wrapper.text()).toContain('Firefox on Linux');
        expect(wrapper.text()).toContain('Current');
    });

    it('revokes another session and refreshes authoritative server state', async () => {
        vi.mocked(authApi.getSessions)
            .mockResolvedValueOnce({ data: [currentSession, otherSession] })
            .mockResolvedValueOnce({ data: [currentSession] });
        vi.mocked(authApi.revokeSession).mockResolvedValue(undefined);

        const wrapper = mount(AccountSessionsView, {
            global: {
                plugins: [createPinia()],
                stubs: { RouterLink: { template: '<a><slot /></a>' } },
            },
        });
        await flushPromises();

        await wrapper.get('[aria-label="Revoke session for Firefox on Linux"]').trigger('click');
        await flushPromises();

        expect(authApi.revokeSession).toHaveBeenCalledWith(otherSession.id);
        expect(authApi.getSessions).toHaveBeenCalledTimes(2);
        expect(wrapper.text()).not.toContain('Firefox on Linux');
    });
});
