import axios from 'axios';
import { onScopeDispose, ref, shallowRef } from 'vue';

// Clear old tenant data immediately; only the latest request may publish a result.
export function useResource<T>(
    fetcher: () => Promise<T>,
    onSuccess?: (result: T) => void
) {
    const data = shallowRef<T>();
    const loading = ref(false);
    const error = ref('');
    let generation = 0;
    let disposed = false;
    function reset() {
        generation++;
        data.value = undefined;
        loading.value = false;
        error.value = '';
    }
    async function load() {
        const current = ++generation;
        data.value = undefined;
        error.value = '';
        loading.value = true;
        try {
            const result = await fetcher();
            if (!disposed && current === generation) {
                data.value = result;
                onSuccess?.(result);
            }
        } catch (cause) {
            if (!disposed && current === generation) error.value = workspaceError(cause);
        } finally {
            if (!disposed && current === generation) loading.value = false;
        }
    }
    onScopeDispose(() => {
        disposed = true;
        generation++;
    });
    return { data, loading, error, load, reset };
}

export const ORGANIZATION_MAX_NAME_LENGTH = 160;
export const PROJECT_MAX_NAME_LENGTH = 80;
export const WORKSPACE_MAX_SLUG_LENGTH = 80;
export const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export function isValidOrganizationName(value: string): boolean {
    const trimmed = value.trim();
    return trimmed.length >= 1 && trimmed.length <= ORGANIZATION_MAX_NAME_LENGTH;
}

export function isValidProjectName(value: string): boolean {
    const trimmed = value.trim();
    return trimmed.length >= 1 && trimmed.length <= PROJECT_MAX_NAME_LENGTH;
}

export function isValidWorkspaceSlug(value: string): boolean {
    const trimmed = value.trim();
    return (
        trimmed.length >= 1 &&
        trimmed.length <= WORKSPACE_MAX_SLUG_LENGTH &&
        SLUG_PATTERN.test(trimmed)
    );
}

export function validateOrganizationFields(name: string, slug: string): string {
    if (!isValidOrganizationName(name)) {
        return `Organization name must be between 1 and ${ORGANIZATION_MAX_NAME_LENGTH} characters.`;
    }
    if (!isValidWorkspaceSlug(slug)) {
        return `Slug must be 1–${WORKSPACE_MAX_SLUG_LENGTH} characters using lowercase letters, numbers, and single hyphens.`;
    }
    return '';
}

export class WorkspaceUnavailable extends Error {}

export const workspaceRevision = ref(0);

export function notifyWorkspaceChanged() {
    workspaceRevision.value++;
}

let pendingInvitationToken = '';

export function captureInvitationToken(token: unknown) {
    if (typeof token === 'string' && token.trim()) {
        pendingInvitationToken = token.trim();
    }
}

export function getPendingInvitationToken(): string {
    return pendingInvitationToken;
}

export function clearPendingInvitationToken() {
    pendingInvitationToken = '';
}

export function extractErrorCode(cause: unknown): string | undefined {
    if (axios.isAxiosError<{ error?: { code?: string } }>(cause)) {
        return cause.response?.data?.error?.code;
    }
    return undefined;
}

export function extractStatus(cause: unknown): number | undefined {
    if (axios.isAxiosError(cause)) {
        return cause.response?.status;
    }
    return undefined;
}

export function workspaceError(cause: unknown): string {
    if (cause instanceof WorkspaceUnavailable) {
        return 'This workspace is unavailable or you no longer have access.';
    }
    if (axios.isAxiosError<{ error?: { code?: string; message?: string } }>(cause)) {
        const code = cause.response?.data?.error?.code;
        if (code === 'INVITE_INVALID_OR_EXPIRED') {
            return 'This invitation is invalid, expired, revoked, or does not match your verified account email.';
        }
        if (code === 'LAST_OWNER_REQUIRED') {
            return 'At least one active owner is required for this organization and its projects.';
        }
        if (code === 'MEMBERSHIP_NOT_FOUND') {
            return 'That membership was not found or is no longer eligible.';
        }
        if (code === 'MEMBERSHIP_ALREADY_EXISTS') return 'This user is already an active project member.';
        if (code === 'PROJECT_ARCHIVED') return 'This project is archived. Restore it before making changes.';
        if (code === 'PROJECT_NOT_ACTIVE') return 'This project is not active and cannot be modified.';
        if (code === 'SETTINGS_POLICY_RESTRICTED') return 'AI advisory is restricted by current platform or organization policy. Reload settings to review the effective policy.';
        if (code === 'ANALYSIS_PROFILE_INVALID') return 'Select an active compatible analysis profile from the current list.';
        if (cause.response?.status === 404) {
            return 'This workspace is unavailable or you no longer have access.';
        }
        if (cause.response?.status === 403) {
            return 'You do not have permission for this action. Refresh to check your current access.';
        }
        if (code === 'CONCURRENCY_CONFLICT') {
            return 'The workspace changed. Refresh the latest data before trying again.';
        }
        if (code === 'ORGANIZATION_SLUG_EXISTS' || code === 'PROJECT_NAME_EXISTS' || code === 'PROJECT_SLUG_EXISTS') {
            return 'That slug is already in use. Choose another slug.';
        }
        if (code === 'ORGANIZATION_SLUG_RESERVED') {
            return 'That slug is reserved. Choose another slug.';
        }
        if (code === 'ORGANIZATION_QUOTA_EXCEEDED') {
            return 'The organization limit has been reached.';
        }
        if (code === 'ORGANIZATION_INVITATION_QUOTA_EXCEEDED') {
            return 'The pending invitation limit for this organization has been reached.';
        }
        if (code === 'ORGANIZATION_NOT_ACTIVE') {
            return 'This organization is not active and cannot be modified.';
        }
        if (code === 'INVITE_ALREADY_PENDING') {
            return 'An active invitation is already pending for this email address.';
        }
        if (code === 'IDEMPOTENCY_KEY_REUSED') {
            return 'This request key was already used with different parameters. Please try again.';
        }
        if (cause.response?.status === 400) {
            return 'Check the name, slug and selected options, then try again.';
        }
    }
    return 'Unable to reach the workspace. Please retry.';
}

export function canCreateProject(role: string, status: string) {
    return status === 'active' && ['organizationOwner', 'organizationAdmin'].includes(role);
}

export function canManageOrganization(role: string, status: string) {
    return status === 'active' && ['organizationOwner', 'organizationAdmin'].includes(role);
}

export function canInviteMembers(role: string, status: string) {
    return status === 'active' && ['organizationOwner', 'organizationAdmin'].includes(role);
}

export function canInviteAdmin(role: string, status: string) {
    return status === 'active' && role === 'organizationOwner';
}

export function canTransferOrganizationOwnership(role: string, status: string) {
    return status === 'active' && role === 'organizationOwner';
}

export function canDeleteOrganization(role: string, status: string) {
    return status === 'active' && role === 'organizationOwner';
}

export function canManageMember(actorRole: string, orgStatus: string, targetRole: string) {
    if (orgStatus !== 'active') return false;
    if (actorRole === 'organizationOwner') return true;
    if (actorRole === 'organizationAdmin') {
        return targetRole === 'organizationMember';
    }
    return false;
}
