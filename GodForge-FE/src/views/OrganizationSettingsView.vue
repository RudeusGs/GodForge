<script setup lang="ts">
import { computed, onScopeDispose, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { organizationsApi } from '@/api/workspace/organizations.api';
import type { OrganizationMember } from '@/api/workspace/workspace.models';
import {
    canDeleteOrganization,
    canManageOrganization,
    canTransferOrganizationOwnership,
    extractErrorCode,
    notifyWorkspaceChanged,
    ORGANIZATION_MAX_NAME_LENGTH,
    useResource,
    validateOrganizationFields,
    workspaceError,
    WORKSPACE_MAX_SLUG_LENGTH,
} from '@/features/workspace/workspaceState';
import { useAuthStore } from '@/stores/auth.store';
import OrganizationSectionNav from '@/components/workspace/OrganizationSectionNav.vue';
import PageControls from '@/components/workspace/PageControls.vue';
import ResourceState from '@/components/workspace/ResourceState.vue';

const route = useRoute();
const auth = useAuthStore();
const id = computed(() => String(route.params.organizationId));

const nameInput = ref('');
const slugInput = ref('');
const initializedOrgId = ref('');
const generalDirty = ref(false);
const generalFieldError = ref('');
const generalError = ref('');
const generalSuccess = ref('');
const savingGeneral = ref(false);

const candidatePage = ref(1);
const candidateSearchInput = ref('');
const appliedCandidateSearch = ref('');
const pageCandidates = ref<OrganizationMember[]>([]);
const candidateTotalCount = ref(0);
const candidatesLoading = ref(false);
const candidatesError = ref('');
const selectedCandidate = ref<OrganizationMember | null>(null);

const targetOwnerUserId = ref('');
const retainRole = ref<'organizationAdmin' | 'organizationMember'>('organizationAdmin');
const confirmingTransfer = ref(false);
const transferError = ref('');
const transferSuccess = ref('');
const transferring = ref(false);

const confirmationSlug = ref('');
const confirmingDelete = ref(false);
const deleteError = ref('');
const deleteSuccess = ref('');
const deleting = ref(false);

let activeTenantEpoch = 0;
let candidateGeneration = 0;
let disposed = false;
let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

function clearSearchDebounce() {
    if (searchDebounceTimer !== null) {
        clearTimeout(searchDebounceTimer);
        searchDebounceTimer = null;
    }
}

onScopeDispose(() => {
    disposed = true;
    activeTenantEpoch++;
    candidateGeneration++;
    clearSearchDebounce();
});

function isCurrentTenant(orgId: string, epoch: number): boolean {
    return !disposed && epoch === activeTenantEpoch && id.value === orgId;
}

async function loadCandidates() {
    const organizationId = id.value;
    const epoch = activeTenantEpoch;
    const currentGen = ++candidateGeneration;
    const requestedPage = candidatePage.value;
    const requestedSearch = appliedCandidateSearch.value;

    candidatesLoading.value = true;
    candidatesError.value = '';
    try {
        const res = await organizationsApi.listMembers(organizationId, {
            page: requestedPage,
            pageSize: 20,
            status: 'active',
            search: requestedSearch || undefined,
        });
        if (!isCurrentTenant(organizationId, epoch) || currentGen !== candidateGeneration) {
            return;
        }
        const eligible = res.data.filter(
            (m) => m.userId !== auth.user?.id && m.status === 'active'
        );
        pageCandidates.value = eligible;
        candidateTotalCount.value = res.meta.totalCount;

        if (selectedCandidate.value) {
            const refreshedMatch = eligible.find((m) => m.userId === selectedCandidate.value?.userId);
            if (refreshedMatch) {
                selectedCandidate.value = refreshedMatch;
            }
        } else if (eligible.length > 0) {
            selectedCandidate.value = eligible[0]!;
            targetOwnerUserId.value = eligible[0]!.userId;
        }
    } catch (cause) {
        if (!isCurrentTenant(organizationId, epoch) || currentGen !== candidateGeneration) {
            return;
        }
        candidatesError.value = workspaceError(cause);
    } finally {
        if (isCurrentTenant(organizationId, epoch) && currentGen === candidateGeneration) {
            candidatesLoading.value = false;
        }
    }
}

const { data, loading, error, load } = useResource(
    async () => {
        const organizationId = id.value;
        const org = (await organizationsApi.get(organizationId)).data;
        return { org };
    },
    (result) => {
        if (disposed || result.org.id !== id.value) {
            return;
        }
        if (initializedOrgId.value !== result.org.id || !generalDirty.value) {
            nameInput.value = result.org.name;
            slugInput.value = result.org.slug;
            initializedOrgId.value = result.org.id;
            generalDirty.value = false;
        }
        if (canTransferOrganizationOwnership(result.org.currentUserRole, result.org.status)) {
            void loadCandidates();
        } else {
            pageCandidates.value = [];
            candidateTotalCount.value = 0;
            selectedCandidate.value = null;
            targetOwnerUserId.value = '';
        }
    }
);

function resetTransient() {
    clearSearchDebounce();
    initializedOrgId.value = '';
    generalDirty.value = false;
    nameInput.value = '';
    slugInput.value = '';
    generalFieldError.value = '';
    generalError.value = '';
    generalSuccess.value = '';
    savingGeneral.value = false;

    candidatePage.value = 1;
    candidateSearchInput.value = '';
    appliedCandidateSearch.value = '';
    pageCandidates.value = [];
    candidateTotalCount.value = 0;
    candidatesLoading.value = false;
    candidatesError.value = '';
    selectedCandidate.value = null;
    targetOwnerUserId.value = '';
    retainRole.value = 'organizationAdmin';
    confirmingTransfer.value = false;
    transferError.value = '';
    transferSuccess.value = '';
    transferring.value = false;

    confirmationSlug.value = '';
    confirmingDelete.value = false;
    deleteError.value = '';
    deleteSuccess.value = '';
    deleting.value = false;
}

watch(
    id,
    () => {
        activeTenantEpoch++;
        candidateGeneration++;
        resetTransient();
        void load();
    },
    { immediate: true }
);

function markGeneralEdited() {
    generalDirty.value = true;
    generalFieldError.value = '';
}

function handleCandidateSearchInput() {
    clearSearchDebounce();
    searchDebounceTimer = setTimeout(() => {
        applyCandidateSearch();
    }, 250);
}

function applyCandidateSearch() {
    clearSearchDebounce();
    const nextSearch = candidateSearchInput.value.trim();
    const changed = appliedCandidateSearch.value !== nextSearch || candidatePage.value !== 1;
    candidatePage.value = 1;
    appliedCandidateSearch.value = nextSearch;
    if (changed && data.value && canTransferOrganizationOwnership(data.value.org.currentUserRole, data.value.org.status)) {
        void loadCandidates();
    }
}

function clearCandidateSearch() {
    clearSearchDebounce();
    candidateSearchInput.value = '';
    candidatePage.value = 1;
    appliedCandidateSearch.value = '';
    if (data.value && canTransferOrganizationOwnership(data.value.org.currentUserRole, data.value.org.status)) {
        void loadCandidates();
    }
}

function changeCandidatePage(nextPage: number) {
    candidatePage.value = nextPage;
    if (data.value && canTransferOrganizationOwnership(data.value.org.currentUserRole, data.value.org.status)) {
        void loadCandidates();
    }
}

const displayedCandidates = computed(() => {
    const list = [...pageCandidates.value];
    if (
        selectedCandidate.value &&
        !list.some((m) => m.userId === selectedCandidate.value?.userId)
    ) {
        list.unshift(selectedCandidate.value);
    }
    return list;
});

function handleCandidateSelectChange() {
    confirmingTransfer.value = false;
    const found = displayedCandidates.value.find((m) => m.userId === targetOwnerUserId.value);
    if (found) {
        selectedCandidate.value = found;
    }
}

function validateGeneral(): boolean {
    const validationMsg = validateOrganizationFields(nameInput.value, slugInput.value);
    if (validationMsg) {
        generalFieldError.value = validationMsg;
        return false;
    }
    generalFieldError.value = '';
    return true;
}

async function saveGeneralSettings() {
    if (!data.value || savingGeneral.value) return;
    generalError.value = '';
    generalSuccess.value = '';
    if (!validateGeneral()) return;

    const mutationOrgId = id.value;
    const mutationEpoch = activeTenantEpoch;
    const nextName = nameInput.value.trim();
    const nextSlug = slugInput.value.trim();
    const expectedVersion = data.value.org.version;

    savingGeneral.value = true;
    try {
        const updated = await organizationsApi.update(mutationOrgId, {
            name: nextName,
            slug: nextSlug,
            version: expectedVersion,
        });
        if (!isCurrentTenant(mutationOrgId, mutationEpoch)) return;
        generalDirty.value = false;
        generalSuccess.value = `Saved organization settings (version ${updated.data.version}).`;
        notifyWorkspaceChanged();
        await load();
    } catch (cause) {
        if (!isCurrentTenant(mutationOrgId, mutationEpoch)) return;
        generalError.value = workspaceError(cause);
    } finally {
        if (isCurrentTenant(mutationOrgId, mutationEpoch)) {
            savingGeneral.value = false;
        }
    }
}

const selectedTargetMember = computed(() =>
    displayedCandidates.value.find((m) => m.userId === targetOwnerUserId.value)
);

function requestTransferReview() {
    transferError.value = '';
    transferSuccess.value = '';
    if (!targetOwnerUserId.value || !selectedTargetMember.value) {
        transferError.value = 'Select an active organization member to receive ownership.';
        return;
    }
    confirmingTransfer.value = true;
}

async function confirmOwnershipTransfer() {
    if (!data.value || !targetOwnerUserId.value || transferring.value) return;
    const mutationOrgId = id.value;
    const mutationEpoch = activeTenantEpoch;
    const targetId = targetOwnerUserId.value;
    const retained = retainRole.value;
    const expectedVersion = data.value.org.version;

    transferring.value = true;
    transferError.value = '';
    transferSuccess.value = '';

    try {
        const result = await organizationsApi.transferOwnership(mutationOrgId, {
            newOwnerUserId: targetId,
            retainCurrentOwnerAs: retained,
            version: expectedVersion,
        });
        if (!isCurrentTenant(mutationOrgId, mutationEpoch)) return;
        confirmingTransfer.value = false;
        transferSuccess.value = `Transferred organization ownership to ${result.data.newOwner.displayName}. Your role is now ${result.data.previousOwner.role}.`;
        notifyWorkspaceChanged();
        await load();
    } catch (cause) {
        if (!isCurrentTenant(mutationOrgId, mutationEpoch)) return;
        transferError.value = workspaceError(cause);
        confirmingTransfer.value = false;
        const code = extractErrorCode(cause);
        if (code === 'CONCURRENCY_CONFLICT' || code === 'MEMBERSHIP_NOT_FOUND') {
            selectedCandidate.value = null;
            targetOwnerUserId.value = '';
            await load();
        }
    } finally {
        if (isCurrentTenant(mutationOrgId, mutationEpoch)) {
            transferring.value = false;
        }
    }
}

function requestDeletionReview() {
    deleteError.value = '';
    deleteSuccess.value = '';
    if (!data.value) return;
    if (confirmationSlug.value.trim() !== data.value.org.slug) {
        deleteError.value = `Enter the exact organization slug "${data.value.org.slug}" to confirm deletion.`;
        return;
    }
    confirmingDelete.value = true;
}

async function confirmDeletionRequest() {
    if (!data.value || deleting.value) return;
    const mutationOrgId = id.value;
    const mutationEpoch = activeTenantEpoch;
    const expectedVersion = data.value.org.version;
    const confirmedSlug = confirmationSlug.value.trim();

    deleting.value = true;
    deleteError.value = '';
    deleteSuccess.value = '';

    try {
        const response = await organizationsApi.requestDeletion(mutationOrgId, {
            version: expectedVersion,
            confirmationSlug: confirmedSlug,
        });
        if (!isCurrentTenant(mutationOrgId, mutationEpoch)) return;
        confirmingDelete.value = false;
        confirmationSlug.value = '';
        deleteSuccess.value = `Organization deletion requested. Lifecycle status is now ${response.data.status} (version ${response.data.version}).`;
        notifyWorkspaceChanged();
        await load();
    } catch (cause) {
        if (!isCurrentTenant(mutationOrgId, mutationEpoch)) return;
        deleteError.value = workspaceError(cause);
        confirmingDelete.value = false;
        if (extractErrorCode(cause) === 'CONCURRENCY_CONFLICT') {
            await load();
        }
    } finally {
        if (isCurrentTenant(mutationOrgId, mutationEpoch)) {
            deleting.value = false;
        }
    }
}
</script>

<template>
    <ResourceState :loading="loading" :error="error" @retry="load">
        <template v-if="data">
            <div class="mb-6">
                <p class="text-sm text-cyan-400">{{ data.org.name }} / {{ data.org.slug }}</p>
                <h1 class="mt-2 text-3xl font-semibold">Settings</h1>
                <p class="mt-3 text-sm text-slate-400">
                    {{ data.org.currentUserRole }} · {{ data.org.status }} · v{{ data.org.version }}
                </p>
            </div>

            <OrganizationSectionNav :organization="data.org" />

            <div
                v-if="!['organizationOwner', 'organizationAdmin'].includes(data.org.currentUserRole)"
                role="alert"
                class="workspace-panel border-amber-500/40 bg-amber-950/20 text-amber-200"
            >
                <h2 class="text-lg font-semibold">Permission required</h2>
                <p class="mt-2 text-sm text-slate-300">
                    Only an OrganizationOwner or OrganizationAdmin can access organization settings.
                </p>
            </div>

            <div v-else class="space-y-8">
                <div
                    v-if="transferSuccess"
                    role="status"
                    class="workspace-panel border-emerald-500/40 bg-emerald-950/30 text-emerald-200"
                >
                    <p>{{ transferSuccess }}</p>
                </div>

                <div
                    v-if="deleteSuccess"
                    role="status"
                    class="workspace-panel border-amber-500/40 bg-amber-950/30 text-amber-200"
                >
                    <p>{{ deleteSuccess }}</p>
                </div>

                <!-- 1. General Settings -->
                <section aria-label="General settings" class="workspace-panel">
                    <h2 class="text-lg font-semibold">General settings</h2>
                    <p class="mt-2 text-sm text-slate-400">
                        Update the organization display name (1–{{ ORGANIZATION_MAX_NAME_LENGTH }} characters) and URL slug (1–{{ WORKSPACE_MAX_SLUG_LENGTH }} characters). Changes require the current metadata version ({{ data.org.version }}).
                    </p>

                    <div
                        v-if="generalError"
                        role="alert"
                        class="mt-4 rounded-xl border border-rose-500/40 bg-rose-950/30 p-3 text-sm text-rose-200"
                    >
                        <p>{{ generalError }}</p>
                        <button type="button" class="workspace-button mt-2" @click="load">
                            Reload latest settings
                        </button>
                    </div>

                    <div
                        v-if="generalSuccess"
                        role="status"
                        class="mt-4 rounded-xl border border-emerald-500/40 bg-emerald-950/30 p-3 text-sm text-emerald-200"
                    >
                        {{ generalSuccess }}
                    </div>

                    <p
                        v-if="!canManageOrganization(data.org.currentUserRole, data.org.status)"
                        class="mt-4 text-sm text-amber-200"
                    >
                        General settings are read-only while the organization status is {{ data.org.status }}.
                    </p>

                    <form
                        class="mt-4 grid gap-4 sm:grid-cols-2"
                        novalidate
                        @submit.prevent="saveGeneralSettings"
                    >
                        <div>
                            <label for="org-settings-name" class="text-sm text-slate-300">Organization name</label>
                            <input
                                id="org-settings-name"
                                v-model="nameInput"
                                type="text"
                                :maxlength="ORGANIZATION_MAX_NAME_LENGTH"
                                class="workspace-input"
                                :disabled="
                                    savingGeneral ||
                                    !canManageOrganization(data.org.currentUserRole, data.org.status)
                                "
                                @input="markGeneralEdited"
                            />
                        </div>
                        <div>
                            <label for="org-settings-slug" class="text-sm text-slate-300">Organization slug</label>
                            <input
                                id="org-settings-slug"
                                v-model="slugInput"
                                type="text"
                                :maxlength="WORKSPACE_MAX_SLUG_LENGTH"
                                class="workspace-input"
                                autocapitalize="none"
                                spellcheck="false"
                                :disabled="
                                    savingGeneral ||
                                    !canManageOrganization(data.org.currentUserRole, data.org.status)
                                "
                                @input="markGeneralEdited"
                            />
                        </div>
                        <p
                            v-if="generalFieldError"
                            role="alert"
                            class="text-xs text-rose-300 sm:col-span-2"
                        >
                            {{ generalFieldError }}
                        </p>
                        <div class="sm:col-span-2">
                            <button
                                type="submit"
                                class="workspace-button text-cyan-300"
                                :disabled="
                                    savingGeneral ||
                                    !canManageOrganization(data.org.currentUserRole, data.org.status)
                                "
                            >
                                {{ savingGeneral ? 'Saving changes…' : 'Save general settings' }}
                            </button>
                        </div>
                    </form>
                </section>

                <!-- 2. Ownership Transfer -->
                <section aria-label="Ownership transfer" class="workspace-panel">
                    <h2 class="text-lg font-semibold">Ownership transfer</h2>
                    <p class="mt-2 text-sm text-slate-400">
                        Transfer OrganizationOwner authority to another active member of this organization.
                    </p>

                    <p
                        v-if="data.org.currentUserRole !== 'organizationOwner'"
                        class="mt-4 text-sm text-slate-400"
                    >
                        Only an OrganizationOwner can transfer organization ownership.
                    </p>
                    <p
                        v-else-if="data.org.status !== 'active'"
                        class="mt-4 text-sm text-amber-200"
                    >
                        Ownership cannot be transferred while the organization status is {{ data.org.status }}.
                    </p>
                    <template v-else>
                        <div
                            v-if="transferError"
                            role="alert"
                            class="mt-4 rounded-xl border border-rose-500/40 bg-rose-950/30 p-3 text-sm text-rose-200"
                        >
                            {{ transferError }}
                        </div>

                        <div
                            v-if="candidatesError"
                            role="alert"
                            class="mt-4 rounded-xl border border-rose-500/40 bg-rose-950/30 p-3 text-sm text-rose-200"
                        >
                            <p>{{ candidatesError }}</p>
                            <button type="button" class="workspace-button mt-2" @click="loadCandidates">
                                Retry loading candidates
                            </button>
                        </div>

                        <form
                            aria-label="Search ownership candidates"
                            class="mt-4 flex flex-wrap items-end gap-3"
                            @submit.prevent="applyCandidateSearch"
                        >
                            <div class="min-w-[220px] flex-1">
                                <label for="transfer-candidate-search" class="text-sm text-slate-300">
                                    Search active members
                                </label>
                                <input
                                    id="transfer-candidate-search"
                                    v-model="candidateSearchInput"
                                    type="search"
                                    maxlength="200"
                                    placeholder="Search by name or email"
                                    class="workspace-input"
                                    :disabled="transferring"
                                    @input="handleCandidateSearchInput"
                                />
                            </div>
                            <button
                                type="submit"
                                class="workspace-button"
                                :disabled="transferring || candidatesLoading"
                            >
                                Search candidates
                            </button>
                            <button
                                v-if="appliedCandidateSearch || candidateSearchInput"
                                type="button"
                                class="workspace-button"
                                :disabled="transferring || candidatesLoading"
                                @click="clearCandidateSearch"
                            >
                                Clear search
                            </button>
                        </form>

                        <p
                            v-if="candidatesLoading && !displayedCandidates.length"
                            role="status"
                            class="mt-4 text-sm text-slate-400"
                        >
                            Loading eligible members…
                        </p>

                        <p
                            v-else-if="!displayedCandidates.length"
                            class="mt-4 text-sm text-slate-400"
                        >
                            {{
                                appliedCandidateSearch
                                    ? 'No active members matched your search.'
                                    : 'No other active organization members are available. Invite and activate another member before transferring ownership.'
                            }}
                        </p>

                        <div v-else class="mt-4 space-y-4">
                            <div class="grid gap-4 sm:grid-cols-2">
                                <div>
                                    <label for="transfer-target-member" class="text-sm text-slate-300">
                                        New organization owner
                                    </label>
                                    <select
                                        id="transfer-target-member"
                                        v-model="targetOwnerUserId"
                                        class="workspace-input"
                                        :disabled="transferring"
                                        @change="handleCandidateSelectChange"
                                    >
                                        <option
                                            v-for="member in displayedCandidates"
                                            :key="member.userId"
                                            :value="member.userId"
                                        >
                                            {{ member.displayName }} ({{ member.email }} · {{ member.role }})
                                        </option>
                                    </select>
                                </div>

                                <div>
                                    <label for="transfer-retain-role" class="text-sm text-slate-300">
                                        Retain your membership as
                                    </label>
                                    <select
                                        id="transfer-retain-role"
                                        v-model="retainRole"
                                        class="workspace-input"
                                        :disabled="transferring"
                                        @change="confirmingTransfer = false"
                                    >
                                        <option value="organizationAdmin">organizationAdmin</option>
                                        <option value="organizationMember">organizationMember</option>
                                    </select>
                                </div>
                            </div>

                            <PageControls
                                :page="candidatePage"
                                :total="candidateTotalCount"
                                @change="changeCandidatePage"
                            />

                            <div v-if="!confirmingTransfer">
                                <button
                                    type="button"
                                    class="workspace-button text-amber-300"
                                    :disabled="transferring || !targetOwnerUserId"
                                    @click="requestTransferReview"
                                >
                                    Review ownership transfer
                                </button>
                            </div>

                            <div
                                v-else
                                role="region"
                                aria-label="Confirm ownership transfer"
                                class="rounded-xl border border-amber-500/40 bg-amber-950/20 p-4"
                            >
                                <p class="text-sm text-amber-100">
                                    Confirm transferring <strong>{{ data.org.name }}</strong> ownership to
                                    <strong>{{ selectedTargetMember?.displayName }}</strong> ({{ selectedTargetMember?.email }})?
                                    Your role will immediately change to <strong>{{ retainRole }}</strong>.
                                </p>
                                <div class="mt-4 flex flex-wrap gap-3">
                                    <button
                                        type="button"
                                        class="workspace-button border-amber-500/50 text-amber-200"
                                        :disabled="transferring"
                                        @click="confirmOwnershipTransfer"
                                    >
                                        {{ transferring ? 'Transferring ownership…' : 'Confirm ownership transfer' }}
                                    </button>
                                    <button
                                        type="button"
                                        class="workspace-button"
                                        :disabled="transferring"
                                        @click="confirmingTransfer = false"
                                    >
                                        Cancel
                                    </button>
                                </div>
                            </div>
                        </div>
                    </template>
                </section>

                <!-- 3. Danger Zone -->
                <section
                    aria-label="Danger zone"
                    class="workspace-panel border-rose-500/30"
                >
                    <h2 class="text-lg font-semibold text-rose-300">Danger zone</h2>
                    <p class="mt-2 text-sm text-slate-400">
                        Requesting organization deletion transitions the organization to the retained
                        <code>deleting</code> lifecycle state and blocks further mutations while retention policies apply.
                    </p>

                    <p
                        v-if="data.org.currentUserRole !== 'organizationOwner'"
                        class="mt-4 text-sm text-slate-400"
                    >
                        Only an OrganizationOwner can request organization deletion.
                    </p>
                    <p
                        v-else-if="data.org.status === 'deleting'"
                        role="status"
                        class="mt-4 text-sm text-amber-200"
                    >
                        Deletion has already been requested for this organization. Current status is
                        <strong>{{ data.org.status }}</strong> (version {{ data.org.version }}).
                    </p>
                    <template v-else-if="canDeleteOrganization(data.org.currentUserRole, data.org.status)">
                        <div
                            v-if="deleteError"
                            role="alert"
                            class="mt-4 rounded-xl border border-rose-500/40 bg-rose-950/30 p-3 text-sm text-rose-200"
                        >
                            {{ deleteError }}
                        </div>

                        <div class="mt-4 max-w-md">
                            <label for="delete-confirmation-slug" class="text-sm text-slate-300">
                                Type <code>{{ data.org.slug }}</code> to confirm deletion request
                            </label>
                            <input
                                id="delete-confirmation-slug"
                                v-model="confirmationSlug"
                                type="text"
                                class="workspace-input"
                                :placeholder="data.org.slug"
                                :disabled="deleting"
                                @input="confirmingDelete = false"
                            />
                        </div>

                        <div v-if="!confirmingDelete" class="mt-4">
                            <button
                                type="button"
                                class="workspace-button text-rose-300"
                                :disabled="deleting"
                                @click="requestDeletionReview"
                            >
                                Request organization deletion
                            </button>
                        </div>

                        <div
                            v-else
                            role="region"
                            aria-label="Confirm organization deletion request"
                            class="mt-4 rounded-xl border border-rose-500/40 bg-rose-950/20 p-4"
                        >
                            <p class="text-sm text-rose-100">
                                Confirm placing <strong>{{ data.org.name }}</strong> ({{ data.org.slug }}) into the
                                <strong>deleting</strong> lifecycle state?
                            </p>
                            <div class="mt-4 flex flex-wrap gap-3">
                                <button
                                    type="button"
                                    class="workspace-button border-rose-500/50 text-rose-200"
                                    :disabled="deleting"
                                    @click="confirmDeletionRequest"
                                >
                                    {{ deleting ? 'Submitting request…' : 'Confirm deletion request' }}
                                </button>
                                <button
                                    type="button"
                                    class="workspace-button"
                                    :disabled="deleting"
                                    @click="confirmingDelete = false"
                                >
                                    Cancel
                                </button>
                            </div>
                        </div>
                    </template>
                </section>
            </div>
        </template>
    </ResourceState>
</template>
