<script setup lang="ts">
import { ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { organizationsApi } from '@/api/workspace/organizations.api';
import {
    canInviteMembers,
    canManageOrganization,
    notifyWorkspaceChanged,
    useResource,
} from '@/features/workspace/workspaceState';
import CreateWorkspaceForm from '@/features/workspace/CreateWorkspaceForm.vue';
import ResourceState from '@/components/workspace/ResourceState.vue';
import PageControls from '@/components/workspace/PageControls.vue';
import type { ProjectInput } from '@/api/workspace/workspace.models';
const router = useRouter();
const page = ref(1);
const creating = ref(false);
const { data, loading, error, load } = useResource(() => organizationsApi.list(page.value));
watch(page, load, { immediate: true });
async function create(input: ProjectInput, key: string) {
    const response = await organizationsApi.create({ name: input.name, slug: input.slug }, key);
    notifyWorkspaceChanged();
    if (router.currentRoute.value.name === 'dashboard') await router.push({ name: 'organizationProjects', params: { organizationId: response.data.id } });
}
</script>
<template>
    <div class="mb-8 flex flex-wrap items-start justify-between gap-4">
        <div><p class="text-sm text-cyan-400">Your workspace</p><h1 class="mt-2 text-3xl font-semibold">Dashboard</h1><p class="mt-3 text-slate-400">Choose an organization to work on your projects.</p></div>
        <button v-if="data && data.meta.totalCount > 0" class="workspace-button" :aria-expanded="creating" @click="creating = !creating">{{ creating ? 'Close form' : 'New organization' }}</button>
    </div>
    <ResourceState :loading="loading" :error="error" @retry="load">
        <template v-if="data">
            <section v-if="data.meta.totalCount === 0" class="workspace-panel mb-5">
                <h2 class="text-xl font-semibold">Create your first organization</h2><p class="mt-3 text-slate-400">An organization is your team's workspace. You will become its owner and can create your first project next.</p>
            </section>
            <CreateWorkspaceForm v-if="creating || data.meta.totalCount === 0" kind="organization" :submit="create" class="mb-6" />
            <h2 class="mb-4 text-lg font-semibold">Your organizations</h2>
            <p v-if="!data.data.length && data.meta.totalCount > 0" class="workspace-panel">No organizations on this page. Use Previous to return.</p>
            <ul class="grid gap-4 lg:grid-cols-2" aria-label="Organizations">
                <li v-for="org in data.data" :key="org.id" class="workspace-panel">
                    <router-link :to="{ name: 'organizationProjects', params: { organizationId: org.id } }" class="text-lg font-semibold text-cyan-300 hover:underline">{{ org.name }}</router-link>
                    <p class="mt-2 break-all text-sm text-slate-400">{{ org.slug }}</p>
                    <p class="mt-3 text-sm">{{ org.currentUserRole }} · {{ org.status }}</p>
                    <div class="mt-4 flex flex-wrap gap-3 text-xs text-slate-300">
                        <router-link class="hover:text-cyan-300 hover:underline" :to="{ name: 'organizationOverview', params: { organizationId: org.id } }">Overview</router-link>
                        <router-link class="hover:text-cyan-300 hover:underline" :to="{ name: 'organizationProjects', params: { organizationId: org.id } }">Projects</router-link>
                        <router-link class="hover:text-cyan-300 hover:underline" :to="{ name: 'organizationMembers', params: { organizationId: org.id } }">Members</router-link>
                        <router-link v-if="canInviteMembers(org.currentUserRole, org.status)" class="hover:text-cyan-300 hover:underline" :to="{ name: 'organizationInvitations', params: { organizationId: org.id } }">Invitations</router-link>
                        <router-link v-if="canManageOrganization(org.currentUserRole, org.status)" class="hover:text-cyan-300 hover:underline" :to="{ name: 'organizationSettings', params: { organizationId: org.id } }">Settings</router-link>
                    </div>
                </li>
            </ul>
            <PageControls :page="page" :total="data.meta.totalCount" @change="page = $event" />
        </template>
    </ResourceState>
</template>
