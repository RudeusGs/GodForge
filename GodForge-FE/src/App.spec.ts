import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { nextTick } from 'vue';
import { describe, expect, it } from 'vitest';
import App from './App.vue';
import { useAuthStore } from './stores/auth.store';

describe('App bootstrap state', () => {
    it('shows the branded loader until authentication initialization finishes', async () => {
        const pinia = createPinia();
        setActivePinia(pinia);
        const wrapper = mount(App, {
            global: {
                plugins: [pinia],
                stubs: {
                    RouterView: { template: '<div data-testid="router-view">Application</div>' },
                },
            },
        });
        const authStore = useAuthStore();

        expect(wrapper.text()).toContain('Preparing your workspace');
        expect(wrapper.find('[data-testid="router-view"]').exists()).toBe(false);

        authStore.initialized = true;
        await nextTick();

        expect(wrapper.find('[data-testid="router-view"]').exists()).toBe(true);
    });
});
