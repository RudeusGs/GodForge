<script setup lang="ts">
import { useRoute } from 'vue-router';
import type { Project } from '@/api/workspace/workspace.models';
import { hasProjectRole, managesProject } from '@/features/workspace/projectWorkspace';
defineProps<{ project: Project }>();
const route = useRoute();
</script>
<template>
    <nav aria-label="Project sections" class="my-6 flex flex-wrap gap-3 border-b border-slate-800 pb-4">
        <router-link class="workspace-button" :aria-current="route.name === 'projectDetail' ? 'page' : undefined" :to="{ name: 'projectDetail', params: { organizationId: project.organizationId, projectId: project.id } }">Overview</router-link>
        <router-link v-if="hasProjectRole(project.currentUserRole)" class="workspace-button" :aria-current="route.name === 'projectMembers' ? 'page' : undefined" :to="{ name: 'projectMembers', params: { organizationId: project.organizationId, projectId: project.id } }">Members</router-link>
        <router-link v-if="managesProject(project.currentUserRole)" class="workspace-button" :aria-current="route.name === 'projectSettings' ? 'page' : undefined" :to="{ name: 'projectSettings', params: { organizationId: project.organizationId, projectId: project.id } }">Settings</router-link>
    </nav>
</template>
