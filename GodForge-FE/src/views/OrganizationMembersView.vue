<script setup lang="ts">
import { computed, onScopeDispose, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { organizationsApi } from '@/api/workspace/organizations.api';
import type { OrganizationMember, UpdateOrganizationMemberInput } from '@/api/workspace/workspace.models';
import {
    canInviteMembers,
    canManageMember,
    extractErrorCode,
    notifyWorkspaceChanged,
    useResource,
    workspaceError,
} from '@/features/workspace/workspaceState';
import { useAuthStore } from '@/stores/auth.store';
import OrganizationSectionNav from '@/components/workspace/OrganizationSectionNav.vue';
import PageControls from '@/components/workspace/PageControls.vue';
import ResourceState from '@/components/workspace/ResourceState.vue';

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();

const id = computed(() => String(route.params.organizationId));
const page = ref(1);
const roleFilter = ref('');
const statusFilter = ref('');
const searchInput = ref('');
const appliedSearch = ref('');

const actionError = ref('');
const actionSuccess = ref('');
const busyUserId = ref('');

interface PendingEdit {
    userId: string;
    displayName: string;
    email: string;
    role: UpdateOrganizationMemberInput['role'];
    status: UpdateOrganizationMemberInput['status'];
    version: number;
}

interface PendingRemoval {
    userId: string;
    displayName: string;
    email: string;
    isSelf: boolean;
}

const editingMember = ref<PendingEdit | null>(null);
const confirmingEdit = ref(false);
const confirmingRemoval = ref<PendingRemoval | null>(null);

let activeTenantEpoch = 0;
let disposed = false;

onScopeDispose(() => {
    disposed = true;
    activeTenantEpoch++;
});

function isCurrentTenant(orgId: string, epoch: number): boolean {
    return !disposed && epoch === activeTenantEpoch && id.value === orgId;
}

const { data, loading, error, load } = useResource(async () => {
    const organizationId = id.value;
    const requestedPage = page.value;
    const requestedRole = roleFilter.value;
    const requestedStatus = statusFilter.value;
    const requestedSearch = appliedSearch.value;
    const [orgRes, membersRes] = await Promise.all([
        organizationsApi.get(organizationId),
        organizationsApi.listMembers(organizationId, {
            page: requestedPage,
            pageSize: 20,
            role: requestedRole || undefined,
            status: requestedStatus || undefined,
            search: requestedSearch || undefined,
        }),
    ]);
    return { org: orgRes.data, members: membersRes };
});

function resetTransientState() {
    actionError.value = '';
    actionSuccess.value = '';
    busyUserId.value = '';
    editingMember.value = null;
    confirmingEdit.value = false;
    confirmingRemoval.value = null;
}

watch(
    id,
    () => {
        activeTenantEpoch++;
        page.value = 1;
        roleFilter.value = '';
        statusFilter.value = '';
        searchInput.value = '';
        appliedSearch.value = '';
        resetTransientState();
        void load();
    },
    { immediate: true }
);

watch([page, roleFilter, statusFilter, appliedSearch], () => {
    void load();
});

function applySearch() {
    page.value = 1;
    appliedSearch.value = searchInput.value.trim();
}

function clearFilters() {
    page.value = 1;
    roleFilter.value = '';
    statusFilter.value = '';
    searchInput.value = '';
    appliedSearch.value = '';
}

function allowedRolesForActor(actorRole: string): UpdateOrganizationMemberInput['role'][] {
    if (actorRole === 'organizationOwner') {
        return ['organizationOwner', 'organizationAdmin', 'organizationMember'];
    }
    return ['organizationMember'];
}

function canEditRow(member: OrganizationMember): boolean {
    if (!data.value || member.status === 'removed') return false;
    return canManageMember(data.value.org.currentUserRole, data.value.org.status, member.role);
}

function canRemoveRow(member: OrganizationMember): boolean {
    if (!data.value || data.value.org.status !== 'active' || member.status === 'removed') {
        return false;
    }
    const isSelf = auth.user?.id === member.userId;
    if (isSelf) {
        return true;
    }
    if (!['organizationOwner', 'organizationAdmin'].includes(data.value.org.currentUserRole)) {
        return false;
    }
    return canManageMember(data.value.org.currentUserRole, data.value.org.status, member.role);
}

function startEdit(member: OrganizationMember) {
    actionError.value = '';
    actionSuccess.value = '';
    confirmingRemoval.value = null;
    confirmingEdit.value = false;
    editingMember.value = {
        userId: member.userId,
        displayName: member.displayName,
        email: member.email,
        role: (member.role as UpdateOrganizationMemberInput['role']) || 'organizationMember',
        status: member.status === 'suspended' ? 'suspended' : 'active',
        version: member.version,
    };
}

function cancelEdit() {
    editingMember.value = null;
    confirmingEdit.value = false;
}

function requestEditConfirmation() {
    if (!editingMember.value) return;
    confirmingEdit.value = true;
}

async function submitEdit() {
    if (!editingMember.value || !data.value) return;
    const currentEdit = editingMember.value;
    const organizationId = id.value;
    const mutationEpoch = activeTenantEpoch;
    busyUserId.value = currentEdit.userId;
    actionError.value = '';
    actionSuccess.value = '';
    try {
        await organizationsApi.updateMember(organizationId, currentEdit.userId, {
            role: currentEdit.role,
            status: currentEdit.status,
            version: currentEdit.version,
        });
        if (!isCurrentTenant(organizationId, mutationEpoch)) return;
        editingMember.value = null;
        confirmingEdit.value = false;
        actionSuccess.value = `Updated membership for ${currentEdit.displayName}.`;
        notifyWorkspaceChanged();
        await load();
    } catch (cause) {
        if (!isCurrentTenant(organizationId, mutationEpoch)) return;
        actionError.value = workspaceError(cause);
        const code = extractErrorCode(cause);
        if (code === 'CONCURRENCY_CONFLICT' || code === 'LAST_OWNER_REQUIRED' || code === 'MEMBERSHIP_NOT_FOUND') {
            editingMember.value = null;
            confirmingEdit.value = false;
            await load();
        }
    } finally {
        if (isCurrentTenant(organizationId, mutationEpoch)) {
            busyUserId.value = '';
        }
    }
}

function startRemoval(member: OrganizationMember) {
    actionError.value = '';
    actionSuccess.value = '';
    editingMember.value = null;
    confirmingEdit.value = false;
    confirmingRemoval.value = {
        userId: member.userId,
        displayName: member.displayName,
        email: member.email,
        isSelf: auth.user?.id === member.userId,
    };
}

async function confirmRemoveMember() {
    if (!confirmingRemoval.value) return;
    const target = confirmingRemoval.value;
    const organizationId = id.value;
    const mutationEpoch = activeTenantEpoch;
    busyUserId.value = target.userId;
    actionError.value = '';
    actionSuccess.value = '';
    try {
        await organizationsApi.removeMember(organizationId, target.userId);
        if (!isCurrentTenant(organizationId, mutationEpoch)) return;
        confirmingRemoval.value = null;
        notifyWorkspaceChanged();
        if (target.isSelf) {
            await router.replace({ name: 'dashboard' });
            return;
        }
        actionSuccess.value = `Removed ${target.displayName} from the organization.`;
        await load();
    } catch (cause) {
        if (!isCurrentTenant(organizationId, mutationEpoch)) return;
        actionError.value = workspaceError(cause);
        confirmingRemoval.value = null;
        await load();
    } finally {
        if (isCurrentTenant(organizationId, mutationEpoch)) {
            busyUserId.value = '';
        }
    }
}

const formatDate = (value: string) =>
    new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(value));
