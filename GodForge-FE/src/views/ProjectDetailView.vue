<script setup lang="ts">
import { watch } from 'vue';
import { useRoute } from 'vue-router';
import { projectsApi } from '@/api/workspace/projects.api';
import { organizationsApi } from '@/api/workspace/organizations.api';
import { useResource, WorkspaceUnavailable } from '@/features/workspace/workspaceState';
import ResourceState from '@/components/workspace/ResourceState.vue';
import ProjectSectionNav from '@/components/workspace/ProjectSectionNav.vue';
const route = useRoute();
const { data, loading, error, load } = useResource(async () => {
    const organizationId = String(route.params.organizationId);
    const project = (await projectsApi.get(String(route.params.projectId))).data;
    if (project.organizationId !== organizationId) throw new WorkspaceUnavailable();
    const org = (await organizationsApi.get(organizationId)).data;
    return { project, org };
});
watch(() => route.fullPath, load, { immediate: true });
const date = (value: string) => new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(value));
</script>
<template>
    <ResourceState :loading="loading" :error="error" @retry="load">
        <template v-if="data">
            <nav aria-label="Breadcrumb" class="mb-6 text-sm text-slate-400"><router-link class="text-cyan-300 hover:underline" :to="{ name: 'organizationProjects', params: { organizationId: data.org.id } }">{{ data.org.name }} / Projects</router-link><span> / {{ data.project.name }}</span></nav>
            <h1 class="text-3xl font-semibold">{{ data.project.name }}</h1>
            <p class="mt-3 break-all text-slate-400">{{ data.org.slug }} / {{ data.project.slug }}</p>
            <ProjectSectionNav :project="data.project" />
            <section class="workspace-panel mt-8"><h2 class="text-lg font-semibold">Project overview</h2>
                <p v-if="data.project.description" class="mt-4 whitespace-pre-wrap break-words text-slate-300">{{ data.project.description }}</p>
                <p v-else class="mt-4 text-slate-400">No description provided.</p>
                <dl class="mt-6 grid gap-6 sm:grid-cols-2">
                    <div><dt class="text-sm text-slate-400">Your project role</dt><dd class="mt-1">{{ data.project.currentUserRole }}</dd></div>
                    <div><dt class="text-sm text-slate-400">Status</dt><dd class="mt-1">{{ data.project.status }}</dd></div>
                    <div><dt class="text-sm text-slate-400">Visibility</dt><dd class="mt-1">{{ data.project.visibility }}</dd></div>
                    <div><dt class="text-sm text-slate-400">Created</dt><dd class="mt-1">{{ date(data.project.createdAt) }}</dd></div>
                    <div><dt class="text-sm text-slate-400">Updated</dt><dd class="mt-1">{{ date(data.project.updatedAt) }}</dd></div>
                    <div><dt class="text-sm text-slate-400">Metadata version</dt><dd class="mt-1">{{ data.project.version }}</dd></div>
                </dl>
            </section>
        </template>
    </ResourceState>
</template>
