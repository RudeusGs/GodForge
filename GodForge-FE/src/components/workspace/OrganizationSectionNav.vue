<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import type { Organization } from '@/api/workspace/workspace.models';
import { canInviteMembers, canManageOrganization } from '@/features/workspace/workspaceState';

const props = defineProps<{
    organization: Organization;
}>();

const route = useRoute();

const showInvitations = computed(
    () =>
        canInviteMembers(props.organization.currentUserRole, props.organization.status) ||
        route.name === 'organizationInvitations'
);

const showSettings = computed(
    () =>
        canManageOrganization(props.organization.currentUserRole, props.organization.status) ||
        route.name === 'organizationSettings'
);

const sections = computed(() => {
    const items: Array<{ name: string; label: string }> = [
        { name: 'organizationOverview', label: 'Overview' },
        { name: 'organizationProjects', label: 'Projects' },
        { name: 'organizationMembers', label: 'Members' },
    ];
    if (showInvitations.value) {
        items.push({ name: 'organizationInvitations', label: 'Invitations' });
    }
    if (showSettings.value) {
        items.push({ name: 'organizationSettings', label: 'Settings' });
    }
    return items;
});
</script>

<template>
    <nav aria-label="Organization sections" class="mb-6 flex flex-wrap gap-2 border-b border-slate-800 pb-4">
        <router-link
            v-for="section in sections"
            :key="section.name"
            :to="{ name: section.name, params: { organizationId: organization.id } }"
            class="rounded-lg border px-3.5 py-2 text-sm font-medium transition-colors"
            :class="
                route.name === section.name
                    ? 'border-cyan-500/40 bg-slate-800 text-cyan-300'
                    : 'border-slate-800 bg-slate-900/40 text-slate-300 hover:border-slate-700 hover:bg-slate-800/70'
            "
            :aria-current="route.name === section.name ? 'page' : undefined"
        >
            {{ section.label }}
        </router-link>
    </nav>
</template>
