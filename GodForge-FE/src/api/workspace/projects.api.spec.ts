import { beforeEach, describe, expect, it, vi } from 'vitest';
import baseApi from '../baseApi';
import { projectsApi } from './projects.api';
vi.mock('../baseApi', () => ({ default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), put: vi.fn(), delete: vi.fn() } }));
describe('project API executable contracts', () => {
    beforeEach(() => vi.resetAllMocks());
    it('serializes member pagination and optional filters', async () => {
        await projectsApi.listMembers('a/b', { page: 6, search: 'some@example.test', role: 'viewer', status: 'active' });
        expect(baseApi.get).toHaveBeenCalledWith('/projects/a%2Fb/members', { params: { page: 6, pageSize: 20, search: 'some@example.test', role: 'viewer', status: 'active' } });
    });
    it('serializes member create idempotency and member-specific version', async () => {
        await projectsApi.addMember('p', { userId: 'u', role: 'developer' }, 'key');
        expect(baseApi.post).toHaveBeenCalledWith('/projects/p/members', { userId: 'u', role: 'developer' }, { headers: { 'Idempotency-Key': 'key' } });
        await projectsApi.updateMember('p', 'u', { role: 'reviewer', version: 4 });
        expect(baseApi.patch).toHaveBeenCalledWith('/projects/p/members/u', { role: 'reviewer', version: 4 });
    });
    it('sends DELETE confirmation in the request body and preserves the accepted envelope', async () => {
        const result = { data: { resourceId: 'p', status: 'deleting', version: 8 } };
        vi.mocked(baseApi.delete).mockResolvedValue(result);
        expect(await projectsApi.requestDeletion('p', { version: 7, confirmationSlug: 'game' })).toEqual(result);
        expect(baseApi.delete).toHaveBeenCalledWith('/projects/p', { data: { version: 7, confirmationSlug: 'game' } });
    });
    it('preserves independent settings version and typed fields', async () => {
        const input = { analysisProfileKey: 'current-default-v1', aiAdvisoryEnabled: true, defaultAssetVisibility: 'internal' as const, notificationPolicyVersion: 1, version: 2 };
        await projectsApi.updateSettings('p', input); expect(baseApi.put).toHaveBeenCalledWith('/projects/p/settings', input);
    });
    it.each(['archive', 'restore'] as const)('serializes %s aggregate version', async action => {
        await projectsApi[action]('p', 3); expect(baseApi.post).toHaveBeenCalledWith(`/projects/p/${action}`, { version: 3 });
    });
    it('serializes ownership target and retained role', async () => {
        const input = { newOwnerUserId: 'u', retainCurrentOwnerAs: 'reviewer' as const, version: 7 };
        await projectsApi.transferOwnership('p', input); expect(baseApi.post).toHaveBeenCalledWith('/projects/p/transfer-ownership', input);
    });
});
