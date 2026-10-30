<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { projectsApi } from '@/api/workspace/projects.api';
import type { OrganizationMember, ProjectMember, ProjectRole } from '@/api/workspace/workspace.models';
import { projectRoles, useProjectWorkspace } from '@/features/workspace/projectWorkspace';
import { useResource } from '@/features/workspace/workspaceState';
import { useAuthStore } from '@/stores/auth.store';
import ProjectSectionNav from '@/components/workspace/ProjectSectionNav.vue';
import ProjectMemberPicker from '@/components/workspace/ProjectMemberPicker.vue';
import PageControls from '@/components/workspace/PageControls.vue';
import ResourceState from '@/components/workspace/ResourceState.vue';
import WorkspaceActionPanel from '@/components/workspace/WorkspaceActionPanel.vue';

const { data, loading, error, load, id, identity, busy, actionError, success, manager, owner, mutable, mutate, captureContext } = useProjectWorkspace();
const auth = useAuthStore();
const router = useRouter();
const page = ref(1);
const search = ref('');
const applied = ref('');
const role = ref('');
const status = ref('');
const adding = ref(false);
const candidate = ref<OrganizationMember>();
const newRole = ref<ProjectRole>('developer');
const editing = ref<ProjectMember>();
const editedRole = ref<ProjectRole>('developer');
const removing = ref<ProjectMember>();
let addKey = '';
const allowedRoles = computed(() => projectRoles.filter(value => owner.value || value !== 'projectOwner'));
const members = useResource(async () => {
    return projectsApi.listMembers(id.value, { page: page.value, pageSize: 20, search: applied.value || undefined, role: role.value || undefined, status: status.value || undefined });
});
const { data: directory, loading: membersLoading, error: membersError, load: loadMembers } = members;
watch(identity, () => {
    page.value = 1; search.value = ''; applied.value = ''; role.value = ''; status.value = '';
    adding.value = false; candidate.value = undefined; editing.value = undefined; removing.value = undefined; addKey = '';
});
watch([() => data.value?.project.id, page, applied, role, status], () => { if (data.value) void loadMembers(); });
watch([candidate, newRole], () => { addKey = ''; });
function canManage(member: ProjectMember) {
    return mutable.value && manager.value && member.status === 'active' && (owner.value || member.role !== 'projectOwner');
}
function edit(member: ProjectMember) { editing.value = { ...member }; editedRole.value = member.role; }
async function add() {
    if (!candidate.value || !mutable.value || !manager.value) return;
    addKey ||= crypto.randomUUID();
    const input = { userId: candidate.value.userId, role: newRole.value };
    await mutate(projectId => projectsApi.addMember(projectId, input, addKey), 'Project member added.', async () => { adding.value = false; candidate.value = undefined; await loadMembers(); });
}
async function update() {
    if (!editing.value) return;
    const isCurrent = captureContext();
    const member = editing.value;
    await mutate(projectId => projectsApi.updateMember(projectId, member.userId, { role: editedRole.value, version: member.version }), 'Project role updated.', async () => { editing.value = undefined; await loadMembers(); });
    // A conflict requires selecting the freshly loaded member before another submission.
    if (isCurrent() && actionError.value) { editing.value = undefined; await loadMembers(); }
}
async function remove() {
    if (!removing.value) return;
    const member = removing.value;
    const self = member.userId === auth.user?.id;
    await mutate(projectId => projectsApi.removeMember(projectId, member.userId), self ? 'You left the project.' : 'Project member removed. Organization membership is retained.', async () => {
        removing.value = undefined;
        if (self) await router.replace({ name: 'organizationProjects', params: { organizationId: data.value?.org.id ?? String(router.currentRoute.value.params.organizationId) } });
        else await loadMembers();
    });
}
</script>
<template>
    <ResourceState :loading="loading" :error="error" @retry="load">
        <template v-if="data">
            <nav aria-label="Breadcrumb" class="text-sm text-cyan-300"><router-link :to="{ name: 'organizationProjects', params: { organizationId: data.org.id } }">{{ data.org.name }} / Projects</router-link></nav>
            <h1 class="mt-4 break-words text-3xl font-semibold">{{ data.project.name }} · Members</h1>
            <ProjectSectionNav :project="data.project" />
            <p class="mb-4 text-slate-400">Project status: {{ data.project.status }}. Project access requires active organization membership.</p>
            <button v-if="manager && mutable" class="workspace-button" :disabled="busy" @click="adding = !adding; candidate = undefined">Add member</button>
            <p v-if="actionError" role="alert" class="my-4 text-rose-300">{{ actionError }}</p>
            <p v-if="success" role="status" class="my-4 text-cyan-300">{{ success }}</p>
            <WorkspaceActionPanel v-if="adding && manager && mutable" aria-label="Add project member">
                <h2 class="text-xl font-semibold">Add organization member</h2>
                <p class="my-3 text-slate-400">Select an active organization member. External collaborators must first accept an <router-link class="text-cyan-300 underline" :to="{ name: 'organizationOverview', params: { organizationId: data.org.id } }">organization invitation</router-link>.</p>
                <ProjectMemberPicker :key="identity" :organization-id="data.org.id" :project-id="id" source="organization" @select="candidate = $event" />
                <form v-if="candidate" class="space-y-4" @submit.prevent="add">
                    <p>Selected: {{ candidate.displayName }} · {{ candidate.email }}</p>
                    <label class="block">New member role<select v-model="newRole" class="workspace-input"><option v-for="value in allowedRoles" :key="value" :value="value">{{ value }}</option></select></label>
                    <button class="workspace-button" :disabled="busy">Confirm add member</button>
                </form>
                <button class="workspace-button mt-3" :disabled="busy" @click="adding = false">Cancel adding</button>
            </WorkspaceActionPanel>
            <form class="my-6 flex flex-wrap items-end gap-3" @submit.prevent="page = 1; applied = search.trim()">
                <label class="grow">Search members<input v-model="search" class="workspace-input" type="search" /></label>
                <label>Role filter<select v-model="role" class="workspace-input" @change="page = 1"><option value="">All roles</option><option v-for="value in projectRoles" :key="value">{{ value }}</option></select></label>
                <label>Status filter<select v-model="status" class="workspace-input" @change="page = 1"><option value="">All statuses</option><option>active</option><option>suspended</option><option>removed</option></select></label>
                <button class="workspace-button">Search</button>
            </form>
            <ResourceState :loading="membersLoading" :error="membersError" @retry="loadMembers">
                <p v-if="directory && !directory.data.length" class="workspace-panel">No project members match these filters.</p>
                <ul v-if="directory" aria-label="Project members" class="space-y-3">
                    <li v-for="member in directory.data" :key="member.userId" class="workspace-panel flex flex-wrap items-center justify-between gap-4">
                        <div class="min-w-0 break-words"><h2 class="font-semibold">{{ member.displayName }}</h2><p class="break-all text-sm text-slate-400">{{ member.email }}</p><p class="mt-2">{{ member.role }} · {{ member.status }}</p><p class="text-sm text-slate-400">Joined {{ new Date(member.joinedAt).toLocaleDateString() }} · Membership version {{ member.version }}</p></div>
                        <div class="flex flex-wrap gap-2">
                            <button v-if="canManage(member)" class="workspace-button" :disabled="busy" @click="edit(member)">Change role</button>
                            <button v-if="canManage(member) || (mutable && member.status === 'active' && member.userId === auth.user?.id)" class="workspace-button" :disabled="busy" @click="removing = member">{{ member.userId === auth.user?.id ? 'Leave project' : 'Remove member' }}</button>
                        </div>
                    </li>
                </ul>
                <PageControls v-if="directory" :page="page" :total="directory.meta.totalCount" @change="page = $event" />
            </ResourceState>
            <WorkspaceActionPanel v-if="editing" aria-label="Confirm role change">
                <h2 class="text-xl font-semibold">Change role for {{ editing.displayName }}</h2>
                <form class="mt-4 space-y-4" @submit.prevent="update"><label for="project-member-role" class="block">Project role</label><select id="project-member-role" v-model="editedRole" class="workspace-input"><option v-for="value in allowedRoles" :key="value">{{ value }}</option></select><p>Confirm this change to project authority. The server protects the last active owner.</p><button class="workspace-button" :disabled="busy">Confirm role change</button><button type="button" class="workspace-button ml-3" :disabled="busy" @click="editing = undefined">Cancel</button></form>
            </WorkspaceActionPanel>
            <WorkspaceActionPanel v-if="removing" aria-label="Confirm member removal">
                <h2 class="text-xl font-semibold">Remove {{ removing.displayName }} from this project?</h2><p class="my-4">Project access is revoked immediately. Organization membership remains. The last active owner must transfer ownership first.</p><button class="workspace-button" :disabled="busy" @click="remove">Confirm removal</button><button class="workspace-button ml-3" :disabled="busy" @click="removing = undefined">Cancel</button>
            </WorkspaceActionPanel>
        </template>
    </ResourceState>
</template>
