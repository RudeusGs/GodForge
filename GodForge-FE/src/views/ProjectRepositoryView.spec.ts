import { flushPromises, mount } from '@vue/test-utils';
import { createMemoryHistory, createRouter } from 'vue-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError } from 'axios';
import { repositoryApi } from '@/api/repository/repository.api';
import { projectsApi } from '@/api/workspace/projects.api';
import { organizationsApi } from '@/api/workspace/organizations.api';
import Repository from './ProjectRepositoryView.vue';
vi.mock('@/api/repository/repository.api');
vi.mock('@/api/workspace/projects.api');
vi.mock('@/api/workspace/organizations.api');
const project = { id: 'p', organizationId: 'o', name: 'Godot game', slug: 'godot-game', description: null, visibility: 'private', currentUserRole: 'projectOwner', status: 'active', createdAt: '', updatedAt: '', version: 1 };
const repo = { id: 'r', projectId: 'p', mode: 'InternalHosted' as const, provider: 'Forgejo', cloneUrl: 'https://git.example/service/game.git', defaultBranch: 'main', status: 'Ready' as const, autoAnalyzeEnabled: false };
const failure = (code: string) => new AxiosError('request failed', undefined, undefined, undefined, { status: 400, data: { error: { code } }, config: {} as never, headers: {}, statusText: '' });
async function render() {
    const router = createRouter({ history: createMemoryHistory(), routes: [
        { path: '/organizations/:organizationId/projects/:projectId/repository', name: 'projectRepository', component: Repository },
        { path: '/organizations/:organizationId/projects/:projectId', name: 'projectDetail', component: Repository },
        { path: '/organizations/:organizationId/projects/:projectId/members', name: 'projectMembers', component: Repository },
        { path: '/organizations/:organizationId/projects/:projectId/settings', name: 'projectSettings', component: Repository }
    ] });
    await router.push('/organizations/o/projects/p/repository');
    const wrapper = mount(Repository, { global: { plugins: [router] } });
    await flushPromises(); return { wrapper, router };
}
describe('repository workspace', () => {
    beforeEach(() => {
        vi.resetAllMocks();
        vi.mocked(projectsApi.get).mockResolvedValue({ data: { ...project } });
        vi.mocked(organizationsApi.get).mockResolvedValue({ data: { id: 'o', name: 'Studio', slug: 'studio', currentUserRole: 'organizationMember', status: 'active', version: 1, createdAt: '', updatedAt: '' } });
        vi.mocked(repositoryApi.get).mockResolvedValue({ data: { ...repo } });
        vi.mocked(repositoryApi.branches).mockResolvedValue({ data: [{ name: 'main', commitSha: 'a'.repeat(40), isDefault: true, isProtected: false }] });
        vi.mocked(repositoryApi.commits).mockResolvedValue({ data: [{ sha: 'a'.repeat(40), shortSha: 'aaaaaaaa', message: 'Godot fixture', author: 'Dev', timestamp: '2026-10-31T00:00:00Z', parents: [] }] });
        vi.mocked(repositoryApi.tree).mockResolvedValue({ data: { commitSha: 'a'.repeat(40), entries: [{ name: 'project.godot', path: 'project.godot', type: 'blob', size: 10, sha: 'b'.repeat(40) }] } });
    });
    it('creates a private repository and shows real pending state', async () => {
        vi.mocked(repositoryApi.get).mockRejectedValueOnce(failure('REPOSITORY_NOT_CONNECTED'));
        vi.mocked(repositoryApi.provision).mockResolvedValue({ data: { jobId: 'job' } });
        const { wrapper } = await render();
        vi.mocked(repositoryApi.get).mockResolvedValue({ data: { ...repo, status: 'ProvisioningPending', cloneUrl: '' } });
        await wrapper.findAll('form')[0]!.trigger('submit'); await flushPromises();
        expect(repositoryApi.provision).toHaveBeenCalledWith('p', { name: 'godot-game', defaultBranch: 'main', isPrivate: true, autoAnalyzeEnabled: false });
        expect(wrapper.text()).toContain('ProvisioningPending'); expect(wrapper.text()).not.toContain('Copy clone URL'); wrapper.unmount();
    });
    it('hides provisioning controls from viewers', async () => {
        vi.mocked(projectsApi.get).mockResolvedValue({ data: { ...project, currentUserRole: 'viewer' } });
        vi.mocked(repositoryApi.get).mockRejectedValue(failure('REPOSITORY_NOT_CONNECTED'));
        const { wrapper } = await render(); expect(wrapper.findAll('form')).toHaveLength(0); wrapper.unmount();
    });
    it('renders source as text and uses immutable revision identity', async () => {
        vi.mocked(repositoryApi.blob).mockResolvedValue({ data: { commitSha: 'a'.repeat(40), path: 'project.godot', content: '<script>window.attack=true</script>', size: 33 } });
        const { wrapper } = await render();
        await wrapper.findAll('button').find(b => b.text().includes('project.godot'))!.trigger('click'); await flushPromises();
        expect(repositoryApi.blob).toHaveBeenCalledWith('p', 'a'.repeat(40), 'project.godot');
        expect(wrapper.find('pre script').exists()).toBe(false); expect(wrapper.get('pre').text()).toContain('<script>'); wrapper.unmount();
    });
    it('provides a binary fallback', async () => {
        vi.mocked(repositoryApi.blob).mockRejectedValue(failure('REPOSITORY_BINARY_FILE'));
        const { wrapper } = await render(); await wrapper.findAll('button').find(b => b.text().includes('project.godot'))!.trigger('click'); await flushPromises();
        expect(wrapper.text()).toContain('Binary files cannot be displayed'); wrapper.unmount();
    });
    it('discards a late blob response after tenant switching', async () => {
        let finish!: (value: Awaited<ReturnType<typeof repositoryApi.blob>>) => void;
        vi.mocked(repositoryApi.blob).mockReturnValue(new Promise(resolve => { finish = resolve; }));
        const { wrapper, router } = await render(); await wrapper.findAll('button').find(b => b.text().includes('project.godot'))!.trigger('click');
        vi.mocked(projectsApi.get).mockResolvedValue({ data: { ...project, id: 'q', organizationId: 'other' } });
        await router.push('/organizations/other/projects/q/repository'); await flushPromises();
        finish({ data: { commitSha: 'a'.repeat(40), path: 'project.godot', content: 'SENSITIVE OLD TENANT', size: 20 } }); await flushPromises();
        expect(wrapper.text()).not.toContain('SENSITIVE OLD TENANT'); wrapper.unmount();
    });
    it('browses a linked repository and exposes explicit synchronization without hosted protection', async () => {
        vi.mocked(repositoryApi.get).mockResolvedValue({ data: { ...repo, mode: 'ExternalLinked', provider: 'Generic' } });
        vi.mocked(repositoryApi.synchronize).mockResolvedValue({ data: { jobId: 'sync' } });
        const { wrapper } = await render();
        expect(wrapper.text()).toContain('Linked Git');
        expect(wrapper.text()).toContain('project.godot');
        expect(wrapper.text()).not.toContain('Open in Forgejo');
        await wrapper.findAll('button').find(b => b.text() === 'Synchronize repository')!.trigger('click'); await flushPromises();
        expect(repositoryApi.synchronize).toHaveBeenCalledWith('p'); wrapper.unmount();
    });
    it('polls the durable operation before showing confirmed provider success', async () => {
        vi.useFakeTimers();
        try {
            vi.mocked(repositoryApi.get).mockRejectedValueOnce(failure('REPOSITORY_NOT_CONNECTED'));
            vi.mocked(repositoryApi.provision).mockResolvedValue({ data: { jobId: 'provision' } });
            const { wrapper } = await render();
            vi.mocked(repositoryApi.get).mockResolvedValue({ data: { ...repo, status: 'ProvisioningPending', cloneUrl: '' } });
            await wrapper.findAll('form')[0]!.trigger('submit'); await flushPromises();
            expect(wrapper.text()).not.toContain('Copy clone URL');
            vi.mocked(repositoryApi.operation).mockResolvedValue({ data: { status: 'Completed' } });
            vi.mocked(repositoryApi.get).mockResolvedValue({ data: { ...repo } });
            await vi.advanceTimersByTimeAsync(2000); await flushPromises();
            expect(repositoryApi.operation).toHaveBeenCalledWith('p', 'provision');
            expect(wrapper.text()).toContain('Copy clone URL'); wrapper.unmount();
        } finally { vi.useRealTimers(); }
    });
});
