<script setup lang="ts">
import axios from 'axios';
import { onScopeDispose, ref, watch } from 'vue';
import { settingsApi, type OrganizationPolicy } from '@/api/settings.api';
import { extractErrorCode, extractStatus, workspaceError } from '@/features/workspace/workspaceState';
const props = defineProps<{ organizationId: string; mutable: boolean }>();
const policy = ref<OrganizationPolicy>();
const allowed = ref(false);
const loading = ref(false);
const busy = ref(false);
const error = ref('');
const success = ref('');
const needsReconciliation = ref(false);
const forbidden = ref(false);
let generation = 0;
let disposed = false;
onScopeDispose(() => { disposed = true; generation++; });
const current = (request: number) => !disposed && request === generation;
async function reconcile(id: string, request: number, message: string, submittedVersion?: number) {
    needsReconciliation.value = true;
    error.value = message;
    try {
        const result = (await settingsApi.getPolicy(id)).data;
        if (!current(request)) return;
        policy.value = result;
        needsReconciliation.value = false;
        forbidden.value = false; // This read also requires current organization administration permission.
        if (submittedVersion !== undefined && result.version > submittedVersion && result.aiAdvisoryAllowed === allowed.value) {
            error.value = '';
            success.value = 'The current policy matches your requested values. No retry is needed.';
        } else {
            error.value = `${message} Latest policy version ${result.version} loaded; your edits are retained. Review and save deliberately to resubmit.`;
        }
    } catch {
        if (current(request)) error.value = `${message} Current policy could not be loaded. Load current capability policy before retrying; your edits are retained.`;
    }
}
async function load() {
    if (busy.value || loading.value) return;
    const request = ++generation;
    const id = props.organizationId;
    loading.value = true; error.value = ''; success.value = '';
    try {
        if (needsReconciliation.value) {
            await reconcile(id, request, 'The previous save outcome is unknown.');
        } else {
            const result = (await settingsApi.getPolicy(id)).data;
            if (current(request)) { policy.value = result; allowed.value = result.aiAdvisoryAllowed; forbidden.value = false; }
        }
    } catch (cause) { if (current(request)) error.value = workspaceError(cause); }
    finally { if (current(request)) loading.value = false; }
}
async function save() {
    if (!policy.value || busy.value || loading.value || !props.mutable || needsReconciliation.value || forbidden.value || allowed.value === policy.value.aiAdvisoryAllowed) return;
    const request = ++generation;
    const id = props.organizationId;
    const input = { aiAdvisoryAllowed: allowed.value, version: policy.value.version };
    busy.value = true; error.value = ''; success.value = '';
    try {
        const result = (await settingsApi.updatePolicy(id, input)).data;
        if (current(request)) { policy.value = result; allowed.value = result.aiAdvisoryAllowed; success.value = 'Organization policy saved.'; }
    } catch (cause) {
        if (!current(request)) return;
        const code = extractErrorCode(cause);
        const status = extractStatus(cause);
        if (code === 'CONCURRENCY_CONFLICT') {
            await reconcile(id, request, 'The policy changed by another administrator.');
        } else if (code === 'SECURITY_FORBIDDEN' || status === 403 || status === 401 || status === 404) {
            forbidden.value = true;
            error.value = workspaceError(cause);
        } else if (status !== undefined && status >= 400 && status < 500 && status !== 408) {
            // API errors are safe contract messages; Vue renders them as text.
            error.value = axios.isAxiosError<{ error?: { message?: string } }>(cause)
                ? cause.response?.data.error?.message || workspaceError(cause) : workspaceError(cause);
        } else {
            await reconcile(id, request, 'The save outcome is unknown because the request failed.', input.version);
        }
    } finally { if (current(request)) busy.value = false; }
}
watch(() => props.organizationId, () => {
    generation++; policy.value = undefined; allowed.value = false;
    busy.value = false; loading.value = false; needsReconciliation.value = false; forbidden.value = false;
    void load();
}, { immediate: true });
</script>
<template>
    <section class="workspace-panel my-5" aria-label="Capability policy">
        <h2 class="text-lg font-semibold">Capability policy</h2>
        <p class="my-3 text-slate-400">Restrictions apply to every project and never grant project permissions. AI execution is not certified by this preference.</p>
        <p v-if="loading" role="status">Loading capability policy…</p><p v-if="error" role="alert" class="text-rose-300">{{ error }}</p><p v-if="success" role="status" class="text-cyan-300">{{ success }}</p>
        <form v-if="policy" class="space-y-4" @submit.prevent="save"><fieldset :disabled="busy || loading || !mutable" class="space-y-4">
            <label class="flex items-center gap-3"><input v-model="allowed" type="checkbox" /> Allow project AI advisory preference</label>
            <p>Platform AI capability: {{ policy.platformAiAdvisoryAllowed ? 'available' : 'disabled' }} · Policy version {{ policy.version }}</p>
            <p v-if="!policy.platformAiAdvisoryAllowed" class="text-slate-400">Platform restriction always wins, including when organization allowance is saved.</p>
            <button class="workspace-button" :disabled="needsReconciliation || forbidden || allowed === policy.aiAdvisoryAllowed">Save capability policy</button>
        </fieldset></form>
        <button class="workspace-button mt-3" :disabled="busy || loading" @click="load">Load current capability policy</button>
    </section>
</template>
