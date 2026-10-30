<script setup lang="ts">
import { computed, onScopeDispose, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { organizationsApi } from '@/api/workspace/organizations.api';
import type {
    CreateOrganizationInvitationInput,
    OrganizationInvitation,
} from '@/api/workspace/workspace.models';
import {
    canInviteAdmin,
    canInviteMembers,
    useResource,
    workspaceError,
} from '@/features/workspace/workspaceState';
import OrganizationSectionNav from '@/components/workspace/OrganizationSectionNav.vue';
import PageControls from '@/components/workspace/PageControls.vue';
import ResourceState from '@/components/workspace/ResourceState.vue';

const route = useRoute();
const id = computed(() => String(route.params.organizationId));

const page = ref(1);
const statusFilter = ref('');
const emailFilterInput = ref('');
const appliedEmailFilter = ref('');

const inviteEmail = ref('');
const inviteRole = ref<CreateOrganizationInvitationInput['role']>('organizationMember');
const inviteFieldError = ref('');
const submitError = ref('');
const submitSuccess = ref('');
const submitting = ref(false);
let idempotencyKey = crypto.randomUUID();

const confirmingRevoke = ref<OrganizationInvitation | null>(null);
const revokingId = ref('');
const revokeError = ref('');

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
    const requestedStatus = statusFilter.value;
    const requestedEmail = appliedEmailFilter.value;

    const org = (await organizationsApi.get(organizationId)).data;
    if (!['organizationOwner', 'organizationAdmin'].includes(org.currentUserRole)) {
        return {
            org,
            forbidden: true,
            invitations: {
                data: [] as OrganizationInvitation[],
                meta: { page: 1, pageSize: 20, totalCount: 0 },
            },
        };
    }

    const invitations = await organizationsApi.listInvitations(organizationId, {
        page: requestedPage,
        pageSize: 20,
        status: requestedStatus || undefined,
        email: requestedEmail || undefined,
    });
    return { org, forbidden: false, invitations };
});

function resetTransient() {
    inviteEmail.value = '';
    inviteRole.value = 'organizationMember';
    inviteFieldError.value = '';
    submitError.value = '';
    submitSuccess.value = '';
    submitting.value = false;
    confirmingRevoke.value = null;
    revokingId.value = '';
    revokeError.value = '';
    idempotencyKey = crypto.randomUUID();
}

watch(
    id,
    () => {
        activeTenantEpoch++;
        page.value = 1;
        statusFilter.value = '';
        emailFilterInput.value = '';
        appliedEmailFilter.value = '';
        resetTransient();
        void load();
    },
    { immediate: true }
);

watch([page, statusFilter, appliedEmailFilter], () => {
    void load();
});

watch([inviteEmail, inviteRole], () => {
    inviteFieldError.value = '';
    idempotencyKey = crypto.randomUUID();
});

function applyEmailFilter() {
    page.value = 1;
    appliedEmailFilter.value = emailFilterInput.value.trim();
}

function clearFilters() {
    page.value = 1;
    statusFilter.value = '';
    emailFilterInput.value = '';
    appliedEmailFilter.value = '';
}

function validateInvitationForm(): boolean {
    const normalized = inviteEmail.value.trim();
    if (!normalized) {
        inviteFieldError.value = 'Enter an email address to invite.';
        return false;
    }
    if (!/^\S+@\S+\.\S+$/.test(normalized)) {
        inviteFieldError.value = 'Enter a valid email address.';
        return false;
    }
    inviteFieldError.value = '';
    return true;
}

async function submitInvitation() {
    if (submitting.value || !data.value) return;
    submitError.value = '';
    submitSuccess.value = '';
    if (!validateInvitationForm()) return;

    submitting.value = true;
    const targetEmail = inviteEmail.value.trim();
    const targetRole = inviteRole.value;
    const organizationId = id.value;
    const mutationEpoch = activeTenantEpoch;

    try {
        await organizationsApi.createInvitation(
            organizationId,
            { email: targetEmail, role: targetRole },
            idempotencyKey
        );
        if (!isCurrentTenant(organizationId, mutationEpoch)) return;
        inviteEmail.value = '';
        inviteRole.value = 'organizationMember';
        idempotencyKey = crypto.randomUUID();
        submitSuccess.value = `Invitation sent to ${targetEmail} (${targetRole}).`;
        await load();
    } catch (cause) {
        if (!isCurrentTenant(organizationId, mutationEpoch)) return;
        submitError.value = workspaceError(cause);
    } finally {
        if (isCurrentTenant(organizationId, mutationEpoch)) {
            submitting.value = false;
        }
    }
}

