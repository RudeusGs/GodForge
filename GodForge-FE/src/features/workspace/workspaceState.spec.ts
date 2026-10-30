import { effectScope } from 'vue';
import { AxiosError } from 'axios';
import { describe, expect, it, vi } from 'vitest';
import {
    canCreateProject,
    isValidOrganizationName,
    isValidWorkspaceSlug,
    ORGANIZATION_MAX_NAME_LENGTH,
    useResource,
    validateOrganizationFields,
    workspaceError,
    WORKSPACE_MAX_SLUG_LENGTH,
} from './workspaceState';

export function apiError(status: number, code = '') {
    return new AxiosError('safe', undefined, undefined, undefined, { status, data: { error: { code } }, headers: {}, statusText: '', config: {} as never });
}
describe('workspace server state', () => {
    it('clears old data and ignores stale tenant responses', async () => {
        let first!: (value: string) => void;
        const fetcher = vi.fn().mockImplementationOnce(() => new Promise<string>(resolve => { first = resolve; })).mockResolvedValueOnce('Tenant B');
        const scope = effectScope();
        const resource = scope.run(() => useResource<string>(fetcher))!;
        const pending = resource.load();
        expect(resource.loading.value).toBe(true);
        await resource.load();
        first('Tenant A'); await pending;
        expect(resource.data.value).toBe('Tenant B');
        scope.stop();
    });
    it('exposes retryable failures and clears them on recovery', async () => {
        const scope = effectScope();
        const fetcher = vi.fn().mockRejectedValueOnce(apiError(403)).mockResolvedValue('fresh');
        const resource = scope.run(() => useResource<string>(fetcher))!;
        await resource.load();
        expect(resource.data.value).toBeUndefined();
        expect(resource.error.value).toContain('permission');
        await resource.load();
        expect(resource.data.value).toBe('fresh'); expect(resource.error.value).toBe('');
        scope.stop();
    });
    it.each([[404, '', 'unavailable'], [403, '', 'permission'], [409, 'CONCURRENCY_CONFLICT', 'changed'], [409, 'PROJECT_NAME_EXISTS', 'slug'], [400, 'VALIDATION_ERROR', 'Check'], [503, '', 'retry']])('maps %s %s safely', (status, code, expected) => {
        expect(workspaceError(apiError(Number(status), String(code)))).toContain(expected);
    });
    it('uses only active organization administration roles for create affordances', () => {
        expect(canCreateProject('organizationOwner', 'active')).toBe(true);
        expect(canCreateProject('organizationAdmin', 'active')).toBe(true);
        expect(canCreateProject('organizationMember', 'active')).toBe(false);
        expect(canCreateProject('projectOwner', 'active')).toBe(false);
        expect(canCreateProject('organizationOwner', 'suspended')).toBe(false);
    });
    it('validates organization name and slug boundaries matching backend domain rules', () => {
        expect(ORGANIZATION_MAX_NAME_LENGTH).toBe(160);
        expect(WORKSPACE_MAX_SLUG_LENGTH).toBe(80);

        // Name boundaries: 1..160 after trim
        expect(isValidOrganizationName('A')).toBe(true);
        expect(isValidOrganizationName('  A  ')).toBe(true);
        expect(isValidOrganizationName('X'.repeat(160))).toBe(true);
        expect(isValidOrganizationName('')).toBe(false);
        expect(isValidOrganizationName('   ')).toBe(false);
        expect(isValidOrganizationName('X'.repeat(161))).toBe(false);

        // Slug boundaries: 1..80 lowercase alphanumeric + single interior hyphens
        expect(isValidWorkspaceSlug('a')).toBe(true);
        expect(isValidWorkspaceSlug('a-1-b')).toBe(true);
        expect(isValidWorkspaceSlug('s'.repeat(80))).toBe(true);
        expect(isValidWorkspaceSlug('')).toBe(false);
        expect(isValidWorkspaceSlug('s'.repeat(81))).toBe(false);
        expect(isValidWorkspaceSlug('-leading')).toBe(false);
        expect(isValidWorkspaceSlug('trailing-')).toBe(false);
        expect(isValidWorkspaceSlug('double--hyphen')).toBe(false);
        expect(isValidWorkspaceSlug('UpperCase')).toBe(false);
        expect(isValidWorkspaceSlug('under_score')).toBe(false);
        expect(isValidWorkspaceSlug('with space')).toBe(false);

        expect(validateOrganizationFields('A', 'a')).toBe('');
        expect(validateOrganizationFields('X'.repeat(160), 's'.repeat(80))).toBe('');
        expect(validateOrganizationFields('', 'a')).toContain('1 and 160');
        expect(validateOrganizationFields('Valid', '-bad')).toContain('1–80');
    });
});
