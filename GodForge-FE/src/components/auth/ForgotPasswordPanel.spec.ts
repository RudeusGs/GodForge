import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ForgotPasswordPanel from './ForgotPasswordPanel.vue';

const { forgotPasswordMock } = vi.hoisted(() => ({
    forgotPasswordMock: vi.fn(),
}));

vi.mock('../../stores/auth.store', () => ({
    useAuthStore: () => ({ forgotPassword: forgotPasswordMock }),
}));

const mountPanel = () => mount(ForgotPasswordPanel, {
    global: {
        stubs: {
            RouterLink: { template: '<a><slot /></a>' },
        },
    },
});

describe('ForgotPasswordPanel', () => {
    beforeEach(() => {
        forgotPasswordMock.mockReset();
        forgotPasswordMock.mockResolvedValue(undefined);
    });

    it('shows accessible validation without sending an empty request', async () => {
        const wrapper = mountPanel();
        await wrapper.get('form').trigger('submit');

        expect(wrapper.text()).toContain('Enter your email address.');
        expect(wrapper.get('input').attributes('aria-invalid')).toBe('true');
        expect(forgotPasswordMock).not.toHaveBeenCalled();
    });

    it('shows the uniform inbox state after an accepted request', async () => {
        const wrapper = mountPanel();
        await wrapper.get('input').setValue('member@example.com');
        await wrapper.get('form').trigger('submit');
        await flushPromises();

        expect(forgotPasswordMock).toHaveBeenCalledWith('member@example.com');
        expect(wrapper.text()).toContain('Check your inbox.');
        expect(wrapper.text()).toContain('If an account matches');
    });
});