function promptRevoke(invitation: OrganizationInvitation) {
    revokeError.value = '';
    submitSuccess.value = '';
    confirmingRevoke.value = invitation;
}

async function confirmRevokeInvitation() {
    if (!confirmingRevoke.value) return;
    const target = confirmingRevoke.value;
    const organizationId = id.value;
    const mutationEpoch = activeTenantEpoch;
    revokingId.value = target.id;
    revokeError.value = '';
    submitSuccess.value = '';

    try {
        await organizationsApi.revokeInvitation(organizationId, target.id);
        if (!isCurrentTenant(organizationId, mutationEpoch)) return;
        confirmingRevoke.value = null;
        submitSuccess.value = `Revoked invitation for ${target.email}.`;
        await load();
    } catch (cause) {
        if (!isCurrentTenant(organizationId, mutationEpoch)) return;
        revokeError.value = workspaceError(cause);
        confirmingRevoke.value = null;
        await load();
    } finally {
        if (isCurrentTenant(organizationId, mutationEpoch)) {
            revokingId.value = '';
        }
    }
}

const formatDate = (value: string) =>
    new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(value));
</script>

<template>
    <ResourceState :loading="loading" :error="error" @retry="load">
        <template v-if="data">
            <div class="mb-6">
                <p class="text-sm text-cyan-400">{{ data.org.name }} / {{ data.org.slug }}</p>
                <h1 class="mt-2 text-3xl font-semibold">Invitations</h1>
                <p class="mt-3 text-sm text-slate-400">{{ data.org.currentUserRole }} · {{ data.org.status }}</p>
            </div>

            <OrganizationSectionNav :organization="data.org" />

            <div
                v-if="data.forbidden"
                role="alert"
                class="workspace-panel border-amber-500/40 bg-amber-950/20 text-amber-200"
            >
                <h2 class="text-lg font-semibold">Permission required</h2>
                <p class="mt-2 text-sm text-slate-300">
                    Only an OrganizationOwner or OrganizationAdmin can view and manage organization invitations.
                </p>
            </div>

            <div v-else>
                <section
                    v-if="canInviteMembers(data.org.currentUserRole, data.org.status)"
                    aria-label="Invite organization member"
                    class="workspace-panel mb-6"
                >
                    <h2 class="text-lg font-semibold">Send organization invitation</h2>
                    <p class="mt-2 text-sm text-slate-400">
                        Invitations expire after 7 days and must be accepted by a verified account with the invited email address.
                    </p>

                    <div
                        v-if="submitError"
                        role="alert"
                        class="mt-4 rounded-xl border border-rose-500/40 bg-rose-950/30 p-3 text-sm text-rose-200"
                    >
                        {{ submitError }}
                    </div>

                    <div
                        v-if="submitSuccess"
                        role="status"
                        class="mt-4 rounded-xl border border-emerald-500/40 bg-emerald-950/30 p-3 text-sm text-emerald-200"
                    >
                        {{ submitSuccess }}
                    </div>

                    <form class="mt-4 grid gap-4 sm:grid-cols-3" novalidate @submit.prevent="submitInvitation">
                        <div class="sm:col-span-2">
                            <label for="invite-email" class="text-sm text-slate-300">Invitee email</label>
                            <input
                                id="invite-email"
                                v-model="inviteEmail"
                                type="email"
                                maxlength="254"
                                placeholder="collaborator@example.com"
                                class="workspace-input"
                                :disabled="submitting"
                                :aria-invalid="Boolean(inviteFieldError)"
                            />
                            <p v-if="inviteFieldError" role="alert" class="mt-1 text-xs text-rose-300">
                                {{ inviteFieldError }}
                            </p>
                        </div>

                        <div>
                            <label for="invite-role" class="text-sm text-slate-300">Organization role</label>
                            <select
                                id="invite-role"
                                v-model="inviteRole"
                                class="workspace-input"
                                :disabled="submitting"
                            >
                                <option value="organizationMember">organizationMember</option>
                                <option
                                    v-if="canInviteAdmin(data.org.currentUserRole, data.org.status)"
                                    value="organizationAdmin"
                                >
                                    organizationAdmin
                                </option>
                            </select>
                        </div>

                        <div class="sm:col-span-3">
                            <button
                                type="submit"
                                class="workspace-button text-cyan-300"
                                :disabled="submitting"
                            >
                                {{ submitting ? 'Sending invitation…' : 'Send invitation' }}
                            </button>
                        </div>
                    </form>
                </section>

                <p v-else class="workspace-panel mb-6 text-sm text-slate-400">
                    New invitations cannot be created while the organization status is {{ data.org.status }}.
                </p>

                <div
                    v-if="revokeError"
                    role="alert"
                    class="workspace-panel mb-6 border-rose-500/40 bg-rose-950/30 text-rose-200"
                >
                    <p>{{ revokeError }}</p>
                </div>

                <section
                    v-if="confirmingRevoke"
                    role="dialog"
                    aria-modal="false"
                    aria-label="Confirm invitation revocation"
                    class="workspace-panel mb-6 border-rose-500/40 bg-rose-950/20"
                >
                    <h2 class="text-lg font-semibold text-rose-200">Revoke invitation</h2>
                    <p class="mt-2 text-sm text-slate-300">
                        Revoke the pending invitation for <strong>{{ confirmingRevoke.email }}</strong> ({{ confirmingRevoke.role }})? Their invitation link will stop working immediately.
                    </p>
                    <div class="mt-4 flex flex-wrap gap-3">
                        <button
                            type="button"
                            class="workspace-button border-rose-500/50 text-rose-200"
                            :disabled="revokingId === confirmingRevoke.id"
                            @click="confirmRevokeInvitation"
                        >
                            {{ revokingId === confirmingRevoke.id ? 'Revoking…' : 'Confirm revoke' }}
                        </button>
                        <button
                            type="button"
                            class="workspace-button"
                            :disabled="revokingId === confirmingRevoke.id"
                            @click="confirmingRevoke = null"
                        >
                            Cancel
                        </button>
                    </div>
                </section>

                <form
                    aria-label="Filter invitations"
                    class="workspace-panel mb-6 grid gap-4 sm:grid-cols-3"
                    @submit.prevent="applyEmailFilter"
                >
                    <div>
                        <label for="invitation-email-filter" class="text-sm text-slate-300">Filter by email</label>
                        <input
                            id="invitation-email-filter"
                            v-model="emailFilterInput"
                            type="search"
                            maxlength="254"
                            placeholder="Filter email"
                            class="workspace-input"
                        />
                    </div>
                    <div>
                        <label for="invitation-status-filter" class="text-sm text-slate-300">Invitation status</label>
                        <select
                            id="invitation-status-filter"
                            v-model="statusFilter"
                            class="workspace-input"
                            @change="page = 1"
                        >
                            <option value="">All statuses</option>
                            <option value="pending">pending</option>
                            <option value="accepted">accepted</option>
                            <option value="revoked">revoked</option>
                            <option value="expired">expired</option>
                        </select>
                    </div>
                    <div class="flex items-end gap-2">
                        <button type="submit" class="workspace-button">Filter</button>
                        <button
                            v-if="statusFilter || appliedEmailFilter || emailFilterInput"
                            type="button"
                            class="workspace-button"
                            @click="clearFilters"
                        >
                            Clear filters
                        </button>
                    </div>
                </form>

                <div v-if="!data.invitations.data.length" class="workspace-panel">
                    <h2 class="text-xl font-semibold">No invitations found</h2>
                    <p class="mt-2 text-sm text-slate-400">
                        {{ statusFilter || appliedEmailFilter ? 'No invitations matched the selected filters.' : 'Send an invitation above to add members to this organization.' }}
                    </p>
                </div>

                <ul v-else aria-label="Organization invitations" class="space-y-4">
                    <li
                        v-for="invitation in data.invitations.data"
                        :key="invitation.id"
                        class="workspace-panel flex flex-wrap items-start justify-between gap-4"
                    >
                        <div>
                            <h2 class="break-all text-lg font-semibold">{{ invitation.email }}</h2>
                            <p class="mt-2 text-sm">
                                <span class="font-medium text-cyan-300">{{ invitation.role }}</span>
                                ·
                                <span>{{ invitation.status }}</span>
                            </p>
                            <p class="mt-2 text-xs text-slate-400">
                                Sent {{ formatDate(invitation.createdAt) }} · Expires {{ formatDate(invitation.expiresAt) }}
                            </p>
                        </div>

                        <div v-if="invitation.status === 'pending' && data.org.status === 'active'">
                            <button
                                type="button"
                                class="workspace-button text-rose-300"
                                :disabled="Boolean(revokingId)"
                                @click="promptRevoke(invitation)"
                            >
                                Revoke invitation
                            </button>
                        </div>
                    </li>
                </ul>

                <PageControls
                    :page="page"
                    :total="data.invitations.meta.totalCount"
                    @change="page = $event"
                />
            </div>
        </template>
    </ResourceState>
</template>
