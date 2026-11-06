<script setup lang="ts">
import { computed, onScopeDispose, ref, watch } from 'vue';
import { repositoryApi } from '@/api/repository/repository.api';
import type { RepositoryDto, RepositoryBranch, RepositoryCommit, RepositoryTreeEntry, RepositoryBlob } from '@/api/repository/repository.models';
import { useProjectWorkspace } from '@/features/workspace/projectWorkspace';
import { extractErrorCode, extractStatus, workspaceError } from '@/features/workspace/workspaceState';
import ResourceState from '@/components/workspace/ResourceState.vue';
import ProjectSectionNav from '@/components/workspace/ProjectSectionNav.vue';

const workspace = useProjectWorkspace();
const { data, loading, error, load } = workspace;
const repository = ref<RepositoryDto>();
const repositoryLoading = ref(false);
const message = ref('');
const busy = ref(false);
const operationJob = ref('');
const operationStatus = ref('');
const protection = ref<boolean>();
let pollTimer: ReturnType<typeof setTimeout> | undefined;
const name = ref('');
const defaultBranch = ref('main');
const remoteUrl = ref('');
const token = ref('');
const branches = ref<RepositoryBranch[]>([]);
const branch = ref('');
const branchPage = ref(1);
const commits = ref<RepositoryCommit[]>([]);
const commitPage = ref(1);
const revision = ref('');
const path = ref('');
const entries = ref<RepositoryTreeEntry[]>([]);
const blob = ref<RepositoryBlob>();
const tab = ref<'files' | 'commits' | 'settings'>('files');
let generation = 0;
let browseGeneration = 0;
let disposed = false;
const currentId = computed(() => data.value?.project.id ?? '');
const canManage = computed(() => workspace.manager.value && workspace.mutable.value);
const canSync = computed(() => workspace.mutable.value && ['projectOwner', 'maintainer', 'developer'].includes(data.value?.project.currentUserRole ?? ''));
const providerLink = computed(() => {
    if (repository.value?.mode !== 'InternalHosted') return '';
    try { const url = new URL(repository.value.cloneUrl); return url.protocol === 'https:' || url.protocol === 'http:' && ['localhost', '127.0.0.1'].includes(url.hostname) ? url.href.replace(/\.git$/, '') : ''; } catch { return ''; }
});
const accessPending = computed(() => repository.value?.accessErrorCode || repository.value?.accessRequestedAt &&
    (repository.value.accessGeneration !== undefined ? repository.value.accessGeneration > (repository.value.accessConfirmedGeneration ?? 0) :
    !repository.value.accessReconciledAt || repository.value.accessRequestedAt > repository.value.accessReconciledAt));
const breadcrumbs = computed(() => path.value ? path.value.split('/').map((label, i, all) => ({ label, path: all.slice(0, i + 1).join('/') })) : []);

