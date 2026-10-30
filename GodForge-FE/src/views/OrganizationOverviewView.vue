<script setup lang="ts">
import { computed, watch } from 'vue';
import { useRoute } from 'vue-router';
import { organizationsApi } from '@/api/workspace/organizations.api';
import {
    canInviteMembers,
    canManageOrganization,
    useResource,
} from '@/features/workspace/workspaceState';
import OrganizationSectionNav from '@/components/workspace/OrganizationSectionNav.vue';
import ResourceState from '@/components/workspace/ResourceState.vue';

const route = useRoute();
const id = computed(() => String(route.params.organizationId));

const { data, loading, error, load } = useResource(async () => {
    const response = await organizationsApi.get(id.value);
    return response.data;
});

watch(id, load, { immediate: true });

const formatDate = (value: string) =>
    new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(value));
</script>

<template>
    <ResourceState :loading="loading" :error="error" @retry="load">
        <template v-if="data">
            <div class="mb-6">
                <p class="text-sm text-cyan-400">{{ data.name }} / {{ data.slug }}</p>
                <h1 class="mt-2 text-3xl font-semibold">{{ data.name }}</h1>
                <p class="mt-3 text-sm text-slate-400">{{ data.currentUserRole }} · {{ data.status }}</p>
            </div>

            <OrganizationSectionNav :organization="data" />

            <div
                v-if="data.status === 'deleting'"
                role="status"
                class="workspace-panel mb-6 border-amber-500/40 bg-amber-950/20 text-amber-200"
            >
                <p class="font-semibold">Deletion requested</p>
                <p class="mt-1 text-sm text-amber-200/80">
                    This organization is in the retained <code>deleting</code> lifecycle state and is read-only.
                </p>
            </div>

            <section class="workspace-panel mb-6" aria-label="Organization details">
                <h2 class="text-lg font-semibold">Organization overview</h2>
                <dl class="mt-4 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
                    <div>
                        <dt class="text-sm text-slate-400">Organization name</dt>
                        <dd class="mt-1 font-medium">{{ data.name }}</dd>
                    </div>
                    <div>
                        <dt class="text-sm text-slate-400">Slug</dt>
                        <dd class="mt-1 break-all font-mono text-sm">{{ data.slug }}</dd>
                    </div>
                    <div>
                        <dt class="text-sm text-slate-400">Your organization role</dt>
                        <dd class="mt-1">{{ data.currentUserRole }}</dd>
                    </div>
                    <div>
                        <dt class="text-sm text-slate-400">Lifecycle status</dt>
                        <dd class="mt-1">{{ data.status }}</dd>
                    </div>
                    <div>
                        <dt class="text-sm text-slate-400">Created</dt>
                        <dd class="mt-1">{{ formatDate(data.createdAt) }}</dd>
                    </div>
                    <div>
                        <dt class="text-sm text-slate-400">Updated</dt>
                        <dd class="mt-1">{{ formatDate(data.updatedAt) }}</dd>
                    </div>
                    <div>
                        <dt class="text-sm text-slate-400">Metadata version</dt>
                        <dd class="mt-1">{{ data.version }}</dd>
                    </div>
                </dl>
            </section>

            <section aria-label="Organization workspace sections" class="grid gap-4 sm:grid-cols-2">
                <div class="workspace-panel flex flex-col justify-between">
                    <div>
                        <h2 class="text-lg font-semibold">Projects</h2>
                        <p class="mt-2 text-sm text-slate-400">
                            Browse your assigned projects or manage the organization project catalog.
                        </p>
                    </div>
                    <div class="mt-4">
                        <router-link
                            class="workspace-button inline-block text-cyan-300"
                            :to="{ name: 'organizationProjects', params: { organizationId: data.id } }"
                        >
                            Open projects
                        </router-link>
                    </div>
                </div>

                <div class="workspace-panel flex flex-col justify-between">
                    <div>
                        <h2 class="text-lg font-semibold">Members</h2>
                        <p class="mt-2 text-sm text-slate-400">
                            View organization members, filter by role or status, and manage membership access.
                        </p>
                    </div>
                    <div class="mt-4">
                        <router-link
                            class="workspace-button inline-block text-cyan-300"
                            :to="{ name: 'organizationMembers', params: { organizationId: data.id } }"
                        >
                            Open members
                        </router-link>
                    </div>
                </div>

                <div
                    v-if="canInviteMembers(data.currentUserRole, data.status)"
                    class="workspace-panel flex flex-col justify-between"
                >
                    <div>
                        <h2 class="text-lg font-semibold">Invitations</h2>
                        <p class="mt-2 text-sm text-slate-400">
                            Invite collaborators by email, track pending invitations, or revoke active links.
                        </p>
                    </div>
                    <div class="mt-4">
                        <router-link
                            class="workspace-button inline-block text-cyan-300"
                            :to="{ name: 'organizationInvitations', params: { organizationId: data.id } }"
                        >
                            Manage invitations
                        </router-link>
                    </div>
                </div>

                <div
                    v-if="canManageOrganization(data.currentUserRole, data.status)"
                    class="workspace-panel flex flex-col justify-between"
                >
                    <div>
                        <h2 class="text-lg font-semibold">Settings</h2>
                        <p class="mt-2 text-sm text-slate-400">
                            Update organization profile, transfer ownership, or manage lifecycle status.
                        </p>
                    </div>
                    <div class="mt-4">
                        <router-link
                            class="workspace-button inline-block text-cyan-300"
                            :to="{ name: 'organizationSettings', params: { organizationId: data.id } }"
                        >
                            Open settings
                        </router-link>
                    </div>
                </div>
            </section>
        </template>
    </ResourceState>
</template>
