import { flushPromises, mount } from '@vue/test-utils';
import { createMemoryHistory, createRouter } from 'vue-router';
import { createPinia, setActivePinia } from 'pinia';
import { AxiosError } from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { projectsApi } from '@/api/workspace/projects.api';
import { organizationsApi } from '@/api/workspace/organizations.api';
import type { Project, ProjectMember, ProjectSettings } from '@/api/workspace/workspace.models';
import { useAuthStore } from '@/stores/auth.store';
import Members from './ProjectMembersView.vue';
import Settings from './ProjectSettingsView.vue';
vi.mock('@/api/workspace/projects.api');
vi.mock('@/api/workspace/organizations.api');
const org = { id: 'a', name: 'Studio', slug: 'studio', currentUserRole: 'organizationMember', status: 'active', createdAt: '2026-10-01', updatedAt: '2026-10-01', version: 1 };
const project: Project = { id: 'p', organizationId: 'a', name: 'Game', slug: 'game', description: 'Real description', visibility: 'private', currentUserRole: 'projectOwner', status: 'active', createdAt: '2026-10-01', updatedAt: '2026-10-01', version: 7 };
const member: ProjectMember = { userId: 'u', displayName: '<script>Member</script>', email: 'member@example.test', role: 'developer', status: 'active', joinedAt: '2026-10-02', version: 4 };
const settings: ProjectSettings = { projectId: 'p', analysisProfileKey: 'current-default-v1', aiAdvisoryEnabled: false, defaultAssetVisibility: 'private', notificationPolicyVersion: 1, sourceScope: 'project', version: 2 };
const paged = (rows: ProjectMember[] = [member], totalCount = rows.length) => ({ data: rows, meta: { page: 1, pageSize: 20, totalCount } });
const failure = (code: string) => new AxiosError('conflict', undefined, undefined, undefined, { status: 409, data: { error: { code } }, config: {} as never, headers: {}, statusText: '' });
async function render(component: typeof Members | typeof Settings, path = '/organizations/a/projects/p') {
    const pinia = createPinia(); setActivePinia(pinia);
    const router = createRouter({ history: createMemoryHistory(), routes: [
        { path: '/organizations/:organizationId/projects', name: 'organizationProjects', component: { template: '<p>Projects</p>' } },
        { path: '/organizations/:organizationId', name: 'organizationOverview', component: { template: '<p>Organization</p>' } },
        { path: '/organizations/:organizationId/projects/:projectId', name: 'projectDetail', component: Members },
        { path: '/organizations/:organizationId/projects/:projectId/members', name: 'projectMembers', component: Members },
        { path: '/organizations/:organizationId/projects/:projectId/settings', name: 'projectSettings', component: Settings },
    ] });
    await router.push(path);
    const wrapper = mount(component, { global: { plugins: [pinia, router] } });
    await flushPromises();
    return { wrapper, router };
}
type Wrapper = Awaited<ReturnType<typeof render>>['wrapper'];
async function click(wrapper: Wrapper, text: string) { const button = wrapper.findAll('button').find(b => b.text() === text); expect(button, text).toBeTruthy(); await button!.trigger('click'); await flushPromises(); }

