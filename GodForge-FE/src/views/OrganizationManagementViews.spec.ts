import { flushPromises, mount } from '@vue/test-utils';
import { AxiosError } from 'axios';
import { createPinia, setActivePinia } from 'pinia';
import { createMemoryHistory, createRouter } from 'vue-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { organizationsApi } from '@/api/workspace/organizations.api';
import { clearPendingInvitationToken } from '@/features/workspace/workspaceState';
import { useAuthStore } from '@/stores/auth.store';
import DashboardView from './DashboardView.vue';
import OrganizationInvitationAcceptView from './OrganizationInvitationAcceptView.vue';
import OrganizationInvitationsView from './OrganizationInvitationsView.vue';
import OrganizationMembersView from './OrganizationMembersView.vue';
import OrganizationOverviewView from './OrganizationOverviewView.vue';
import OrganizationProjectsView from './OrganizationProjectsView.vue';
import OrganizationSettingsView from './OrganizationSettingsView.vue';

vi.mock('@/api/workspace/organizations.api', () => ({
    organizationsApi: {
        list: vi.fn(),
        get: vi.fn(),
        create: vi.fn(),
        update: vi.fn(),
        requestDeletion: vi.fn(),
        transferOwnership: vi.fn(),
        listMembers: vi.fn(),
        updateMember: vi.fn(),
        removeMember: vi.fn(),
        listInvitations: vi.fn(),
        createInvitation: vi.fn(),
        revokeInvitation: vi.fn(),
        acceptInvitation: vi.fn(),
    },
}));

const apiError = (status: number, code?: string) =>
    new AxiosError('error', undefined, undefined, undefined, {
        status,
        data: code ? { error: { code } } : {},
        headers: {},
        statusText: '',
        config: {} as never,
    });

const ownerOrg = {
    id: 'org-1',
    name: 'Godot Studio',
    slug: 'godot-studio',
    currentUserRole: 'organizationOwner',
    status: 'active',
    createdAt: '2026-10-01T00:00:00Z',
    updatedAt: '2026-10-02T00:00:00Z',
    version: 1,
};

const membersList = [
    {
        userId: 'user-owner',
        email: 'owner@example.com',
        displayName: 'Alice Owner',
        role: 'organizationOwner',
        status: 'active',
        joinedAt: '2026-10-01T00:00:00Z',
        version: 1,
    },
    {
        userId: 'user-admin',
        email: 'admin@example.com',
        displayName: 'Bob Admin',
        role: 'organizationAdmin',
        status: 'active',
        joinedAt: '2026-10-02T00:00:00Z',
        version: 1,
    },
    {
        userId: 'user-member',
        email: 'member@example.com',
        displayName: 'Charlie Member',
        role: 'organizationMember',
        status: 'active',
        joinedAt: '2026-10-03T00:00:00Z',
        version: 2,
    },
];

const invitationsList = [
    {
        id: 'inv-1',
        email: 'pending@example.com',
        role: 'organizationMember',
        status: 'pending',
        expiresAt: '2026-10-10T00:00:00Z',
        invitedByUserId: 'user-owner',
        createdAt: '2026-10-03T00:00:00Z',
        version: 1,
    },
];

async function renderView(
    component: unknown,
    path: string,
    currentUser = { id: 'user-owner', email: 'owner@example.com', displayName: 'Alice Owner' }
) {
    const pinia = createPinia();
    setActivePinia(pinia);
    const auth = useAuthStore();
    auth.user = {
        ...currentUser,
        status: 'active',
        emailVerifiedAt: '2026-10-01T00:00:00Z',
        createdAt: '2026-10-01T00:00:00Z',
        version: 1,
    };

    const router = createRouter({
        history: createMemoryHistory(),
        routes: [
            { path: '/dashboard', name: 'dashboard', component: DashboardView },
            { path: '/organizations/:organizationId', name: 'organizationOverview', component: OrganizationOverviewView },
            { path: '/organizations/:organizationId/projects', name: 'organizationProjects', component: OrganizationProjectsView },
            { path: '/organizations/:organizationId/members', name: 'organizationMembers', component: OrganizationMembersView },
            { path: '/organizations/:organizationId/invitations', name: 'organizationInvitations', component: OrganizationInvitationsView },
            { path: '/organizations/:organizationId/settings', name: 'organizationSettings', component: OrganizationSettingsView },
            { path: '/organization-invitations/accept', name: 'organizationInvitationAccept', component: OrganizationInvitationAcceptView },
        ],
    });
    await router.push(path);
    const wrapper = mount(component as never, { global: { plugins: [pinia, router] } });
    await flushPromises();
    return { wrapper, router };
}