function clear() {
    clearTimeout(pollTimer); operationJob.value = ''; operationStatus.value = ''; protection.value = undefined; branchPage.value = 1; tab.value = 'files';
    generation++; browseGeneration++; repository.value = undefined; branches.value = []; commits.value = [];
    entries.value = []; blob.value = undefined; path.value = ''; branch.value = ''; revision.value = ''; token.value = '';
    name.value = ''; remoteUrl.value = ''; defaultBranch.value = 'main'; message.value = ''; busy.value = false;
}
function schedulePoll() {
    clearTimeout(pollTimer);
    if (!disposed && currentId.value && (operationJob.value || accessPending.value || ['ProvisioningPending', 'Provisioning'].includes(repository.value?.status ?? '')))
        pollTimer = setTimeout(() => void pollMetadata(), 2000);
}
watch([operationJob, accessPending, () => repository.value?.status], schedulePoll);
async function pollMetadata() {
    const id = currentId.value; const epoch = generation;
    try {
        let complete = false;
        if (operationJob.value) {
            const job = (await repositoryApi.operation(id, operationJob.value)).data;
            if (disposed || epoch !== generation || id !== currentId.value) return;
            operationStatus.value = job.status;
            complete = ['Completed', 'Failed', 'DeadLettered', 'Cancelled', 'Timeout'].includes(job.status);
            if (complete) { operationJob.value = ''; if (job.errorCode) message.value = job.errorCode; }
        }
        const latest = (await repositoryApi.get(id)).data;
        if (disposed || epoch !== generation || id !== currentId.value) return;
        const becameReady = repository.value?.status !== 'Ready' && latest.status === 'Ready';
        repository.value = latest;
        if ((complete || becameReady) && latest.status === 'Ready') await loadBranches();
    } catch (cause) {
        if (!disposed && epoch === generation) {
            message.value = workspaceError(cause);
            if ([401, 403, 404].includes(extractStatus(cause) ?? 0)) { repository.value = undefined; entries.value = []; blob.value = undefined; commits.value = []; operationJob.value = ''; }
        }
    } finally { schedulePoll(); }
}
watch(currentId, () => { clear(); if (currentId.value) { name.value = data.value?.project.slug ?? ''; void refresh(); } });
onScopeDispose(() => { disposed = true; clear(); });
async function refresh() {
    const id = currentId.value; const epoch = ++generation;
    repositoryLoading.value = true; message.value = '';
    try {
        const result = (await repositoryApi.get(id)).data;
        if (disposed || epoch !== generation || id !== currentId.value) return;
        repository.value = result;
        if (result.status === 'Ready') await loadBranches();
    } catch (cause) {
        if (epoch === generation && !disposed && extractErrorCode(cause) !== 'REPOSITORY_NOT_CONNECTED') message.value = workspaceError(cause);
    } finally { if (epoch === generation && !disposed) repositoryLoading.value = false; }
}
async function action(operation: (id: string) => Promise<unknown>) {
    if (busy.value) return;
    const id = currentId.value; const epoch = generation;
    busy.value = true; message.value = '';
    try { await operation(id); if (!disposed && epoch === generation && id === currentId.value) await refresh(); }
    catch (cause) { if (!disposed && epoch === generation) message.value = workspaceError(cause); }
    finally { if (!disposed && id === currentId.value) busy.value = false; token.value = ''; }
}
function provision() {
    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(name.value) || name.value.length > 60) { message.value = 'Use 1–60 lowercase letters, digits and interior hyphens.'; return; }
    return action(async id => { const result = await repositoryApi.provision(id, { name: name.value, defaultBranch: defaultBranch.value, isPrivate: true, autoAnalyzeEnabled: false }); if (!disposed && id === currentId.value) operationJob.value = result.data.jobId; return result; });
}
async function loadBranches() {
    const id = currentId.value; const epoch = ++browseGeneration;
    try {
        const result = (await repositoryApi.branches(id, branchPage.value)).data;
        if (disposed || epoch !== browseGeneration || id !== currentId.value) return;
        branches.value = result;
        branch.value = result.find(b => b.isDefault)?.name ?? result[0]?.name ?? '';
        await selectBranch();
    } catch (cause) { if (!disposed && epoch === browseGeneration) message.value = workspaceError(cause); }
}
async function selectBranch() {
    browseGeneration++; commits.value = []; entries.value = []; blob.value = undefined; path.value = ''; revision.value = '';
    commitPage.value = 1;
    const selected = branches.value.find(b => b.name === branch.value);
    if (selected) { revision.value = selected.commitSha; await openDirectory(''); await loadCommits(); }
    protection.value = selected?.isProtected;
}
async function loadCommits() {
    const id = currentId.value; const epoch = browseGeneration; const selected = branch.value; const page = commitPage.value;
    try {
        const result = (await repositoryApi.commits(id, selected, page)).data;
        if (!disposed && epoch === browseGeneration && selected === branch.value && page === commitPage.value) commits.value = result;
    } catch (cause) { if (!disposed && epoch === browseGeneration) message.value = workspaceError(cause); }
}
async function openDirectory(directory: string) {
    const id = currentId.value; const epoch = ++browseGeneration; const sha = revision.value;
    path.value = directory; entries.value = []; blob.value = undefined; message.value = '';
    try {
        const result = (await repositoryApi.tree(id, sha, directory)).data;
        if (!disposed && epoch === browseGeneration && id === currentId.value) entries.value = result.entries;
    } catch (cause) { if (!disposed && epoch === browseGeneration) message.value = workspaceError(cause); }
}
async function openFile(entry: RepositoryTreeEntry) {
    if (entry.type === 'tree') return openDirectory(entry.path);
    if (entry.type !== 'blob') { message.value = 'Symlinks and submodules cannot be displayed.'; return; }
    const epoch = ++browseGeneration; const id = currentId.value; blob.value = undefined; message.value = '';
    try {
        const result = (await repositoryApi.blob(id, revision.value, entry.path)).data;
        if (!disposed && epoch === browseGeneration && id === currentId.value) blob.value = result;
    } catch (cause) {
        if (!disposed && epoch === browseGeneration) message.value = extractErrorCode(cause) === 'REPOSITORY_BINARY_FILE' ? 'Binary files cannot be displayed. Use your Git client to download this file.' : workspaceError(cause);
    }
}
async function copyCloneUrl() {
    try { await navigator.clipboard.writeText(repository.value?.cloneUrl ?? ''); message.value = 'Clone URL copied.'; }
    catch { message.value = 'Clipboard unavailable. Select and copy the clone URL.'; }
}
function retry() {
    if (!repository.value) return;
    const repo = repository.value;
    return action(async id => { const result = await repositoryApi.provision(id, { name: name.value, defaultBranch: repo.defaultBranch, isPrivate: true, autoAnalyzeEnabled: repo.autoAnalyzeEnabled }); if (!disposed && id === currentId.value) operationJob.value = result.data.jobId; return result; });
}
function synchronize() {
    return action(async id => { const result = await repositoryApi.synchronize(id); if (!disposed && id === currentId.value) operationJob.value = result.data.jobId; return result; });
}
function checkProtection() {
    const epoch = generation; const selected = branch.value;
    return action(async id => {
        const result = await repositoryApi.protection(id, selected);
        if (!disposed && epoch === generation && id === currentId.value && selected === branch.value) protection.value = result.data.enabled;
    });
}
</script>

