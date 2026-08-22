import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import LandingView from './LandingView.vue';

const authState = vi.hoisted(() => ({ isAuthenticated: false }));

vi.mock('@/stores/auth.store', () => ({
    useAuthStore: () => authState,
}));

const mountLanding = () => mount(LandingView, {
    global: {
        stubs: {
            RouterLink: {
                props: ['to'],
                template: '<a :data-to="to"><slot /></a>',
            },
        },
    },
});

describe('LandingView', () => {
    beforeEach(() => {
        authState.isAuthenticated = false;
    });

    it('presents product positioning and public authentication actions', () => {
        const wrapper = mountLanding();

        expect(wrapper.text()).toContain('A specialized Git and project-intelligence platform');
        expect(wrapper.find('a[data-to="/login"]').exists()).toBe(true);
        expect(wrapper.find('a[data-to="/register"]').exists()).toBe(true);
        expect(wrapper.get('.scene-stage').attributes('aria-hidden')).toBe('true');
        expect(wrapper.findAll('.bento-card')).toHaveLength(3);
        expect(wrapper.text()).toContain('See how everything connects.');
        expect(wrapper.text()).toContain('Share the project.');
    });

    it('offers the dashboard action to an authenticated user', () => {
        authState.isAuthenticated = true;
        const wrapper = mountLanding();

        expect(wrapper.text()).toContain('Open dashboard');
        expect(wrapper.find('a[data-to="/dashboard"]').exists()).toBe(true);
    });
});