describe('project management', () => {
    beforeEach(() => {
        vi.resetAllMocks();
        vi.mocked(organizationsApi.get).mockResolvedValue({ data: org });
        vi.mocked(projectsApi.get).mockResolvedValue({ data: { ...project } });
        vi.mocked(projectsApi.listMembers).mockResolvedValue(paged());
        vi.mocked(projectsApi.getSettings).mockResolvedValue({ data: { ...settings } });
        vi.mocked(organizationsApi.listMembers).mockResolvedValue({ ...paged(), data: [{ ...member, role: 'organizationMember' }] });
    });
    it('paginates, searches and filters members on the server; escapes user text', async () => {
        vi.mocked(projectsApi.listMembers).mockResolvedValue(paged([member], 125));
        const { wrapper } = await render(Members);
        expect(wrapper.text()).toContain(member.displayName); expect(wrapper.find('script').exists()).toBe(false);
        await click(wrapper, 'Next');
        expect(projectsApi.listMembers).toHaveBeenLastCalledWith('p', expect.objectContaining({ page: 2 }));
        await wrapper.get('input[type=search]').setValue('Member'); await wrapper.get('form').trigger('submit'); await flushPromises();
        expect(projectsApi.listMembers).toHaveBeenLastCalledWith('p', expect.objectContaining({ page: 1, search: 'Member' }));
        await wrapper.findAll('select')[0]!.setValue('reviewer'); await wrapper.findAll('select')[1]!.setValue('removed'); await flushPromises();
        expect(projectsApi.listMembers).toHaveBeenLastCalledWith('p', expect.objectContaining({ role: 'reviewer', status: 'removed' })); wrapper.unmount();
    });
    it.each(['developer', 'reviewer', 'viewer', 'organizationAdmin'])('hides administrative controls for %s', async currentUserRole => {
        vi.mocked(projectsApi.get).mockResolvedValue({ data: { ...project, currentUserRole } });
        const { wrapper } = await render(Members); expect(wrapper.text()).not.toContain('Add member'); expect(wrapper.text()).not.toContain('Change role'); wrapper.unmount();
    });
    it('maintainer cannot manage or grant owner', async () => {
        vi.mocked(projectsApi.get).mockResolvedValue({ data: { ...project, currentUserRole: 'maintainer' } });
        vi.mocked(projectsApi.listMembers).mockResolvedValue(paged([{ ...member, role: 'projectOwner' }]));
        const { wrapper } = await render(Members); expect(wrapper.text()).not.toContain('Change role'); expect(wrapper.text()).not.toContain('Remove member'); wrapper.unmount();
    });
    it('confirms role updates with membership version, independently of project version', async () => {
        vi.mocked(projectsApi.updateMember).mockResolvedValue({ data: { ...member, role: 'reviewer', version: 5 } });
        const { wrapper } = await render(Members); await click(wrapper, 'Change role');
        await wrapper.get('section[aria-label="Confirm role change"] select').setValue('reviewer');
        expect(projectsApi.updateMember).not.toHaveBeenCalled(); await wrapper.get('section[aria-label="Confirm role change"] form').trigger('submit'); await flushPromises();
        expect(projectsApi.updateMember).toHaveBeenCalledWith('p', 'u', { role: 'reviewer', version: 4 }); wrapper.unmount();
    });
    it('reports authoritative last-owner protection', async () => {
        vi.mocked(projectsApi.removeMember).mockRejectedValue(failure('LAST_OWNER_REQUIRED'));
        const { wrapper } = await render(Members); await click(wrapper, 'Remove member'); await click(wrapper, 'Confirm removal');
        expect(wrapper.get('[role=alert]').text()).toContain('active owner'); wrapper.unmount();
    });
    it('adds a selected eligible organization member and prevents duplicate submission', async () => {
        vi.mocked(projectsApi.listMembers).mockResolvedValue(paged([]));
        let finish!: (value: { data: ProjectMember }) => void;
        vi.mocked(projectsApi.addMember).mockImplementation(() => new Promise(resolve => { finish = resolve; }));
        const { wrapper } = await render(Members); await click(wrapper, 'Add member'); await click(wrapper, `Select ${member.displayName}`);
        // Candidate search and review are separate forms; submit the role review.
        const review = wrapper.findAll('form').find(f => f.text().includes('Confirm add member'))!;
        await review.trigger('submit'); await review.trigger('submit');
        expect(projectsApi.addMember).toHaveBeenCalledTimes(1); expect(projectsApi.addMember).toHaveBeenCalledWith('p', { userId: 'u', role: 'developer' }, expect.any(String));
        finish({ data: member }); await flushPromises(); expect(wrapper.text()).toContain('Project member added'); wrapper.unmount();
    });
    it('prevents selection of an existing active member', async () => {
        const { wrapper } = await render(Members); await click(wrapper, 'Add member'); await click(wrapper, `Select ${member.displayName}`);
        expect(wrapper.text()).toContain('already an active project member'); expect(projectsApi.addMember).not.toHaveBeenCalled(); wrapper.unmount();
    });
    it('preserves general edits on conflict and requires deliberate resubmission', async () => {
        vi.mocked(projectsApi.update).mockRejectedValue(failure('CONCURRENCY_CONFLICT'));
        const { wrapper } = await render(Settings); await wrapper.get('#general input').setValue('My edited name');
        vi.mocked(projectsApi.get).mockResolvedValue({ data: { ...project, name: 'Other edit', version: 8 } });
        await wrapper.get('#general form').trigger('submit'); await flushPromises();
        expect((wrapper.get('#general input').element as HTMLInputElement).value).toBe('My edited name');
        expect(wrapper.get('[role=alert]').text()).toContain('submit again deliberately'); expect(projectsApi.update).toHaveBeenCalledTimes(1);
        await wrapper.get('#general form').trigger('submit'); await flushPromises(); expect(projectsApi.update).toHaveBeenLastCalledWith('p', expect.objectContaining({ version: 8 })); wrapper.unmount();
    });
    it('allows long descriptions and sends separate settings version', async () => {
        vi.mocked(projectsApi.updateSettings).mockResolvedValue({ data: { ...settings, version: 3 } });
        const { wrapper } = await render(Settings); expect(wrapper.get('textarea').attributes('maxlength')).toBeUndefined();
        await wrapper.get('#analysis input[type=checkbox]').setValue(true); await wrapper.get('#analysis form').trigger('submit'); await flushPromises();
        expect(projectsApi.updateSettings).toHaveBeenCalledWith('p', { analysisProfileKey: 'current-default-v1', aiAdvisoryEnabled: true, defaultAssetVisibility: 'private', notificationPolicyVersion: 1, version: 2 });
        expect(wrapper.get('#analysis select').text()).not.toContain('public'); wrapper.unmount();
    });
    it.each(['archive', 'restore'])('confirms %s with project aggregate version', async action => {
        if (action === 'restore') vi.mocked(projectsApi.get).mockResolvedValue({ data: { ...project, status: 'archived' } });
        vi.mocked(projectsApi[action as 'archive' | 'restore']).mockResolvedValue({ data: project });
        const { wrapper } = await render(Settings); await click(wrapper, action === 'archive' ? 'Archive project' : 'Restore project');
        expect(projectsApi[action as 'archive' | 'restore']).not.toHaveBeenCalled(); await click(wrapper, `Confirm ${action}`);
        expect(projectsApi[action as 'archive' | 'restore']).toHaveBeenCalledWith('p', 7); wrapper.unmount();
    });
    it('requires exact slug and presents retained deleting state truthfully', async () => {
        vi.mocked(projectsApi.requestDeletion).mockResolvedValue({ data: { resourceId: 'p', status: 'deleting', version: 8 } });
        const { wrapper } = await render(Settings);
        await wrapper.get('#danger input').setValue('wrong'); expect(wrapper.get('#danger button').attributes('disabled')).toBeDefined();
        await wrapper.get('#danger input').setValue('game'); await click(wrapper, 'Request project deletion');
        vi.mocked(projectsApi.get).mockResolvedValue({ data: { ...project, status: 'deleting', version: 8 } });
        await click(wrapper, 'Confirm delete'); expect(projectsApi.requestDeletion).toHaveBeenCalledWith('p', { version: 7, confirmationSlug: 'game' });
        expect(wrapper.text()).toContain('Status: deleting'); expect(wrapper.text()).not.toContain('Project permanently deleted'); wrapper.unmount();
    });
    it('paginates ownership candidates and confirms retained role', async () => {
        vi.mocked(projectsApi.listMembers).mockResolvedValue(paged([member], 121));
        vi.mocked(projectsApi.transferOwnership).mockResolvedValue({ data: { previousOwner: { ...member, role: 'maintainer' }, newOwner: { ...member, role: 'projectOwner' } } });
        const { wrapper } = await render(Settings); await click(wrapper, 'Next'); expect(projectsApi.listMembers).toHaveBeenLastCalledWith('p', expect.objectContaining({ page: 2, status: 'active' }));
        await click(wrapper, `Select ${member.displayName}`); await click(wrapper, 'Review ownership transfer'); await click(wrapper, 'Confirm ownership');
        expect(projectsApi.transferOwnership).toHaveBeenCalledWith('p', { newOwnerUserId: 'u', retainCurrentOwnerAs: 'maintainer', version: 7 }); wrapper.unmount();
    });
    it('rejects tenant mismatch before loading members or settings', async () => {
        const { wrapper } = await render(Settings, '/organizations/b/projects/p'); expect(wrapper.text()).not.toContain('Real description'); expect(projectsApi.getSettings).not.toHaveBeenCalled(); expect(organizationsApi.get).not.toHaveBeenCalled(); wrapper.unmount();
    });
    it('does not publish late mutation feedback after switching projects', async () => {
        let reject!: (error: Error) => void;
        vi.mocked(projectsApi.update).mockImplementation(() => new Promise((_, fail) => { reject = fail; }));
        const { wrapper, router } = await render(Settings); await wrapper.get('#general form').trigger('submit');
        vi.mocked(projectsApi.get).mockResolvedValue({ data: { ...project, id: 'q', name: 'Second' } });
        await router.push('/organizations/a/projects/q/settings'); await flushPromises(); reject(failure('CONCURRENCY_CONFLICT')); await flushPromises();
        expect(wrapper.text()).not.toContain('submit again deliberately'); expect(wrapper.text()).toContain('Second'); wrapper.unmount();
    });
    it('shows empty and retry states without treating errors as an empty directory', async () => {
        vi.mocked(projectsApi.listMembers).mockRejectedValueOnce(new Error('offline')).mockResolvedValue(paged([]));
        const { wrapper } = await render(Members); expect(wrapper.find('[role=alert]').exists()).toBe(true);
        await wrapper.get('[role=alert] button').trigger('click'); await flushPromises(); expect(wrapper.text()).toContain('No project members match'); wrapper.unmount();
    });
    it('self-leave safely returns to organization projects after access is revoked', async () => {
        const { wrapper, router } = await render(Members);
        useAuthStore().user = { id: member.userId, email: member.email, displayName: member.displayName, status: 'active', emailVerifiedAt: '2026-10-01', createdAt: '2026-10-01', version: 1 };
        await flushPromises(); await click(wrapper, 'Leave project');
        vi.mocked(projectsApi.get).mockRejectedValue(new Error('revoked'));
        await click(wrapper, 'Confirm removal'); expect(projectsApi.removeMember).toHaveBeenCalledWith('p', 'u'); expect(router.currentRoute.value.name).toBe('organizationProjects'); wrapper.unmount();
    });
    it('ignores a mutation completed after unmount', async () => {
        let finish!: (value: { data: Project }) => void;
        vi.mocked(projectsApi.update).mockImplementation(() => new Promise(resolve => { finish = resolve; }));
        const { wrapper } = await render(Settings); await wrapper.get('#general form').trigger('submit');
        wrapper.unmount(); const reads = vi.mocked(projectsApi.get).mock.calls.length;
        finish({ data: project }); await flushPromises(); expect(projectsApi.get).toHaveBeenCalledTimes(reads);
    });
    it('supports organization candidates beyond the hundredth entry', async () => {
        vi.mocked(organizationsApi.listMembers).mockResolvedValue({ ...paged([], 121), data: [{ ...member, role: 'organizationMember' }] });
        const { wrapper } = await render(Members); await click(wrapper, 'Add member');
        for (let index = 0; index < 5; index++) await click(wrapper, 'Next');
        expect(organizationsApi.listMembers).toHaveBeenLastCalledWith('a', expect.objectContaining({ page: 6, status: 'active', pageSize: 20 })); wrapper.unmount();
    });
    it('excludes inactive accounts from ownership selection', async () => {
        vi.mocked(projectsApi.listMembers).mockResolvedValue(paged([{ ...member, accountStatus: 'disabled' }]));
        const { wrapper } = await render(Settings); expect(wrapper.text()).toContain('No eligible candidates on this page.'); expect(wrapper.text()).not.toContain(`Select ${member.displayName}`); wrapper.unmount();
    });
    it('does not reload settings after a save completes on an unmounted view', async () => {
        let finish!: (value: { data: ProjectSettings }) => void;
        vi.mocked(projectsApi.updateSettings).mockImplementation(() => new Promise(resolve => { finish = resolve; }));
        const { wrapper } = await render(Settings); await wrapper.get('#analysis form').trigger('submit');
        wrapper.unmount(); const reads = vi.mocked(projectsApi.getSettings).mock.calls.length;
        finish({ data: settings }); await flushPromises(); expect(projectsApi.getSettings).toHaveBeenCalledTimes(reads);
    });
});
