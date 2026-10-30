import { computed, onScopeDispose, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { projectsApi } from '@/api/workspace/projects.api';
import { organizationsApi } from '@/api/workspace/organizations.api';
import { extractErrorCode, notifyWorkspaceChanged, useResource, WorkspaceUnavailable, workspaceError } from './workspaceState';

export const projectRoles = ['projectOwner', 'maintainer', 'developer', 'reviewer', 'viewer'] as const;
export const managesProject = (role: string) => ['projectOwner', 'maintainer'].includes(role);
export const hasProjectRole = (role: string) => projectRoles.some(value => value === role);

export function useProjectWorkspace() {
    const route = useRoute();
    const identity = computed(() => `${String(route.params.organizationId)}/${String(route.params.projectId)}`);
    const id = computed(() => String(route.params.projectId));
    const resource = useResource(async () => {
        const organizationId = String(route.params.organizationId);
        const project = (await projectsApi.get(id.value)).data;
        if (project.organizationId !== organizationId) throw new WorkspaceUnavailable();
        const org = (await organizationsApi.get(organizationId)).data;
        return { project, org };
    });
    const busy = ref(false);
    const actionError = ref('');
    const success = ref('');
    let epoch = 0;
    let disposed = false;
    watch(identity, () => { epoch++; busy.value = false; actionError.value = ''; success.value = ''; void resource.load(); }, { immediate: true });
    onScopeDispose(() => { disposed = true; epoch++; });
    const mutable = computed(() => resource.data.value?.project.status === 'active' && resource.data.value.org.status === 'active');
    const manager = computed(() => managesProject(resource.data.value?.project.currentUserRole ?? ''));
    const owner = computed(() => resource.data.value?.project.currentUserRole === 'projectOwner');
    function captureContext() {
        const capturedEpoch = epoch;
        return () => !disposed && epoch === capturedEpoch;
    }
    async function mutate(operation: (projectId: string) => Promise<unknown>, message: string, after?: () => void | Promise<void>) {
        if (busy.value || !resource.data.value) return;
        const capturedId = id.value;
        const capturedEpoch = epoch;
        const current = () => !disposed && epoch === capturedEpoch && id.value === capturedId;
        busy.value = true; actionError.value = ''; success.value = '';
        try {
            await operation(capturedId);
            if (!current()) return;
            notifyWorkspaceChanged();
            await resource.load();
            if (!current()) return;
            success.value = message;
            await after?.();
        } catch (cause) {
            if (!current()) return;
            actionError.value = workspaceError(cause);
            if (extractErrorCode(cause) === 'CONCURRENCY_CONFLICT') {
                await resource.load();
                if (current()) actionError.value = 'The project changed. Latest versions loaded; review your preserved edits and submit again deliberately.';
            }
        } finally { if (current()) busy.value = false; }
    }
    return { ...resource, id, identity, busy, actionError, success, mutable, manager, owner, mutate, captureContext };
}
