<script setup lang="ts">
import { ref, watch } from 'vue';
import { projectsApi } from '@/api/workspace/projects.api';
import type { OrganizationMember, ProjectInput, TransferProjectOwnershipInput, UpdateProjectSettingsInput } from '@/api/workspace/workspace.models';
import { useProjectWorkspace } from '@/features/workspace/projectWorkspace';
import { isValidProjectName, isValidWorkspaceSlug, useResource } from '@/features/workspace/workspaceState';
import { useAuthStore } from '@/stores/auth.store';
import ProjectSectionNav from '@/components/workspace/ProjectSectionNav.vue';
import ProjectMemberPicker from '@/components/workspace/ProjectMemberPicker.vue';
import ResourceState from '@/components/workspace/ResourceState.vue';
import WorkspaceActionPanel from '@/components/workspace/WorkspaceActionPanel.vue';

const { data, loading, error, load, id, identity, busy, actionError, success, manager, owner, mutable, mutate, captureContext } = useProjectWorkspace();
const auth = useAuthStore();
const general = ref<ProjectInput>({ name: '', slug: '', description: null, visibility: 'private' });
const preferences = ref<UpdateProjectSettingsInput>();
const candidate = ref<OrganizationMember>();
const retainedRole = ref<TransferProjectOwnershipInput['retainCurrentOwnerAs']>('maintainer');
const confirmation = ref<'archive' | 'restore' | 'delete' | 'ownership' | ''>('');
const confirmationSlug = ref('');
const validation = ref('');
let initialized = '';
const settings = useResource(async () => (await projectsApi.getSettings(id.value)).data);
const { data: settingsData, loading: settingsLoading, error: settingsError, load: loadSettings } = settings;
watch(identity, () => { initialized = ''; preferences.value = undefined; candidate.value = undefined; confirmation.value = ''; confirmationSlug.value = ''; validation.value = ''; });
watch(data, value => {
    if (!value || initialized === value.project.id) return;
    initialized = value.project.id;
    general.value = { name: value.project.name, slug: value.project.slug, description: value.project.description, visibility: value.project.visibility as ProjectInput['visibility'] };
    if (manager.value) void loadSettings();
});
watch(settingsData, value => {
    if (value && !preferences.value) preferences.value = { analysisProfileKey: value.analysisProfileKey, aiAdvisoryEnabled: value.aiAdvisoryEnabled, defaultAssetVisibility: value.defaultAssetVisibility, notificationPolicyVersion: value.notificationPolicyVersion, version: value.version };
});
async function saveGeneral() {
    validation.value = '';
    if (!isValidProjectName(general.value.name) || !isValidWorkspaceSlug(general.value.slug)) { validation.value = 'Name and slug must be 1–80 characters. Slugs use lowercase letters, digits and single interior hyphens.'; return; }
    const input = { ...general.value, name: general.value.name.trim(), slug: general.value.slug.trim(), version: data.value!.project.version };
    await mutate(projectId => projectsApi.update(projectId, input), 'Project metadata saved.');
}
async function saveSettings() {
    if (!preferences.value || !settingsData.value) return;
    const input = { ...preferences.value, version: settingsData.value.version };
    const isCurrent = captureContext();
    await mutate(projectId => projectsApi.updateSettings(projectId, input), 'Project settings saved.');
    // Refresh settings' independent concurrency domain even after a conflict; keep edits.
    if (isCurrent()) await loadSettings();
}
async function execute() {
    if (!data.value || !confirmation.value) return;
    const action = confirmation.value;
    const version = data.value.project.version;
    const target = candidate.value?.userId;
    const retained = retainedRole.value;
    const slug = confirmationSlug.value;
    if (action === 'ownership' && !target) return;
    if (action === 'delete' && slug !== data.value.project.slug) return;
    await mutate(projectId => {
        if (action === 'archive') return projectsApi.archive(projectId, version);
        if (action === 'restore') return projectsApi.restore(projectId, version);
        if (action === 'delete') return projectsApi.requestDeletion(projectId, { version, confirmationSlug: slug });
        return projectsApi.transferOwnership(projectId, { newOwnerUserId: target!, retainCurrentOwnerAs: retained, version });
    }, action === 'delete' ? 'Deletion request accepted. Status: deleting. Data is retained pending the documented retention process.' : `Project ${action === 'ownership' ? 'ownership transferred' : action === 'archive' ? 'archived' : 'restored'}.`, () => { confirmation.value = ''; candidate.value = undefined; });
}
</script>
<template>
    <ResourceState :loading="loading" :error="error" @retry="load">
        <template v-if="data">
            <nav aria-label="Breadcrumb" class="text-sm text-cyan-300"><router-link :to="{ name: 'organizationProjects', params: { organizationId: data.org.id } }">{{ data.org.name }} / Projects</router-link></nav>
            <h1 class="mt-4 break-words text-3xl font-semibold">{{ data.project.name }} · Settings</h1>
            <ProjectSectionNav :project="data.project" />
            <p class="my-4">Status: {{ data.project.status }} · Project version {{ data.project.version }}</p>
            <p v-if="actionError || validation" role="alert" class="my-4 text-rose-300">{{ actionError || validation }}</p>
            <p v-if="success" role="status" class="my-4 text-cyan-300">{{ success }}</p>
            <p v-if="!manager" class="workspace-panel">You do not have project settings permission.</p>
            <template v-else>
                <nav aria-label="Settings sections" class="my-5 flex flex-wrap gap-4 text-cyan-300"><a href="#general">General</a><a href="#analysis">Analysis &amp; Preferences</a><a v-if="owner" href="#ownership">Ownership</a><a href="#lifecycle">Lifecycle</a><a v-if="owner" href="#danger">Danger Zone</a></nav>
                <section id="general" class="workspace-panel my-5"><h2 class="text-xl font-semibold">General settings</h2>
                    <form class="mt-5 space-y-4" @submit.prevent="saveGeneral"><fieldset :disabled="busy || !mutable" class="space-y-4">
                        <label class="block">Project name<input v-model="general.name" class="workspace-input" required maxlength="80" /></label>
                        <label class="block">Project slug<input v-model="general.slug" class="workspace-input" required maxlength="80" /></label>
                        <label class="block">Project description<textarea v-model="general.description" class="workspace-input" rows="4" /></label>
                        <label class="block">Project visibility<select v-model="general.visibility" class="workspace-input"><option>private</option><option>internal</option><option>public</option></select></label>
                        <button class="workspace-button">Save general settings</button>
                    </fieldset></form>
                </section>
                <section id="analysis" class="workspace-panel my-5"><h2 class="text-xl font-semibold">Analysis &amp; Preferences</h2>
                    <ResourceState :loading="settingsLoading" :error="settingsError" @retry="loadSettings">
                        <form v-if="preferences && settingsData" class="mt-5 space-y-4" @submit.prevent="saveSettings"><fieldset :disabled="busy || !mutable" class="space-y-4">
                            <p>Analysis profile: {{ settingsData.analysisProfileKey }}</p><p class="text-sm text-slate-400">The current analysis profile is managed by the platform.</p>
                            <label class="flex items-center gap-3"><input v-model="preferences.aiAdvisoryEnabled" type="checkbox" /> Enable AI advisory preference</label><p class="text-sm text-slate-400">AI analysis is not available yet.</p>
                            <label class="block">Default asset visibility<select v-model="preferences.defaultAssetVisibility" class="workspace-input"><option>private</option><option>internal</option></select></label>
                            <p>Notification policy version: {{ settingsData.notificationPolicyVersion }}</p>
                            <p class="text-sm text-slate-400">Source: {{ settingsData.sourceScope }} · Settings version {{ settingsData.version }}</p>
                            <button class="workspace-button">Save analysis preferences</button>
                        </fieldset></form>
                    </ResourceState>
                </section>
                <section v-if="owner" id="ownership" class="workspace-panel my-5"><h2 class="text-xl font-semibold">Ownership transfer</h2><p class="my-3 text-slate-400">Your role: {{ data.project.currentUserRole }}. Select another active project member. Their account and organization membership must remain active.</p>
                    <template v-if="mutable"><ProjectMemberPicker :key="identity" :organization-id="data.org.id" :project-id="id" source="project" :exclude-user-id="auth.user?.id" @select="candidate = $event; confirmation = ''" />
                        <div v-if="candidate" class="space-y-4"><p>New owner: {{ candidate.displayName }} · {{ candidate.email }}</p><label class="block">Your retained role<select v-model="retainedRole" class="workspace-input"><option>maintainer</option><option>developer</option><option>reviewer</option><option>viewer</option></select></label><button class="workspace-button" :disabled="busy" @click="confirmation = 'ownership'">Review ownership transfer</button></div>
                    </template>
                </section>
                <section id="lifecycle" class="workspace-panel my-5"><h2 class="text-xl font-semibold">Archive and restore</h2><p class="my-4 text-slate-400">Archived projects remain readable. Restore allows changes again and preserves revoked memberships.</p><button v-if="mutable" class="workspace-button" :disabled="busy" @click="confirmation = 'archive'">Archive project</button><button v-if="data.project.status === 'archived' && data.org.status === 'active'" class="workspace-button" :disabled="busy" @click="confirmation = 'restore'">Restore project</button></section>
                <section v-if="owner" id="danger" class="workspace-panel my-5 border-rose-800"><h2 class="text-xl font-semibold text-rose-300">Danger Zone</h2><p class="my-4">Deletion requests retain the project in deleting status. This does not permanently delete retained data or audit history.</p><template v-if="['active', 'archived'].includes(data.project.status)"><label class="block">Confirm project slug<input v-model="confirmationSlug" class="workspace-input" :placeholder="data.project.slug" /></label><button class="workspace-button mt-4" :disabled="busy || confirmationSlug !== data.project.slug" @click="confirmation = 'delete'">Request project deletion</button></template></section>
                <WorkspaceActionPanel v-if="confirmation" aria-label="Confirm project action"><h2 class="text-xl font-semibold">Confirm {{ confirmation }}</h2><p class="my-4">{{ confirmation === 'ownership' ? `Transfer ownership to ${candidate?.displayName} and retain your role as ${retainedRole}?` : `Apply ${confirmation} to ${data.project.name}?` }}</p><button class="workspace-button" :disabled="busy" @click="execute">Confirm {{ confirmation }}</button><button class="workspace-button ml-3" :disabled="busy" @click="confirmation = ''">Cancel</button></WorkspaceActionPanel>
            </template>
        </template>
    </ResourceState>
</template>
