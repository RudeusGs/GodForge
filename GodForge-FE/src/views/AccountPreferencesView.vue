<script setup lang="ts">
import { computed, onMounted, onScopeDispose, ref } from 'vue';
import { settingsApi, type UserPreferences, type UserPreferencesInput } from '@/api/settings.api';
import { extractErrorCode, workspaceError } from '@/features/workspace/workspaceState';
import { applyTheme } from '@/features/theme';
import ResourceState from '@/components/workspace/ResourceState.vue';

const persisted = ref<UserPreferences>();
const form = ref<UserPreferencesInput>();
const loading = ref(true);
const saving = ref(false);
const error = ref('');
const feedback = ref('');
const conflict = ref(false);
let disposed = false;
let generation = 0;
onScopeDispose(() => { disposed = true; generation++; });
function input(value: UserPreferences): UserPreferencesInput {
    return { theme: value.theme, notificationInApp: value.notificationInApp, notificationEmail: value.notificationEmail, notificationDigest: value.notificationDigest, version: value.version };
}
const dirty = computed(() => !!form.value && !!persisted.value && JSON.stringify(form.value) !== JSON.stringify(input(persisted.value)));
async function load() {
    if (saving.value) return;
    const current = ++generation;
    loading.value = true; error.value = ''; feedback.value = '';
    try {
        const result = (await settingsApi.getPreferences()).data;
        if (disposed || current !== generation) return;
        persisted.value = result; form.value = input(result); conflict.value = false; applyTheme(result.theme);
    } catch (cause) { if (!disposed && current === generation) error.value = workspaceError(cause); }
    finally { if (!disposed && current === generation) loading.value = false; }
}
function revert() {
    if (!persisted.value || saving.value) return;
    form.value = input(persisted.value); feedback.value = ''; error.value = '';
}
async function save() {
    if (!form.value || !dirty.value || saving.value || conflict.value) return;
    if (!['light', 'dark'].includes(form.value.theme) || !['off', 'daily', 'weekly'].includes(form.value.notificationDigest)) {
        error.value = 'Choose a supported theme and digest.'; return;
    }
    const current = ++generation;
    saving.value = true; error.value = ''; feedback.value = '';
    try {
        const result = (await settingsApi.updatePreferences({ ...form.value })).data;
        if (disposed || current !== generation) return;
        persisted.value = result; form.value = input(result); applyTheme(result.theme); feedback.value = 'Preferences saved.';
    } catch (cause) {
        if (disposed || current !== generation) return;
        conflict.value = extractErrorCode(cause) === 'CONCURRENCY_CONFLICT';
        error.value = conflict.value ? 'Preferences changed in another session. Your edits are retained. Load current preferences before saving again.' : workspaceError(cause);
    } finally { if (!disposed && current === generation) saving.value = false; }
}
onMounted(load);
</script>
<template>
    <main class="min-h-screen bg-slate-950 px-4 py-10 text-slate-100">
        <section class="mx-auto max-w-2xl">
            <nav aria-label="Account" class="mb-6 flex flex-wrap gap-4 text-cyan-300"><router-link to="/dashboard">Workspace</router-link><router-link to="/account/sessions">Security sessions</router-link></nav>
            <h1 class="text-3xl font-semibold">Account preferences</h1>
            <p class="my-4 text-slate-400">Theme applies to the workspace and account screens. Notification delivery is not available yet; these values store your preferences.</p>
            <p v-if="error" role="alert" class="my-4 text-rose-300">{{ error }}</p>
            <p v-if="feedback" role="status" class="my-4 text-cyan-300">{{ feedback }}</p>
            <ResourceState :loading="loading" :error="!persisted ? error : ''" @retry="load">
                <form v-if="form && persisted" class="workspace-panel space-y-5" @submit.prevent="save">
                    <fieldset :disabled="saving" class="space-y-5">
                        <div><label for="preference-theme" class="block">Theme</label><select id="preference-theme" v-model="form.theme" class="workspace-input"><option value="light">Light</option><option value="dark">Dark</option></select></div>
                        <label class="flex items-center gap-3"><input v-model="form.notificationInApp" type="checkbox" /> In-app notifications preference</label>
                        <label class="flex items-center gap-3"><input v-model="form.notificationEmail" type="checkbox" /> Email notifications preference</label>
                        <div><label for="preference-digest" class="block">Notification digest</label><select id="preference-digest" v-model="form.notificationDigest" class="workspace-input"><option value="off">Off</option><option value="daily">Daily</option><option value="weekly">Weekly</option></select></div>
                        <p class="text-sm text-slate-400">Version {{ persisted.version }} · {{ dirty ? 'Unsaved changes' : 'All changes saved' }}</p>
                        <div class="flex flex-wrap gap-3"><button class="workspace-button" :disabled="!dirty || conflict">{{ saving ? 'Saving…' : 'Save preferences' }}</button><button type="button" class="workspace-button" :disabled="!dirty" @click="revert">Revert unsaved edits</button><button type="button" class="workspace-button" @click="load">Load current preferences</button></div>
                    </fieldset>
                </form>
            </ResourceState>
        </section>
    </main>
</template>
