import { beforeEach, describe, expect, it, vi } from 'vitest';
import baseApi from '../baseApi';
import { organizationsApi } from './organizations.api';
import { projectsApi } from './projects.api';

vi.mock('../baseApi', () => ({
    default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() },
}));

describe('workspace transport contracts', () => {
    beforeEach(() => vi.clearAllMocks());

    it('preserves envelopes and pagination through baseApi', async () => {
        const envelope = { data: [], meta: { page: 2, pageSize: 20, totalCount: 21 } };
        vi.mocked(baseApi.get).mockResolvedValue(envelope);
        expect(await organizationsApi.list(2)).toBe(envelope);
        expect(baseApi.get).toHaveBeenCalledWith('/organizations', { params: { page: 2, pageSize: 20 } });
        await projectsApi.list('tenant', 3);
        expect(baseApi.get).toHaveBeenLastCalledWith('/projects', {
            params: { organizationId: 'tenant', page: 3, pageSize: 20 },
        });
        await projectsApi.catalog('tenant', 2);
        expect(baseApi.get).toHaveBeenLastCalledWith('/organizations/tenant/projects', {
            params: { page: 2, pageSize: 20 },
        });
    });

    it('encodes route IDs and keeps project and organization reads separate', async () => {
        await organizationsApi.get('a/b');
        expect(baseApi.get).toHaveBeenLastCalledWith('/organizations/a%2Fb');
        await projectsApi.get('a/b');
        expect(baseApi.get).toHaveBeenLastCalledWith('/projects/a%2Fb');
    });

    it('sends allowed create fields and explicit idempotency keys without inventing a version', async () => {
        const input = { name: 'Studio', slug: 'studio' };
        await organizationsApi.create(input, 'key');
        expect(baseApi.post).toHaveBeenLastCalledWith('/organizations', input, {
            headers: { 'Idempotency-Key': 'key' },
        });
        const project = { ...input, description: null, visibility: 'private' as const };
        await projectsApi.create('tenant', project, 'project-key');
        expect(baseApi.post).toHaveBeenLastCalledWith('/organizations/tenant/projects', project, {
            headers: { 'Idempotency-Key': 'project-key' },
        });
    });

    it('supports organization settings, ownership transfer, members, and invitations endpoints', async () => {
        await organizationsApi.update('org-1', { name: 'New Name', slug: 'new-slug', version: 2 });
        expect(baseApi.patch).toHaveBeenLastCalledWith('/organizations/org-1', {
            name: 'New Name',
            slug: 'new-slug',
            version: 2,
        });

        await organizationsApi.requestDeletion('org-1', {
            version: 3,
            confirmationSlug: 'new-slug',
        });
        expect(baseApi.delete).toHaveBeenLastCalledWith('/organizations/org-1', {
            data: { version: 3, confirmationSlug: 'new-slug' },
        });

        await organizationsApi.transferOwnership('org-1', {
            newOwnerUserId: 'user-2',
            retainCurrentOwnerAs: 'organizationAdmin',
            version: 3,
        });
        expect(baseApi.post).toHaveBeenLastCalledWith('/organizations/org-1/transfer-ownership', {
            newOwnerUserId: 'user-2',
            retainCurrentOwnerAs: 'organizationAdmin',
            version: 3,
        });

        await organizationsApi.listMembers('org-1', {
            page: 2,
            role: 'organizationAdmin',
            status: 'active',
            search: '  alice  ',
        });
        expect(baseApi.get).toHaveBeenLastCalledWith('/organizations/org-1/members', {
            params: {
                page: 2,
                pageSize: 20,
                role: 'organizationAdmin',
                status: 'active',
                search: 'alice',
            },
        });

        await organizationsApi.updateMember('org-1', 'user-2', {
            role: 'organizationAdmin',
            status: 'active',
            version: 1,
        });
        expect(baseApi.patch).toHaveBeenLastCalledWith('/organizations/org-1/members/user-2', {
            role: 'organizationAdmin',
            status: 'active',
            version: 1,
        });

        await organizationsApi.removeMember('org-1', 'user-2');
        expect(baseApi.delete).toHaveBeenLastCalledWith('/organizations/org-1/members/user-2');

        await organizationsApi.listInvitations('org-1', {
            page: 1,
            status: 'pending',
            email: '  bob@example.com ',
        });
        expect(baseApi.get).toHaveBeenLastCalledWith('/organizations/org-1/invitations', {
            params: { page: 1, pageSize: 20, status: 'pending', email: 'bob@example.com' },
        });

        await organizationsApi.createInvitation(
            'org-1',
            { email: 'bob@example.com', role: 'organizationMember' },
            'inv-key-1'
        );
        expect(baseApi.post).toHaveBeenLastCalledWith(
            '/organizations/org-1/invitations',
            { email: 'bob@example.com', role: 'organizationMember' },
            { headers: { 'Idempotency-Key': 'inv-key-1' } }
        );

        await organizationsApi.revokeInvitation('org-1', 'inv-1');
        expect(baseApi.delete).toHaveBeenLastCalledWith('/organizations/org-1/invitations/inv-1');

        await organizationsApi.acceptInvitation({ token: 'secret-token' });
        expect(baseApi.post).toHaveBeenLastCalledWith('/organization-invitations/accept', {
            token: 'secret-token',
        });
    });

    it('propagates transport failures to the caller', async () => {
        vi.mocked(baseApi.get).mockRejectedValueOnce(new Error('offline'));
        await expect(projectsApi.get('id')).rejects.toThrow('offline');
    });
});
