<script setup lang="ts">
import { ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { organizationsApi } from '@/api/workspace/organizations.api';
import { useResource, workspaceRevision } from './workspaceState';
import PageControls from '@/components/workspace/PageControls.vue';
defineProps<{ selected: string }>();
const router = useRouter();
const route = useRoute();
const page = ref(1);
const { data, loading, error, load } = useResource(() => organizationsApi.list(page.value));
watch([page, () => route.fullPath, workspaceRevision], load, { immediate: true });
const organizationRoutes = new Set([
    'organizationOverview',
    'organizationProjects',
    'organizationMembers',
    'organizationInvitations',
    'organizationSettings',
]);
function select(event: Event) {
    const id = (event.target as HTMLSelectElement).value;
    if (!id) return;
    const targetRoute =
        typeof route.name === 'string' && organizationRoutes.has(route.name)
            ? route.name
            : 'organizationProjects';
    void router.push({ name: targetRoute, params: { organizationId: id } });
}
</script>
<template>
    <div>
        <label for="organization-switcher" class="text-xs font-semibold uppercase tracking-wider text-slate-400">Organization</label>
        <p v-if="loading" role="status" class="mt-3 text-sm text-slate-400">Loading organizations…</p>
        <div v-else-if="error" role="alert" class="mt-3 text-sm"><p>{{ error }}</p><button class="workspace-button mt-2" @click="load">Retry organizations</button></div>
        <template v-else-if="data">
            <select id="organization-switcher" :value="selected" class="workspace-input" @change="select">
                <option value="">Choose organization</option>
                <option v-if="selected && !data.data.some(org => org.id === selected)" :value="selected" disabled>Selected organization</option>
                <option v-for="org in data.data" :key="org.id" :value="org.id">{{ org.name }}</option>
            </select>
            <p v-if="!data.data.length" class="mt-2 text-sm text-slate-400">Create your first organization to begin.</p>
            <PageControls :page="page" :total="data.meta.totalCount" @change="page = $event" />
        </template>
    </div>
</template>
