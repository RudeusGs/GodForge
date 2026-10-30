<script setup lang="ts">
import { ref } from 'vue';
import {
    isValidOrganizationName,
    isValidProjectName,
    isValidWorkspaceSlug,
    ORGANIZATION_MAX_NAME_LENGTH,
    PROJECT_MAX_NAME_LENGTH,
    workspaceError,
    WORKSPACE_MAX_SLUG_LENGTH,
} from './workspaceState';
import type { ProjectInput } from '@/api/workspace/workspace.models';

const props = defineProps<{ kind: 'organization' | 'project'; submit: (input: ProjectInput, key: string) => Promise<void> }>();
const emit = defineEmits<{ created: [] }>();
const name = ref('');
const slug = ref('');
const description = ref('');
const visibility = ref<ProjectInput['visibility']>('private');
const pending = ref(false);
const error = ref('');
// Preserve the key for identical retries after an uncertain network outcome.
let previousPayload = '';
let key = '';
async function save() {
    if (pending.value) return;
    const input: ProjectInput = { name: name.value.trim(), slug: slug.value.trim(), description: description.value.trim() || null, visibility: visibility.value };
    const validName = props.kind === 'project' ? isValidProjectName(input.name) : isValidOrganizationName(input.name);
    if (!validName || !isValidWorkspaceSlug(input.slug)) {
        error.value = 'Enter a name and a lowercase slug using letters, numbers and single hyphens.';
        return;
    }
    const payload = JSON.stringify(input);
    if (payload !== previousPayload) { key = crypto.randomUUID(); previousPayload = payload; }
    pending.value = true;
    error.value = '';
    try { await props.submit(input, key); emit('created'); }
    catch (cause) { error.value = workspaceError(cause); }
    finally { pending.value = false; }
}
</script>
<template>
    <form class="workspace-panel space-y-4" :aria-label="`Create ${kind}`" :aria-busy="pending" @submit.prevent="save">
        <h2 class="text-lg font-semibold">Create {{ kind }}</h2>
        <p v-if="error" role="alert" class="text-rose-300">{{ error }}</p>
        <fieldset :disabled="pending" class="space-y-4 disabled:opacity-60">
            <label class="block">Name<input v-model="name" class="workspace-input" required :maxlength="kind === 'project' ? PROJECT_MAX_NAME_LENGTH : ORGANIZATION_MAX_NAME_LENGTH" autocomplete="off" /></label>
            <label class="block">Slug<input v-model="slug" class="workspace-input" required :maxlength="WORKSPACE_MAX_SLUG_LENGTH" pattern="[a-z0-9]+(-[a-z0-9]+)*" autocapitalize="none" spellcheck="false" aria-describedby="slug-help" /></label>
            <p id="slug-help" class="text-sm text-slate-400">Use lowercase letters, numbers and single hyphens.</p>
            <template v-if="kind === 'project'">
                <label class="block">Description<textarea v-model="description" class="workspace-input" maxlength="2000" rows="3" /></label>
                <label class="block">Visibility<select v-model="visibility" class="workspace-input"><option value="private">Private</option><option value="internal">Internal</option><option value="public">Public</option></select></label>
                <p class="text-sm text-slate-400">Visibility is subject to server policy. Creating a project gives you its ProjectOwner role.</p>
            </template>
            <button type="submit" class="workspace-button bg-cyan-950 text-cyan-100">{{ pending ? 'Creating…' : `Create ${kind}` }}</button>
        </fieldset>
    </form>
</template>
