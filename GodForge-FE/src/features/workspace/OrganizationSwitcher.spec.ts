import { flushPromises, mount } from '@vue/test-utils';
import { createMemoryHistory, createRouter } from 'vue-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { organizationsApi } from '@/api/workspace/organizations.api';
import OrganizationSwitcher from './OrganizationSwitcher.vue';
vi.mock('@/api/workspace/organizations.api', () => ({ organizationsApi: { list: vi.fn() } }));
const org = { id: 'a', name: 'Studio A', slug: 'studio-a', currentUserRole: 'organizationOwner', status: 'active', createdAt: '', updatedAt: '', version: 1 };
async function render() {
    const router = createRouter({ history: createMemoryHistory(), routes: [
        { path: '/dashboard', component: { template: '<div />' } },
        { path: '/organizations/:organizationId/projects', name: 'organizationProjects', component: { template: '<div />' } },
    ] });
    await router.push('/dashboard');
    const wrapper = mount(OrganizationSwitcher, { props: { selected: '' }, global: { plugins: [router] } });
    return { wrapper, router };
}
describe('organization switcher', () => {
    beforeEach(() => vi.resetAllMocks());
    it('paginates organizations and uses the chosen server ID in the route', async () => {
        vi.mocked(organizationsApi.list).mockImplementation(async page => ({ data: [{ ...org, id: page === 2 ? 'b' : 'a', name: page === 2 ? 'Studio B' : 'Studio A' }], meta: { page: page ?? 1, pageSize: 20, totalCount: 21 } }));
        const { wrapper, router } = await render(); await flushPromises();
        await wrapper.findAll('button').find(button => button.text() === 'Next')!.trigger('click'); await flushPromises();
        expect(organizationsApi.list).toHaveBeenLastCalledWith(2);
        await wrapper.get('select').setValue('b'); await flushPromises();
        expect(router.currentRoute.value.params.organizationId).toBe('b');
        wrapper.unmount();
    });
    it('recovers an organization-list failure through its own retry control', async () => {
        vi.mocked(organizationsApi.list).mockRejectedValueOnce(new Error('offline')).mockResolvedValue({ data: [], meta: { page: 1, pageSize: 20, totalCount: 0 } });
        const { wrapper } = await render(); await flushPromises();
        expect(wrapper.find('[role="alert"]').exists()).toBe(true);
        await wrapper.get('button').trigger('click'); await flushPromises();
        expect(wrapper.text()).toContain('Create your first organization'); wrapper.unmount();
    });
});
