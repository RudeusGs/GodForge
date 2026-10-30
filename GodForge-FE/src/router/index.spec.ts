import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import router from './index';
import {
    clearPendingInvitationToken,
    getPendingInvitationToken,
} from '../features/workspace/workspaceState';
import { safeReturnTo } from '../utils/safeReturnTo';
import { useAuthStore } from '../stores/auth.store';

vi.mock('../stores/auth.store', () => ({
    useAuthStore: vi.fn(),
}));

describe('authentication routes', () => {
    let mockAuthStore: { initialize: ReturnType<typeof vi.fn>; initialized: boolean; isAuthenticated: boolean };

    beforeEach(() => {
        setActivePinia(createPinia());
        clearPendingInvitationToken();
        mockAuthStore = {
            initialize: vi.fn().mockResolvedValue(undefined),
            initialized: true,
            isAuthenticated: false,
        };
        vi.mocked(useAuthStore).mockReturnValue(mockAuthStore as unknown as ReturnType<typeof useAuthStore>);
    });

    it('allows reset-password while a user is already authenticated', () => {
        const route = router.getRoutes().find(candidate => candidate.name === 'resetPassword');
        expect(route?.meta.requiresGuest).not.toBe(true);
    });

    it('accepts only internal return targets', () => {
        expect(safeReturnTo('/projects/123?tab=health')).toBe('/projects/123?tab=health');
        expect(safeReturnTo('//evil.example/path')).toBeNull();
        expect(safeReturnTo('/\\evil.example/path')).toBeNull();
        expect(safeReturnTo('https://evil.example/path')).toBeNull();
    });

    it('transient protected-route bootstrap does NOT redirect to login', async () => {
        mockAuthStore.initialized = false;
        mockAuthStore.isAuthenticated = false; // unknown due to transient failure

        await router.push('/dashboard');
        
        expect(router.currentRoute.value.path).toBe('/dashboard');
    });

    it('definitive unauthenticated protected route redirects to login', async () => {
        mockAuthStore.initialized = true;
        mockAuthStore.isAuthenticated = false;

        await router.push('/account/sessions');

        expect(router.currentRoute.value.path).toBe('/login');
        expect(router.currentRoute.value.query.returnTo).toBe('/account/sessions');
    });

    it('authenticated guest-only route redirects to dashboard', async () => {
        mockAuthStore.initialized = true;
        mockAuthStore.isAuthenticated = true;

        await router.push('/login');

        expect(router.currentRoute.value.path).toBe('/dashboard');
    });

    it.each([
        '/organizations/tenant',
        '/organizations/tenant/projects',
        '/organizations/tenant/projects/project',
        '/organizations/tenant/members',
        '/organizations/tenant/invitations',
        '/organizations/tenant/settings',
    ])('protects workspace deep link %s and preserves the return path', async path => {
        await router.push(path);
        expect(router.currentRoute.value.name).toBe('login');
        expect(router.currentRoute.value.query.returnTo).toBe(path);
    });

    it('captures invitation token into memory and strips token from login returnTo query', async () => {
        await router.push('/organization-invitations/accept?token=raw-secret-invite-token');
        expect(router.currentRoute.value.name).toBe('login');
        expect(router.currentRoute.value.query.returnTo).toBe('/organization-invitations/accept');
        expect(getPendingInvitationToken()).toBe('raw-secret-invite-token');
    });

    it('allows authenticated workspace deep links without claiming a tenant permission', async () => {
        mockAuthStore.isAuthenticated = true;
        await router.push('/organizations/tenant/projects/project');
        expect(router.currentRoute.value.name).toBe('projectDetail');
    });
});
