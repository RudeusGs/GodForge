<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { organizationsApi } from '@/api/workspace/organizations.api';
import { projectsApi } from '@/api/workspace/projects.api';
import { canCreateProject, useResource } from '@/features/workspace/workspaceState';
import CreateWorkspaceForm from '@/features/workspace/CreateWorkspaceForm.vue';
import ResourceState from '@/components/workspace/ResourceState.vue';
import PageControls from '@/components/workspace/PageControls.vue';
import type { ProjectInput } from '@/api/workspace/workspace.models';
const route = useRoute();
const router = useRouter();
const id = computed(() => String(route.params.organizationId));
const page = ref(1);
const catalog = ref(false);
const creating = ref(false);
const { data, loading, error, load } = useResource(async () => {
    const organizationId = id.value;
    const requestedCatalog = catalog.value;
    const requestedPage = page.value;
    const org = (await organizationsApi.get(organizationId)).data;
    const isCatalog = requestedCatalog && canCreateProject(org.currentUserRole, org.status);
    const projects = isCatalog ? await projectsApi.catalog(organizationId, requestedPage) : await projectsApi.list(organizationId, requestedPage);
    return { org, projects, isCatalog };
});
const mayCreate = computed(() => !!data.value && canCreateProject(data.value.org.currentUserRole, data.value.org.status));
watch(id, () => { page.value = 1; catalog.value = false; creating.value = false; void load(); }, { immediate: true });
watch([page, catalog], load);
function changeCatalog(value: boolean) { page.value = 1; catalog.value = value; }
async function create(input: ProjectInput, key: string) {
    const organizationId = id.value;
    const result = await projectsApi.create(organizationId, input, key);
    if (organizationId === id.value) await router.push({ name: 'projectDetail', params: { organizationId, projectId: result.data.id } });
}
</script>
<template>
    <ResourceState :loading="loading" :error="error" @retry="load">
        <template v-if="data">
            <div class="mb-8 flex flex-wrap items-start justify-between gap-4">
                <div><p class="text-sm text-cyan-400">{{ data.org.name }} / {{ data.org.slug }}</p><h1 class="mt-2 text-3xl font-semibold">Projects</h1><p class="mt-3 text-sm text-slate-400">{{ data.org.currentUserRole }} · {{ data.org.status }}</p></div>
                <button v-if="mayCreate" class="workspace-button" :aria-expanded="creating" @click="creating = !creating">{{ creating ? 'Close form' : 'New project' }}</button>
            </div>
            <CreateWorkspaceForm v-if="creating && mayCreate" :key="id" kind="project" :submit="create" class="mb-6" />
            <p v-if="!mayCreate" class="mb-5 text-sm text-slate-400">Project creation requires an active organization and an OrganizationOwner or OrganizationAdmin role.</p>
            <div class="mb-5 flex flex-wrap gap-3" aria-label="Project list scope">
                <button class="workspace-button" :aria-pressed="!catalog" @click="changeCatalog(false)">Your projects</button>
                <button v-if="mayCreate" class="workspace-button" :aria-pressed="catalog" @click="changeCatalog(true)">Organization catalog</button>
            </div>
            <p v-if="data.isCatalog" class="mb-5 text-sm text-slate-400">Administration metadata only. Organization administration does not grant project content access. Open assigned projects from Your projects.</p>
            <div v-if="!data.projects.data.length" class="workspace-panel"><h2 class="text-xl font-semibold">{{ page > 1 ? 'No projects on this page' : data.isCatalog ? 'No projects yet' : 'No assigned projects yet' }}</h2><p class="mt-3 text-slate-400">{{ mayCreate ? 'Create a project to start your team workspace, or check the organization catalog.' : 'Ask a project owner to add you to a project.' }}</p></div>
            <ul v-else class="space-y-4" aria-label="Projects">
                <li v-for="project in data.projects.data" :key="project.id" class="workspace-panel">
                    <h2 class="text-lg font-semibold"><router-link v-if="!data.isCatalog" class="text-cyan-300 hover:underline" :to="{ name: 'projectDetail', params: { organizationId: id, projectId: project.id } }">{{ project.name }}</router-link><span v-else>{{ project.name }}</span></h2>
                    <p class="mt-2 break-all text-sm text-slate-400">{{ project.slug }} · {{ project.status }}</p>
                    <p v-if="'currentUserRole' in project" class="mt-3 text-sm">Your project role: {{ project.currentUserRole }}</p>
                    <p v-if="'memberCount' in project" class="mt-3 text-sm">{{ project.memberCount }} active members · {{ project.ownerCount }} owners</p>
                </li>
            </ul>
            <PageControls :page="page" :total="data.projects.meta.totalCount" @change="page = $event" />
        </template>
    </ResourceState>
</template>
