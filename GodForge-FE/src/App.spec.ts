import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { nextTick } from 'vue';
import { describe, expect, it } from 'vitest';
import App from './App.vue';
import { useAuthStore } from './stores/auth.store';
import { createMemoryHistory, createRouter } from 'vue-router';

async function mountApplication(path = '/') {
    const pinia = createPinia();
    setActivePinia(pinia);
    const router = createRouter({
        history: createMemoryHistory(),
        routes: [
            { path: '/', component: { template: '<div data-testid="application">Application</div>' } },
            { path: '/dashboard', meta: { requiresAuth: true }, component: { template: '<h1>Private dashboard</h1>' } },
            { path: '/login', name: 'login', component: { template: '<h1>Sign in</h1>' } },
        ],
    });
    await router.push(path);
    await router.isReady();
    const wrapper = mount(App, { global: { plugins: [pinia, router] } });
    return { wrapper, router, authStore: useAuthStore() };
}

describe('App bootstrap state', () => {
    it('shows the branded loader until authentication initialization finishes', async () => {
        const { wrapper, authStore } = await mountApplication();

        expect(wrapper.text()).toContain('Preparing your workspace');
        expect(wrapper.find('[data-testid="application"]').exists()).toBe(false);

        authStore.initialized = true;
        await nextTick();

        expect(wrapper.find('[data-testid="application"]').exists()).toBe(true);
        wrapper.unmount();
    });

    it('redirects a protected route when a bootstrap retry establishes an anonymous session', async () => {
        const { wrapper, router, authStore } = await mountApplication('/dashboard');
        expect(wrapper.text()).not.toContain('Private dashboard');

        // A transient failure leaves initialized=false and isAuthenticated=false.
        // A definitive 401 changes only initialized, which must trigger the guard.
        authStore.initialized = true;
        await flushPromises();

        expect(router.currentRoute.value.name).toBe('login');
        expect(router.currentRoute.value.query.returnTo).toBe('/dashboard');
        expect(wrapper.text()).not.toContain('Private dashboard');
        expect(wrapper.text()).toContain('Sign in');
        wrapper.unmount();
    });
});