</script>

<template>
    <ResourceState :loading="loading" :error="error" @retry="load">
        <template v-if="data">
            <div class="mb-6 flex flex-wrap items-start justify-between gap-4">
                <div>
                    <p class="text-sm text-cyan-400">{{ data.org.name }} / {{ data.org.slug }}</p>
                    <h1 class="mt-2 text-3xl font-semibold">Members</h1>
                    <p class="mt-3 text-sm text-slate-400">{{ data.org.currentUserRole }} · {{ data.org.status }}</p>
                </div>
                <router-link
                    v-if="canInviteMembers(data.org.currentUserRole, data.org.status)"
                    class="workspace-button text-cyan-300"
                    :to="{ name: 'organizationInvitations', params: { organizationId: data.org.id } }"
                >
                    Invite members
                </router-link>
            </div>

            <OrganizationSectionNav :organization="data.org" />

            <div
                v-if="actionError"
                role="alert"
                class="workspace-panel mb-6 border-rose-500/40 bg-rose-950/30 text-rose-200"
            >
                <p>{{ actionError }}</p>
            </div>

            <div
                v-if="actionSuccess"
                role="status"
                class="workspace-panel mb-6 border-emerald-500/40 bg-emerald-950/30 text-emerald-200"
            >
                <p>{{ actionSuccess }}</p>
            </div>

            <form
                aria-label="Filter organization members"
                class="workspace-panel mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4"
                @submit.prevent="applySearch"
            >
                <div>
                    <label for="member-search" class="text-sm text-slate-300">Search members</label>
                    <input
                        id="member-search"
                        v-model="searchInput"
                        type="search"
                        maxlength="200"
                        placeholder="Name or email"
                        class="workspace-input"
                    />
                </div>
                <div>
                    <label for="member-role-filter" class="text-sm text-slate-300">Role filter</label>
                    <select
                        id="member-role-filter"
                        v-model="roleFilter"
                        class="workspace-input"
                        @change="page = 1"
                    >
                        <option value="">All roles</option>
                        <option value="organizationOwner">organizationOwner</option>
                        <option value="organizationAdmin">organizationAdmin</option>
                        <option value="organizationMember">organizationMember</option>
                    </select>
                </div>
                <div>
                    <label for="member-status-filter" class="text-sm text-slate-300">Status filter</label>
                    <select
                        id="member-status-filter"
                        v-model="statusFilter"
                        class="workspace-input"
                        @change="page = 1"
                    >
                        <option value="">Active &amp; suspended</option>
                        <option value="active">active</option>
                        <option value="suspended">suspended</option>
                        <option value="removed">removed</option>
                    </select>
                </div>
                <div class="flex items-end gap-2">
                    <button type="submit" class="workspace-button">Search</button>
                    <button
                        v-if="roleFilter || statusFilter || appliedSearch || searchInput"
                        type="button"
                        class="workspace-button"
                        @click="clearFilters"
                    >
                        Clear filters
                    </button>
                </div>
            </form>

            <section
                v-if="confirmingRemoval"
                role="dialog"
                aria-modal="false"
                aria-label="Confirm member removal"
                class="workspace-panel mb-6 border-rose-500/40 bg-rose-950/20"
            >
                <h2 class="text-lg font-semibold text-rose-200">
                    {{ confirmingRemoval.isSelf ? 'Leave organization' : 'Remove organization member' }}
                </h2>
                <p class="mt-2 text-sm text-slate-300">
                    <template v-if="confirmingRemoval.isSelf">
                        Are you sure you want to leave <strong>{{ data.org.name }}</strong>? Your organization and project memberships in this organization will be revoked immediately.
                    </template>
                    <template v-else>
                        Remove <strong>{{ confirmingRemoval.displayName }}</strong> ({{ confirmingRemoval.email }}) from <strong>{{ data.org.name }}</strong>? All project memberships in this organization will be revoked in the same transaction.
                    </template>
                </p>
                <div class="mt-4 flex flex-wrap gap-3">
                    <button
                        type="button"
                        class="workspace-button border-rose-500/50 text-rose-200 hover:bg-rose-900/40"
                        :disabled="busyUserId === confirmingRemoval.userId"
                        @click="confirmRemoveMember"
                    >
                        {{ confirmingRemoval.isSelf ? 'Confirm leave' : 'Confirm removal' }}
                    </button>
                    <button
                        type="button"
                        class="workspace-button"
                        :disabled="busyUserId === confirmingRemoval.userId"
                        @click="confirmingRemoval = null"
                    >
                        Cancel
                    </button>
                </div>
            </section>

            <div v-if="!data.members.data.length" class="workspace-panel">
                <h2 class="text-xl font-semibold">No matching members</h2>
                <p class="mt-2 text-sm text-slate-400">
                    No organization members matched the current filters or page.
                </p>
            </div>

            <ul v-else aria-label="Organization members" class="space-y-4">
                <li
                    v-for="member in data.members.data"
                    :key="member.userId"
                    class="workspace-panel"
                >
                    <div class="flex flex-wrap items-start justify-between gap-4">
                        <div>
                            <h2 class="text-lg font-semibold">
                                {{ member.displayName }}
                                <span
                                    v-if="auth.user?.id === member.userId"
                                    class="ml-2 rounded border border-cyan-500/40 bg-cyan-950/40 px-2 py-0.5 text-xs text-cyan-300"
                                >
                                    You
                                </span>
                            </h2>
                            <p class="mt-1 break-all text-sm text-slate-400">{{ member.email }}</p>
                            <p class="mt-2 text-sm">
                                <span class="font-medium text-cyan-300">{{ member.role }}</span>
                                ·
                                <span>{{ member.status }}</span>
                                ·
                                <span class="text-slate-400">Joined {{ formatDate(member.joinedAt) }}</span>
                                ·
                                <span class="text-slate-500">v{{ member.version }}</span>
                            </p>
                            <p
                                v-if="member.role === 'organizationOwner' && member.status === 'active'"
                                class="mt-2 text-xs text-slate-400"
                            >
                                Organization owner — at least one active owner must remain in the organization.
                            </p>
                        </div>

                        <div class="flex flex-wrap gap-2">
                            <button
                                v-if="canEditRow(member) && editingMember?.userId !== member.userId"
                                type="button"
                                class="workspace-button"
                                :disabled="Boolean(busyUserId)"
                                @click="startEdit(member)"
                            >
                                Manage role
                            </button>
                            <button
                                v-if="canRemoveRow(member)"
                                type="button"
                                class="workspace-button text-rose-300"
                                :disabled="Boolean(busyUserId)"
                                @click="startRemoval(member)"
                            >
                                {{ auth.user?.id === member.userId ? 'Leave organization' : 'Remove member' }}
                            </button>
                        </div>
                    </div>

                    <div
                        v-if="editingMember && editingMember.userId === member.userId"
                        class="mt-4 border-t border-slate-800 pt-4"
                    >
                        <div class="grid gap-4 sm:grid-cols-2">
                            <div>
                                <label :for="`role-${member.userId}`" class="text-sm text-slate-300">Organization role</label>
                                <select
                                    :id="`role-${member.userId}`"
                                    v-model="editingMember.role"
                                    class="workspace-input"
                                    :disabled="busyUserId === member.userId"
                                    @change="confirmingEdit = false"
                                >
                                    <option
                                        v-for="roleOption in allowedRolesForActor(data.org.currentUserRole)"
                                        :key="roleOption"
                                        :value="roleOption"
                                    >
                                        {{ roleOption }}
                                    </option>
                                </select>
                            </div>
                            <div>
                                <label :for="`status-${member.userId}`" class="text-sm text-slate-300">Membership status</label>
                                <select
                                    :id="`status-${member.userId}`"
                                    v-model="editingMember.status"
                                    class="workspace-input"
                                    :disabled="busyUserId === member.userId"
                                    @change="confirmingEdit = false"
                                >
                                    <option value="active">active</option>
                                    <option value="suspended">suspended</option>
                                </select>
                            </div>
                        </div>

                        <div v-if="!confirmingEdit" class="mt-4 flex flex-wrap gap-3">
                            <button
                                type="button"
                                class="workspace-button text-cyan-300"
                                :disabled="busyUserId === member.userId"
                                @click="requestEditConfirmation"
                            >
                                Review change
                            </button>
                            <button
                                type="button"
                                class="workspace-button"
                                :disabled="busyUserId === member.userId"
                                @click="cancelEdit"
                            >
                                Cancel
                            </button>
                        </div>

                        <div
                            v-else
                            role="region"
                            aria-label="Confirm membership update"
                            class="mt-4 rounded-xl border border-cyan-500/30 bg-slate-950/60 p-4"
                        >
                            <p class="text-sm text-slate-200">
                                Confirm updating <strong>{{ member.displayName }}</strong> to role
                                <strong>{{ editingMember.role }}</strong> and status
                                <strong>{{ editingMember.status }}</strong> (version {{ editingMember.version }})?
                            </p>
                            <div class="mt-3 flex flex-wrap gap-3">
                                <button
                                    type="button"
                                    class="workspace-button text-cyan-300"
                                    :disabled="busyUserId === member.userId"
                                    @click="submitEdit"
                                >
                                    {{ busyUserId === member.userId ? 'Saving…' : 'Confirm update' }}
                                </button>
                                <button
                                    type="button"
                                    class="workspace-button"
                                    :disabled="busyUserId === member.userId"
                                    @click="cancelEdit"
                                >
                                    Cancel
                                </button>
                            </div>
                        </div>
                    </div>
                </li>
            </ul>

            <PageControls
                :page="page"
                :total="data.members.meta.totalCount"
                @change="page = $event"
            />
        </template>
    </ResourceState>
</template>