describe('organization management views', () => {
    beforeEach(() => {
        vi.resetAllMocks();
        clearPendingInvitationToken();
        vi.mocked(organizationsApi.get).mockResolvedValue({ data: ownerOrg });
        vi.mocked(organizationsApi.listMembers).mockResolvedValue({
            data: membersList,
            meta: { page: 1, pageSize: 20, totalCount: membersList.length },
        });
        vi.mocked(organizationsApi.listInvitations).mockResolvedValue({
            data: invitationsList,
            meta: { page: 1, pageSize: 20, totalCount: invitationsList.length },
        });
    });

    it('renders organization overview and displays deleting banner when status is deleting', async () => {
        vi.mocked(organizationsApi.get).mockResolvedValueOnce({
            data: { ...ownerOrg, status: 'deleting', version: 4 },
        });
        const { wrapper } = await renderView(OrganizationOverviewView, '/organizations/org-1');

        expect(wrapper.text()).toContain('Godot Studio');
        expect(wrapper.text()).toContain('Deletion requested');
        expect(wrapper.text()).toContain('deleting');
        wrapper.unmount();
    });

    it('supports member search, filters, role update with confirmation, and member removal', async () => {
        vi.mocked(organizationsApi.updateMember).mockResolvedValue({
            data: { ...membersList[2]!, role: 'organizationAdmin', version: 3 },
        });
        vi.mocked(organizationsApi.removeMember).mockResolvedValue(undefined);

        const { wrapper } = await renderView(OrganizationMembersView, '/organizations/org-1/members');

        expect(wrapper.text()).toContain('Alice Owner');
        expect(wrapper.text()).toContain('Bob Admin');
        expect(wrapper.text()).toContain('Charlie Member');

        // Filter by role
        await wrapper.get('#member-role-filter').setValue('organizationMember');
        await flushPromises();
        expect(organizationsApi.listMembers).toHaveBeenLastCalledWith('org-1', {
            page: 1,
            pageSize: 20,
            role: 'organizationMember',
            status: undefined,
            search: undefined,
        });

        // Start role edit on Charlie Member
        const manageButtons = wrapper.findAll('button').filter((b) => b.text() === 'Manage role');
        await manageButtons[manageButtons.length - 1]!.trigger('click');
        await wrapper.get('#role-user-member').setValue('organizationAdmin');
        await wrapper.findAll('button').find((b) => b.text() === 'Review change')!.trigger('click');
        expect(wrapper.text()).toContain('Confirm updating Charlie Member');
        await wrapper.findAll('button').find((b) => b.text() === 'Confirm update')!.trigger('click');
        await flushPromises();

        expect(organizationsApi.updateMember).toHaveBeenCalledWith('org-1', 'user-member', {
            role: 'organizationAdmin',
            status: 'active',
            version: 2,
        });
        expect(wrapper.text()).toContain('Updated membership for Charlie Member.');

        // Remove Charlie Member
        const removeButtons = wrapper.findAll('button').filter((b) => b.text() === 'Remove member');
        await removeButtons[removeButtons.length - 1]!.trigger('click');
        await wrapper.findAll('button').find((b) => b.text() === 'Confirm removal')!.trigger('click');
        await flushPromises();

        expect(organizationsApi.removeMember).toHaveBeenCalledWith('org-1', 'user-member');
        expect(wrapper.text()).toContain('Removed Charlie Member from the organization.');
        wrapper.unmount();
    });

    it('restricts administrative controls for OrganizationMember while allowing self-leave redirect', async () => {
        vi.mocked(organizationsApi.get).mockResolvedValue({
            data: { ...ownerOrg, currentUserRole: 'organizationMember' },
        });
        vi.mocked(organizationsApi.removeMember).mockResolvedValue(undefined);

        const { wrapper, router } = await renderView(
            OrganizationMembersView,
            '/organizations/org-1/members',
            { id: 'user-member', email: 'member@example.com', displayName: 'Charlie Member' }
        );

        expect(wrapper.findAll('button').some((b) => b.text() === 'Manage role')).toBe(false);
        expect(wrapper.findAll('button').some((b) => b.text() === 'Remove member')).toBe(false);

        const leaveButton = wrapper.findAll('button').find((b) => b.text() === 'Leave organization');
        expect(leaveButton).toBeTruthy();
        await leaveButton!.trigger('click');
        await wrapper.findAll('button').find((b) => b.text() === 'Confirm leave')!.trigger('click');
        await flushPromises();

        expect(organizationsApi.removeMember).toHaveBeenCalledWith('org-1', 'user-member');
        expect(router.currentRoute.value.name).toBe('dashboard');
        wrapper.unmount();
    });

    it('creates and revokes invitations and hides admin role option for OrganizationAdmin', async () => {
        vi.mocked(organizationsApi.get).mockResolvedValue({
            data: { ...ownerOrg, currentUserRole: 'organizationAdmin' },
        });
        vi.mocked(organizationsApi.createInvitation).mockResolvedValue({
            data: {
                id: 'inv-2',
                email: 'newbie@example.com',
                role: 'organizationMember',
                status: 'pending',
                expiresAt: '2026-10-10T00:00:00Z',
                invitedByUserId: 'user-admin',
                createdAt: '2026-10-03T00:00:00Z',
                version: 1,
            },
        });
        vi.mocked(organizationsApi.revokeInvitation).mockResolvedValue(undefined);

        const { wrapper } = await renderView(
            OrganizationInvitationsView,
            '/organizations/org-1/invitations',
            { id: 'user-admin', email: 'admin@example.com', displayName: 'Bob Admin' }
        );

        // OrganizationAdmin cannot invite organizationAdmin
        const roleOptions = wrapper.get('#invite-role').findAll('option').map((o) => o.element.value);
        expect(roleOptions).toEqual(['organizationMember']);

        // Validation on empty email
        await wrapper.find('form').trigger('submit');
        expect(wrapper.text()).toContain('Enter an email address to invite.');

        // Valid invitation submission
        await wrapper.get('#invite-email').setValue('newbie@example.com');
        await wrapper.find('form').trigger('submit');
        await flushPromises();

        expect(organizationsApi.createInvitation).toHaveBeenCalledWith(
            'org-1',
            { email: 'newbie@example.com', role: 'organizationMember' },
            expect.any(String)
        );
        expect(wrapper.text()).toContain('Invitation sent to newbie@example.com');

        // Revoke pending invitation
        await wrapper.findAll('button').find((b) => b.text() === 'Revoke invitation')!.trigger('click');
        await wrapper.findAll('button').find((b) => b.text() === 'Confirm revoke')!.trigger('click');
        await flushPromises();

        expect(organizationsApi.revokeInvitation).toHaveBeenCalledWith('org-1', 'inv-1');
        expect(wrapper.text()).toContain('Revoked invitation for pending@example.com.');
        wrapper.unmount();
    });

    it('scrubs invitation token from URL, never touches web storage, and handles acceptance and invalid tokens', async () => {
        vi.mocked(organizationsApi.acceptInvitation).mockResolvedValue({
            data: {
                organization: { ...ownerOrg, currentUserRole: 'organizationMember' },
                membership: {
                    userId: 'user-invitee',
                    email: 'invitee@example.com',
                    displayName: 'Invitee',
                    role: 'organizationMember',
                    status: 'active',
                    joinedAt: '2026-10-04T00:00:00Z',
                    version: 1,
                },
            },
        });

        const { wrapper, router } = await renderView(
            OrganizationInvitationAcceptView,
            '/organization-invitations/accept?token=sensitive-invite-token',
            { id: 'user-invitee', email: 'invitee@example.com', displayName: 'Invitee' }
        );

        // URL query is scrubbed immediately
        expect(router.currentRoute.value.query.token).toBeUndefined();
        expect(window.localStorage.getItem('token')).toBeNull();
        expect(window.sessionStorage.getItem('token')).toBeNull();

        await wrapper.findAll('button').find((b) => b.text() === 'Accept invitation')!.trigger('click');
        await flushPromises();

        expect(organizationsApi.acceptInvitation).toHaveBeenCalledWith({
            token: 'sensitive-invite-token',
        });
        expect(wrapper.text()).toContain('Joined Godot Studio');
        expect(wrapper.text()).toContain('organizationMember');
        wrapper.unmount();

        // Invalid or expired token handling
        vi.mocked(organizationsApi.acceptInvitation).mockRejectedValueOnce(
            apiError(401, 'INVITE_INVALID_OR_EXPIRED')
        );
        const expiredRender = await renderView(
            OrganizationInvitationAcceptView,
            '/organization-invitations/accept?token=expired-token',
            { id: 'user-invitee', email: 'invitee@example.com', displayName: 'Invitee' }
        );
        await expiredRender.wrapper
            .findAll('button')
            .find((b) => b.text() === 'Accept invitation')!
            .trigger('click');
        await flushPromises();

        expect(expiredRender.wrapper.get('[role="alert"]').text()).toContain(
            'invalid, expired, revoked'
        );
        expiredRender.wrapper.unmount();
    });

    it('updates general settings, transfers ownership, and requests organization deletion in settings view', async () => {
        vi.mocked(organizationsApi.update).mockResolvedValue({
            data: { ...ownerOrg, name: 'Renamed Studio', slug: 'renamed-studio', version: 2 },
        });
        vi.mocked(organizationsApi.transferOwnership).mockResolvedValue({
            data: {
                previousOwner: { ...membersList[0]!, role: 'organizationAdmin', version: 2 },
                newOwner: { ...membersList[1]!, role: 'organizationOwner', version: 2 },
            },
        });
        vi.mocked(organizationsApi.requestDeletion).mockResolvedValue({
            data: { resourceId: 'org-1', status: 'deleting', version: 3 },
        });

        const { wrapper } = await renderView(OrganizationSettingsView, '/organizations/org-1/settings');

        // 1. Update general settings
        await wrapper.get('#org-settings-name').setValue('Renamed Studio');
        await wrapper.get('#org-settings-slug').setValue('renamed-studio');
        await wrapper.find('form').trigger('submit');
        await flushPromises();

        expect(organizationsApi.update).toHaveBeenCalledWith('org-1', {
            name: 'Renamed Studio',
            slug: 'renamed-studio',
            version: 1,
        });
        expect(wrapper.text()).toContain('Saved organization settings');

        // 2. Ownership transfer
        await wrapper.get('#transfer-target-member').setValue('user-admin');
        await wrapper.get('#transfer-retain-role').setValue('organizationAdmin');
        await wrapper
            .findAll('button')
            .find((b) => b.text() === 'Review ownership transfer')!
            .trigger('click');
        await wrapper
            .findAll('button')
            .find((b) => b.text() === 'Confirm ownership transfer')!
            .trigger('click');
        await flushPromises();

        expect(organizationsApi.transferOwnership).toHaveBeenCalledWith('org-1', {
            newOwnerUserId: 'user-admin',
            retainCurrentOwnerAs: 'organizationAdmin',
            version: 1,
        });
        expect(wrapper.text()).toContain('Transferred organization ownership to Bob Admin');

        // 3. Danger Zone deletion request
        await wrapper.get('#delete-confirmation-slug').setValue('wrong-slug');
        await wrapper
            .findAll('button')
            .find((b) => b.text() === 'Request organization deletion')!
            .trigger('click');
        expect(wrapper.text()).toContain('Enter the exact organization slug "godot-studio"');

        await wrapper.get('#delete-confirmation-slug').setValue('godot-studio');
        await wrapper
            .findAll('button')
            .find((b) => b.text() === 'Request organization deletion')!
            .trigger('click');
        await wrapper
            .findAll('button')
            .find((b) => b.text() === 'Confirm deletion request')!
            .trigger('click');
        await flushPromises();

        expect(organizationsApi.requestDeletion).toHaveBeenCalledWith('org-1', {
            version: 1,
            confirmationSlug: 'godot-studio',
        });
        expect(wrapper.text()).toContain('Lifecycle status is now deleting');
        wrapper.unmount();
    });

    it('Defect A regression: enforces 1..160 name and 1..80 slug boundaries in OrganizationSettingsView', async () => {
        const maxName = 'G'.repeat(160);
        const maxSlug = 's'.repeat(80);
        vi.mocked(organizationsApi.update)
            .mockResolvedValueOnce({
                data: { ...ownerOrg, name: 'A', slug: 'a', version: 2 },
            })
            .mockResolvedValueOnce({
                data: { ...ownerOrg, name: maxName, slug: maxSlug, version: 3 },
            });

        const { wrapper } = await renderView(OrganizationSettingsView, '/organizations/org-1/settings');

        expect(wrapper.get('#org-settings-name').attributes('maxlength')).toBe('160');
        expect(wrapper.get('#org-settings-slug').attributes('maxlength')).toBe('80');

        // Reject whitespace-only name
        await wrapper.get('#org-settings-name').setValue('   ');
        await wrapper.get('#org-settings-slug').setValue('valid-slug');
        await wrapper.find('form[aria-label="Search ownership candidates"]')?.exists();
        await wrapper.get('section[aria-label="General settings"] form').trigger('submit');
        expect(wrapper.text()).toContain('Organization name must be between 1 and 160 characters.');
        expect(organizationsApi.update).not.toHaveBeenCalled();

        // Reject >160 char name
        await wrapper.get('#org-settings-name').setValue('G'.repeat(161));
        await wrapper.get('section[aria-label="General settings"] form').trigger('submit');
        expect(wrapper.text()).toContain('Organization name must be between 1 and 160 characters.');

        // Reject invalid slugs (leading/trailing/double hyphen, >80 chars)
        await wrapper.get('#org-settings-name').setValue('Valid Name');
        for (const badSlug of ['-leading', 'trailing-', 'double--hyphen', 'Upper', 's'.repeat(81)]) {
            await wrapper.get('#org-settings-slug').setValue(badSlug);
            await wrapper.get('section[aria-label="General settings"] form').trigger('submit');
            expect(wrapper.text()).toContain('Slug must be 1–80 characters');
        }
        expect(organizationsApi.update).not.toHaveBeenCalled();

        // Accept minimum valid boundary: 1-char name 'A' and 1-char slug 'a'
        await wrapper.get('#org-settings-name').setValue('A');
        await wrapper.get('#org-settings-slug').setValue('a');
        await wrapper.get('section[aria-label="General settings"] form').trigger('submit');
        await flushPromises();
        expect(organizationsApi.update).toHaveBeenLastCalledWith('org-1', {
            name: 'A',
            slug: 'a',
            version: 1,
        });

        // Accept maximum valid boundary: 160-char name and 80-char slug
        await wrapper.get('#org-settings-name').setValue(maxName);
        await wrapper.get('#org-settings-slug').setValue(maxSlug);
        await wrapper.get('section[aria-label="General settings"] form').trigger('submit');
        await flushPromises();
        expect(organizationsApi.update).toHaveBeenLastCalledWith('org-1', {
            name: maxName,
            slug: maxSlug,
            version: 1,
        });
        wrapper.unmount();
    });

    it('Defect B regression: does not block owner removal/leave from single-page subset and surfaces backend LAST_OWNER_REQUIRED', async () => {
        // Page 1 only shows 1 owner (Alice Owner) even though totalCount is 25 and a second owner is on Page 2
        vi.mocked(organizationsApi.listMembers).mockResolvedValue({
            data: [membersList[0]!, membersList[1]!, membersList[2]!],
            meta: { page: 1, pageSize: 20, totalCount: 25 },
        });

        const { wrapper, router } = await renderView(OrganizationMembersView, '/organizations/org-1/members');

        // Alice Owner still has the 'Leave organization' button enabled despite being the only owner on Page 1
        const leaveBtn = wrapper.findAll('button').find((b) => b.text() === 'Leave organization');
        expect(leaveBtn).toBeTruthy();

        // Case 1: Backend rejects with 409 LAST_OWNER_REQUIRED when Alice is actually the sole owner
        vi.mocked(organizationsApi.removeMember).mockRejectedValueOnce(
            apiError(409, 'LAST_OWNER_REQUIRED')
        );
        await leaveBtn!.trigger('click');
        await wrapper.findAll('button').find((b) => b.text() === 'Confirm leave')!.trigger('click');
        await flushPromises();

        expect(wrapper.get('[role="alert"]').text()).toContain(
            'At least one active owner is required for this organization and its projects.'
        );
        expect(router.currentRoute.value.name).toBe('organizationMembers');

        // Case 2: Backend succeeds (204) when another active owner exists on Page 2
        vi.mocked(organizationsApi.removeMember).mockResolvedValueOnce(undefined);
        await wrapper.findAll('button').find((b) => b.text() === 'Leave organization')!.trigger('click');
        await wrapper.findAll('button').find((b) => b.text() === 'Confirm leave')!.trigger('click');
        await flushPromises();

        expect(organizationsApi.removeMember).toHaveBeenCalledWith('org-1', 'user-owner');
        expect(router.currentRoute.value.name).toBe('dashboard');
        wrapper.unmount();
    });

    it('Defect C regression: paginates and searches ownership candidates beyond 100 members while preserving selection', async () => {
        const candidatePage1 = Array.from({ length: 20 }, (_, i) => ({
            userId: `user-p1-${i + 1}`,
            email: `p1-${i + 1}@example.com`,
            displayName: `PageOne Candidate ${i + 1}`,
            role: 'organizationMember',
            status: 'active',
            joinedAt: '2026-10-02T00:00:00Z',
            version: 1,
        }));
        const candidate105 = {
            userId: 'user-105',
            email: 'candidate105@example.com',
            displayName: 'Candidate 105 Beyond Hundred',
            role: 'organizationMember',
            status: 'active',
            joinedAt: '2026-10-05T00:00:00Z',
            version: 1,
        };

        vi.mocked(organizationsApi.listMembers).mockImplementation(async (_orgId, params) => {
            if (params?.search === 'Candidate 105') {
                return {
                    data: [candidate105],
                    meta: { page: 1, pageSize: 20, totalCount: 1 },
                };
            }
            if (params?.page === 6) {
                return {
                    data: [candidate105],
                    meta: { page: 6, pageSize: 20, totalCount: 125 },
                };
            }
            return {
                data: candidatePage1,
                meta: { page: params?.page ?? 1, pageSize: 20, totalCount: 125 },
            };
        });
        vi.mocked(organizationsApi.transferOwnership).mockResolvedValue({
            data: {
                previousOwner: { ...membersList[0]!, role: 'organizationAdmin', version: 2 },
                newOwner: { ...candidate105, role: 'organizationOwner', version: 2 },
            },
        });

        const { wrapper } = await renderView(OrganizationSettingsView, '/organizations/org-1/settings');

        // Verify pageSize=20 is used (never hardcoded pageSize=100)
        expect(organizationsApi.listMembers).toHaveBeenCalledWith('org-1', {
            page: 1,
            pageSize: 20,
            status: 'active',
            search: undefined,
        });

        // Search for Candidate 105 (beyond first 100 members)
        await wrapper.get('#transfer-candidate-search').setValue('Candidate 105');
        await wrapper.get('form[aria-label="Search ownership candidates"]').trigger('submit');
        await flushPromises();

        expect(organizationsApi.listMembers).toHaveBeenLastCalledWith('org-1', {
            page: 1,
            pageSize: 20,
            status: 'active',
            search: 'Candidate 105',
        });

        await wrapper.get('#transfer-target-member').setValue('user-105');
        await wrapper.get('#transfer-target-member').trigger('change');

        // Clear search -> returns to Page 1 of 125 members, but selected Candidate 105 is preserved in the selector!
        await wrapper.findAll('button').find((b) => b.text() === 'Clear search')!.trigger('click');
        await flushPromises();

        const optionValues = wrapper
            .get('#transfer-target-member')
            .findAll('option')
            .map((o) => o.element.value);
        expect(optionValues).toContain('user-105');
        expect((wrapper.get('#transfer-target-member').element as HTMLSelectElement).value).toBe('user-105');

        // Complete transfer to Candidate 105
        await wrapper.findAll('button').find((b) => b.text() === 'Review ownership transfer')!.trigger('click');
        await wrapper.findAll('button').find((b) => b.text() === 'Confirm ownership transfer')!.trigger('click');
        await flushPromises();

        expect(organizationsApi.transferOwnership).toHaveBeenCalledWith('org-1', {
            newOwnerUserId: 'user-105',
            retainCurrentOwnerAs: 'organizationAdmin',
            version: 1,
        });
        expect(wrapper.text()).toContain('Transferred organization ownership to Candidate 105 Beyond Hundred');
        wrapper.unmount();
    });

    it('Defect D regression: stale organization fetch or in-flight mutation from Org A never overwrites Org B form state', async () => {
        let resolveOrgA!: (val: { data: typeof ownerOrg }) => void;
        let resolveSaveA!: (val: { data: typeof ownerOrg }) => void;
        const orgB = {
            ...ownerOrg,
            id: 'org-2',
            name: 'Organization B',
            slug: 'org-b',
            version: 4,
        };

        // 1. Slow initial fetch for Org A, fast fetch for Org B
        vi.mocked(organizationsApi.get).mockImplementation((orgId: string) => {
            if (orgId === 'org-1') {
                return new Promise((resolve) => {
                    resolveOrgA = resolve;
                });
            }
            return Promise.resolve({ data: orgB });
        });

        const { wrapper, router } = await renderView(OrganizationSettingsView, '/organizations/org-1/settings');

        // User switches to Org B before Org A finishes loading
        await router.push('/organizations/org-2/settings');
        await flushPromises();

        expect((wrapper.get('#org-settings-name').element as HTMLInputElement).value).toBe('Organization B');
        expect((wrapper.get('#org-settings-slug').element as HTMLInputElement).value).toBe('org-b');

        // Delayed Org A response now resolves
        resolveOrgA({ data: { ...ownerOrg, id: 'org-1', name: 'Stale Org A', slug: 'stale-org-a' } });
        await flushPromises();

        // Org B form inputs must NOT be overwritten by delayed Org A response
        expect((wrapper.get('#org-settings-name').element as HTMLInputElement).value).toBe('Organization B');
        expect((wrapper.get('#org-settings-slug').element as HTMLInputElement).value).toBe('org-b');

        // 2. Switching organizations while a save mutation is in flight on Org B
        const orgC = {
            ...ownerOrg,
            id: 'org-3',
            name: 'Organization C',
            slug: 'org-c',
            version: 1,
        };
        vi.mocked(organizationsApi.get).mockImplementation(async (orgId: string) => ({
            data: orgId === 'org-3' ? orgC : orgB,
        }));
        vi.mocked(organizationsApi.update).mockImplementation(
            () =>
                new Promise((resolve) => {
                    resolveSaveA = resolve;
                })
        );

        await wrapper.get('#org-settings-name').setValue('Edited B');
        await wrapper.get('section[aria-label="General settings"] form').trigger('submit');

        // Switch to Org C while Org B save is still pending
        await router.push('/organizations/org-3/settings');
        await flushPromises();

        expect((wrapper.get('#org-settings-name').element as HTMLInputElement).value).toBe('Organization C');
        expect((wrapper.get('#org-settings-name').element as HTMLInputElement).disabled).toBe(false);

        // Pending save for Org B resolves late
        resolveSaveA({ data: { ...orgB, name: 'Edited B', version: 5 } });
        await flushPromises();

        // Org C must not show Org B's save banner or values
        expect(wrapper.text()).not.toContain('Saved organization settings (version 5)');
        expect((wrapper.get('#org-settings-name').element as HTMLInputElement).value).toBe('Organization C');
        wrapper.unmount();
    });
});
