<script setup lang="ts">
import { computed, onScopeDispose, ref, watch } from 'vue';
import { organizationsApi } from '@/api/workspace/organizations.api';
import { projectsApi } from '@/api/workspace/projects.api';
import type { OrganizationMember } from '@/api/workspace/workspace.models';
import { useResource, workspaceError } from '@/features/workspace/workspaceState';
import PageControls from './PageControls.vue';
import ResourceState from './ResourceState.vue';
const props = defineProps<{ organizationId: string; projectId: string; source: 'organization' | 'project'; excludeUserId?: string }>();
const emit = defineEmits<{ select: [member: OrganizationMember] }>();
const page = ref(1);
const search = ref('');
const applied = ref('');
const selectionError = ref('');
const checking = ref(false);
let disposed = false;
onScopeDispose(() => { disposed = true; });
const { data, loading, error, load } = useResource(async () => {
    const params = { page: page.value, pageSize: 20, status: 'active', search: applied.value || undefined };
    return props.source === 'organization' ? organizationsApi.listMembers(props.organizationId, params) : projectsApi.listMembers(props.projectId, params);
});
const eligibleCandidates = computed(() => data.value?.data.filter(row => row.userId !== props.excludeUserId && (!row.accountStatus || row.accountStatus === 'active')) ?? []);
watch(() => [props.organizationId, props.projectId, props.source], () => { page.value = 1; applied.value = ''; search.value = ''; selectionError.value = ''; void load(); }, { immediate: true });
watch([page, applied], load);
async function select(member: OrganizationMember) {
    if (checking.value) return;
    if (props.source === 'project') { emit('select', member); return; }
    checking.value = true; selectionError.value = '';
    const capturedProject = props.projectId;
    try {
        const existing = await projectsApi.listMembers(capturedProject, { search: member.email, status: 'active', pageSize: 100 });
        if (disposed || capturedProject !== props.projectId) return;
        if (existing.data.some(row => row.userId === member.userId)) selectionError.value = 'This user is already an active project member.';
        else emit('select', member);
    } catch (cause) { if (!disposed && capturedProject === props.projectId) selectionError.value = workspaceError(cause); }
    finally { if (!disposed) checking.value = false; }
}
</script>
<template>
    <section aria-label="Member candidates" class="my-4">
        <form class="flex flex-wrap gap-2" @submit.prevent="page = 1; applied = search.trim()">
            <label class="grow">Search candidates<input v-model="search" class="workspace-input" type="search" /></label>
            <button class="workspace-button">Search candidates</button>
        </form>
        <p v-if="selectionError" role="alert" class="mt-3 text-rose-300">{{ selectionError }}</p>
        <ResourceState :loading="loading" :error="error" @retry="load">
            <ul v-if="data" class="my-3 space-y-2" aria-label="Candidates">
                <li v-for="member in eligibleCandidates" :key="member.userId" class="flex flex-wrap items-center justify-between gap-2 rounded border border-slate-800 p-3">
                    <span class="break-all">{{ member.displayName }} · {{ member.email }}</span>
                    <button class="workspace-button" :disabled="checking" @click="select(member)">Select {{ member.displayName }}</button>
                </li>
            </ul>
            <p v-if="data && !eligibleCandidates.length" class="my-3 text-slate-400">No eligible candidates on this page.</p>
            <PageControls v-if="data" :page="page" :total="data.meta.totalCount" @change="page = $event" />
        </ResourceState>
    </section>
</template>