<template>
    <ResourceState :loading="loading" :error="error" @retry="load">
        <template v-if="data">
            <h1 class="text-3xl font-semibold">{{ data.project.name }} · Repository</h1>
            <ProjectSectionNav :project="data.project" />
            <p v-if="message" role="status" class="my-4 break-words text-amber-200">{{ message }}</p>
            <p v-if="repositoryLoading" role="status" class="animate-pulse text-slate-400">Loading repository…</p>
            <section v-else-if="!repository" class="workspace-panel space-y-6">
                <h2 class="text-xl font-semibold">Connect your source</h2>
                <template v-if="canManage">
                    <form class="grid max-w-xl gap-4" @submit.prevent="provision">
                        <h3 class="font-semibold">Create a private hosted repository</h3>
                        <label>Repository name<input v-model="name" class="workspace-input mt-1 w-full" required maxlength="60" pattern="[a-z0-9]+(-[a-z0-9]+)*" /></label>
                        <label>Default branch<input v-model="defaultBranch" class="workspace-input mt-1 w-full" required maxlength="150" /></label>
                        <p class="text-sm text-slate-400">Private visibility. Automatic analysis is disabled for this repository foundation.</p>
                        <button class="workspace-button" :disabled="busy">Create hosted repository</button>
                    </form>
                    <form class="grid max-w-xl gap-4 border-t border-slate-700 pt-6" @submit.prevent="action(id => repositoryApi.link(id, { remoteUrl, provider: 'Generic', defaultBranch, autoAnalyzeEnabled: false }))">
                        <h3 class="font-semibold">Link an external repository</h3>
                        <label>HTTPS Git URL<input v-model="remoteUrl" class="workspace-input mt-1 w-full" type="url" required maxlength="800" /></label>
                        <button class="workspace-button" :disabled="busy">Link repository</button>
                    </form>
                </template>
                <p v-else>A project owner or maintainer can configure this repository.</p>
            </section>
            <template v-else>
                <section class="workspace-panel space-y-4">
                    <div class="flex flex-wrap items-center justify-between gap-3"><h2 class="text-xl font-semibold">{{ repository.mode === 'InternalHosted' ? 'Hosted Git' : 'Linked Git' }}</h2><span role="status">{{ repository.status }}</span><button class="workspace-button" @click="refresh">Refresh</button></div>
                    <p v-if="repository.lastErrorCode" role="alert">{{ repository.lastErrorCode }}</p>
                    <p v-if="accessPending" role="status" class="text-amber-200">Provider access reconciliation pending{{ repository.accessErrorCode ? `: ${repository.accessErrorCode}` : '' }}.</p>
                    <p v-else-if="repository.mode === 'InternalHosted' && repository.accessReconciledAt" role="status">Provider permissions confirmed.</p>
                    <p v-if="operationStatus" role="status">Operation: {{ operationStatus }}</p>
                    <a v-if="providerLink" :href="providerLink" target="_blank" rel="noopener noreferrer" class="text-cyan-300 underline">Open in Forgejo</a>
                    <button v-if="canSync" class="workspace-button" :disabled="busy || !!operationJob" @click="synchronize">Synchronize repository</button>
                    <div v-if="repository.cloneUrl" class="flex flex-wrap items-center gap-3"><code class="break-all text-sm">{{ repository.cloneUrl }}</code><button class="workspace-button" @click="copyCloneUrl">Copy clone URL</button></div>
                    <p v-if="repository.currentCommitHash" class="break-all text-sm">Latest revision: {{ repository.currentCommitHash }}</p>
                    <p v-if="repository.lastSyncedAt" class="text-sm text-slate-400">Last synchronized: {{ new Date(repository.lastSyncedAt).toLocaleString() }}</p>
                    <p v-if="repository.status === 'ProvisioningPending' || repository.status === 'Provisioning'" role="status">Provisioning is queued or running. Refresh to check its durable status.</p>
                    <template v-if="repository.status === 'ProvisioningFailed' && canManage"><label>Original repository name<input v-model="name" class="workspace-input" maxlength="60" /></label><button class="workspace-button" :disabled="busy" @click="retry">Retry provisioning</button></template>
                </section>
                <section v-if="repository.status === 'Ready'" class="workspace-panel mt-6 space-y-4">
                    <nav aria-label="Repository views" class="flex flex-wrap gap-3"><button v-for="section in (['files', 'commits', 'settings'] as const)" :key="section" class="workspace-button" :aria-pressed="tab === section" @click="tab = section">{{ section }}</button></nav>
                    <label v-if="branches.length">Branch <select v-model="branch" class="workspace-input" @change="selectBranch"><option v-for="item in branches" :key="item.name" :value="item.name">{{ item.name }}{{ item.isProtected ? ' (protected)' : '' }}</option></select></label>
                    <div class="flex gap-3"><button v-if="branchPage > 1" class="workspace-button" @click="branchPage--; loadBranches()">Previous branches</button><button v-if="branches.length === 100" class="workspace-button" @click="branchPage++; loadBranches()">More branches</button></div>
                    <p v-if="!branches.length" class="text-slate-400">No branches yet. Bind your Forgejo account, then clone and push your first commit.</p>
                    <template v-if="tab === 'files' && branch">
                        <nav aria-label="Repository path" class="flex flex-wrap gap-2"><button class="text-cyan-300 underline" @click="openDirectory('')">Root</button><button v-for="crumb in breadcrumbs" :key="crumb.path" class="text-cyan-300 underline" @click="openDirectory(crumb.path)">/ {{ crumb.label }}</button></nav>
                        <p class="break-all text-xs text-slate-400">Revision {{ revision }}</p>
                        <ul class="divide-y divide-slate-700"><li v-for="entry in entries" :key="entry.path"><button class="flex w-full items-center justify-between gap-3 py-3 text-left hover:text-cyan-300 focus-visible:outline" @click="openFile(entry)"><span class="break-all">{{ entry.type === 'tree' ? '📁' : '📄' }} {{ entry.name }}</span><span class="shrink-0 text-xs text-slate-400">{{ entry.type === 'blob' ? `${entry.size} bytes` : entry.type }}</span></button></li></ul>
                        <article v-if="blob"><h3 class="mb-3 break-all font-semibold">{{ blob.path }} · {{ blob.size }} bytes</h3><pre class="max-h-[65vh] overflow-auto rounded border border-slate-700 bg-slate-950 p-4 text-sm"><code>{{ blob.content }}</code></pre></article>
                    </template>
                    <template v-if="tab === 'commits' && branch"><ol class="divide-y divide-slate-700"><li v-for="commit in commits" :key="commit.sha" class="py-4"><p class="whitespace-pre-wrap break-words">{{ commit.message }}</p><p class="mt-2 break-all text-sm text-slate-400">{{ commit.shortSha }} · {{ commit.author }} · {{ new Date(commit.timestamp).toLocaleString() }}</p><button class="text-sm text-cyan-300 underline" @click="revision = commit.sha; tab = 'files'; openDirectory('')">Browse revision</button></li></ol><div class="flex gap-3"><button class="workspace-button" :disabled="commitPage === 1" @click="commitPage--; loadCommits()">Previous</button><button class="workspace-button" :disabled="commits.length < 20" @click="commitPage++; loadCommits()">Next</button></div></template>
                    <template v-if="tab === 'settings'">
                        <form v-if="repository.mode === 'InternalHosted'" class="grid max-w-xl gap-4" @submit.prevent="action(() => repositoryApi.bindIdentity(token))"><h3 class="font-semibold">Bind your Forgejo identity</h3><p class="text-sm text-slate-400">Use a personal token with read:user permission from your own Forgejo account. GodForge verifies the account and discards the token. Revoke it after binding.</p><label>Personal verification token<input v-model="token" class="workspace-input mt-1 w-full" type="password" autocomplete="off" required maxlength="1000" /></label><button class="workspace-button" :disabled="busy">Verify account</button></form>
                        <template v-if="repository.mode === 'InternalHosted' && branch"><p>Branch protection: {{ protection ? 'enabled' : 'disabled' }}</p><button class="workspace-button" @click="checkProtection">Check protection</button><div v-if="canManage" class="mt-6 flex flex-wrap gap-3"><button class="workspace-button" :disabled="busy" @click="action(id => repositoryApi.protect(id, branch, true))">Protect selected branch</button><button class="workspace-button" :disabled="busy" @click="action(id => repositoryApi.protect(id, branch, false))">Remove protection</button></div></template>
                        <p v-else-if="repository.mode === 'ExternalLinked'">Anonymous HTTPS, read-only cached revisions. Synchronize to fetch changes. Private-provider credentials are not supported.</p>
                    </template>
                </section>
            </template>
        </template>
    </ResourceState>
</template>
