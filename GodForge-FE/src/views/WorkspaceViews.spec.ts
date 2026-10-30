import { flushPromises, mount } from '@vue/test-utils';
import { createMemoryHistory, createRouter } from 'vue-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { organizationsApi } from '@/api/workspace/organizations.api';
import { projectsApi } from '@/api/workspace/projects.api';
import DashboardView from './DashboardView.vue';
import OrganizationProjectsView from './OrganizationProjectsView.vue';
import ProjectDetailView from './ProjectDetailView.vue';
import ResourceState from '@/components/workspace/ResourceState.vue';
import { AxiosError } from 'axios';
const apiError = (status: number) => new AxiosError('safe', undefined, undefined, undefined, { status, data: {}, headers: {}, statusText: '', config: {} as never });
vi.mock('@/api/workspace/organizations.api', () => ({ organizationsApi: { list: vi.fn(), get: vi.fn(), create: vi.fn() } }));
vi.mock('@/api/workspace/projects.api', () => ({ projectsApi: { list: vi.fn(), catalog: vi.fn(), get: vi.fn(), create: vi.fn() } }));
const org = { id: 'a', name: 'Studio A', slug: 'studio-a', currentUserRole: 'organizationOwner', status: 'active', createdAt: '2026-10-01', updatedAt: '2026-10-01', version: 1 };
const project = { id: 'p', organizationId: 'a', name: 'Game', slug: 'game', description: '<script>untrusted</script>', visibility: 'private', currentUserRole: 'viewer', status: 'active', createdAt: '2026-10-01', updatedAt: '2026-10-01', version: 3 };
const empty = { data: [], meta: { page: 1, pageSize: 20, totalCount: 0 } };
async function render(component: typeof DashboardView | typeof OrganizationProjectsView | typeof ProjectDetailView, path = '/organizations/a/projects') {
    const router = createRouter({ history: createMemoryHistory(), routes: [
        { path: '/dashboard', name: 'dashboard', component: DashboardView },
        { path: '/organizations/:organizationId/projects', name: 'organizationProjects', component: OrganizationProjectsView },
        { path: '/organizations/:organizationId/projects/:projectId', name: 'projectDetail', component: ProjectDetailView },
        { path: '/organizations/:organizationId/projects/:projectId/members', name: 'projectMembers', component: ProjectDetailView },
        { path: '/organizations/:organizationId/projects/:projectId/settings', name: 'projectSettings', component: ProjectDetailView },
    ] });
    await router.push(path);
    const wrapper = mount(component, { global: { plugins: [router] } });
    return { wrapper, router };
}
describe('workspace views', () => {
    beforeEach(() => {
        vi.resetAllMocks();
        vi.mocked(organizationsApi.list).mockResolvedValue(empty);
        vi.mocked(organizationsApi.get).mockResolvedValue({ data: org });
        vi.mocked(projectsApi.list).mockResolvedValue(empty);
        vi.mocked(projectsApi.catalog).mockResolvedValue(empty);
        vi.mocked(projectsApi.get).mockResolvedValue({ data: project });
    });
    it('shows loading then onboarding and submits organization creation', async () => {
        let resolve!: (value: typeof empty) => void;
        vi.mocked(organizationsApi.list).mockImplementationOnce(() => new Promise(done => { resolve = done; }));
        const { wrapper, router } = await render(DashboardView, '/dashboard');
        expect(wrapper.text()).toContain('Loading workspace');
        resolve(empty);
        await flushPromises();
        expect(wrapper.text()).toContain('Create your first organization');
        vi.mocked(organizationsApi.create).mockResolvedValue({ data: org });
        await wrapper.findAll('input')[0]!.setValue('Studio A');
        await wrapper.findAll('input')[1]!.setValue('studio-a');
        await wrapper.find('form').trigger('submit'); await flushPromises();
        expect(organizationsApi.create).toHaveBeenCalledWith({ name: 'Studio A', slug: 'studio-a' }, expect.any(String));
        expect(router.currentRoute.value.params.organizationId).toBe('a');
        wrapper.unmount();
    });
    it('offers retry and recovers from a list failure', async () => {
        vi.mocked(organizationsApi.list).mockRejectedValueOnce(new Error('offline'));
        const { wrapper } = await render(DashboardView, '/dashboard'); await flushPromises();
        expect(wrapper.get('[role="alert"]').text()).toContain('retry');
        await wrapper.get('[role="alert"] button').trigger('click'); await flushPromises();
        expect(wrapper.text()).toContain('Create your first organization'); wrapper.unmount();
    });
    it('does not turn organization catalog metadata into content links', async () => {
        vi.mocked(projectsApi.catalog).mockResolvedValue({ ...empty, data: [{ id: 'private', organizationId: 'a', name: 'Admin metadata', slug: 'private', status: 'active', ownerCount: 1, memberCount: 2, createdAt: '2026-10-01', version: 1 }] });
        const { wrapper } = await render(OrganizationProjectsView); await flushPromises();
        await wrapper.findAll('button').find(button => button.text() === 'Organization catalog')!.trigger('click'); await flushPromises();
        expect(wrapper.text()).toContain('Admin metadata');
        expect(wrapper.text()).toContain('Administration metadata only');
        expect(wrapper.find('a').exists()).toBe(false);
        expect(projectsApi.get).not.toHaveBeenCalled(); wrapper.unmount();
    });
    it('hides project creation from organization members while showing assigned projects', async () => {
        vi.mocked(organizationsApi.get).mockResolvedValue({ data: { ...org, currentUserRole: 'organizationMember' } });
        vi.mocked(projectsApi.list).mockResolvedValue({ ...empty, data: [project] });
        const { wrapper } = await render(OrganizationProjectsView); await flushPromises();
        expect(wrapper.text()).not.toContain('New project');
        expect(wrapper.text()).not.toContain('Organization catalog');
        expect(wrapper.get('a').attributes('href')).toBe('/organizations/a/projects/p'); wrapper.unmount();
    });
    it('creates a project in the selected organization and navigates to its real detail', async () => {
        vi.mocked(projectsApi.create).mockResolvedValue({ data: project });
        const { wrapper, router } = await render(OrganizationProjectsView); await flushPromises();
        await wrapper.findAll('button').find(button => button.text() === 'New project')!.trigger('click');
        await wrapper.findAll('input')[0]!.setValue('Game'); await wrapper.findAll('input')[1]!.setValue('game');
        await wrapper.find('form').trigger('submit'); await flushPromises();
        expect(projectsApi.create).toHaveBeenCalledWith('a', { name: 'Game', slug: 'game', description: null, visibility: 'private' }, expect.any(String));
        expect(router.currentRoute.value.name).toBe('projectDetail'); wrapper.unmount();
    });
    it('renders only real detail metadata and escapes user text', async () => {
        const { wrapper } = await render(ProjectDetailView, '/organizations/a/projects/p'); await flushPromises();
        expect(wrapper.text()).toContain('<script>untrusted</script>');
        expect(wrapper.find('script').exists()).toBe(false);
        expect(wrapper.text()).toContain('viewer'); expect(wrapper.text()).toContain('Metadata version'); wrapper.unmount();
    });
    it.each([403, 404])('handles detail denial %s with no leaked content', async status => {
        vi.mocked(projectsApi.get).mockRejectedValue(apiError(status));
        const { wrapper } = await render(ProjectDetailView, '/organizations/a/projects/p'); await flushPromises();
        expect(wrapper.find('[role="alert"]').exists()).toBe(true);
        expect(wrapper.text()).not.toContain('untrusted'); wrapper.unmount();
    });
    it('rejects a mismatched route organization without rendering project data', async () => {
        const { wrapper } = await render(ProjectDetailView, '/organizations/b/projects/p'); await flushPromises();
        expect(wrapper.find('[role="alert"]').exists()).toBe(true);
        expect(wrapper.text()).not.toContain('untrusted');
        expect(organizationsApi.get).not.toHaveBeenCalled(); wrapper.unmount();
    });
    it('resource state exposes accessible loading and retry controls', async () => {
        const wrapper = mount(ResourceState, { props: { loading: true, error: '' } });
        expect(wrapper.find('[role="status"]').exists()).toBe(true);
        await wrapper.setProps({ loading: false, error: 'Unavailable' });
        await wrapper.get('button').trigger('click'); expect(wrapper.emitted('retry')).toHaveLength(1);
    });
});
