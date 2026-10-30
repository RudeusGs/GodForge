<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { organizationsApi } from '@/api/workspace/organizations.api';
import type { OrganizationInvitationAcceptance } from '@/api/workspace/workspace.models';
import {
    captureInvitationToken,
    clearPendingInvitationToken,
    extractErrorCode,
    getPendingInvitationToken,
    notifyWorkspaceChanged,
    workspaceError,
} from '@/features/workspace/workspaceState';
import { useAuthStore } from '@/stores/auth.store';

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();

if (route.query.token) {
    captureInvitationToken(route.query.token);
}

const token = ref(getPendingInvitationToken());
const accepting = ref(false);
const errorMsg = ref('');
const terminalTokenError = ref(false);
const accepted = ref<OrganizationInvitationAcceptance | null>(null);

const hasToken = computed(() => Boolean(token.value));

onMounted(async () => {
    if (route.query.token) {
        captureInvitationToken(route.query.token);
        token.value = getPendingInvitationToken();
        await router.replace({ path: route.path, query: {} });
    }
});

async function handleAccept() {
    if (!token.value || accepting.value) return;
    accepting.value = true;
    errorMsg.value = '';

    try {
        const response = await organizationsApi.acceptInvitation({ token: token.value });
        accepted.value = response.data;
        clearPendingInvitationToken();
        token.value = '';
        notifyWorkspaceChanged();
    } catch (cause) {
        errorMsg.value = workspaceError(cause);
        const code = extractErrorCode(cause);
        if (code === 'INVITE_INVALID_OR_EXPIRED') {
            terminalTokenError.value = true;
            clearPendingInvitationToken();
            token.value = '';
        }
    } finally {
        accepting.value = false;
    }
}
</script>

<template>
    <div class="mx-auto max-w-2xl">
        <p class="text-sm text-cyan-400">Organization collaboration</p>
        <h1 class="mt-2 text-3xl font-semibold">Organization invitation</h1>
        <p class="mt-3 text-slate-400">
            Accept an invitation to join a GodForge organization with your verified account.
        </p>

        <section
            v-if="accepted"
            role="status"
            aria-label="Invitation accepted"
            class="workspace-panel mt-6 border-emerald-500/40 bg-emerald-950/20"
        >
            <h2 class="text-xl font-semibold text-emerald-200">
                Joined {{ accepted.organization.name }}
            </h2>
            <p class="mt-2 text-sm text-slate-300">
                Your membership in <strong>{{ accepted.organization.name }}</strong> ({{ accepted.organization.slug }}) is now
                <strong>{{ accepted.membership.status }}</strong> with role
                <strong>{{ accepted.membership.role }}</strong>.
            </p>
            <div class="mt-6 flex flex-wrap gap-3">
                <router-link
                    class="workspace-button text-cyan-300"
                    :to="{
                        name: 'organizationProjects',
                        params: { organizationId: accepted.organization.id },
                    }"
                >
                    Open organization projects
                </router-link>
                <router-link
                    class="workspace-button"
                    :to="{
                        name: 'organizationOverview',
                        params: { organizationId: accepted.organization.id },
                    }"
                >
                    Organization overview
                </router-link>
            </div>
        </section>

        <section v-else class="workspace-panel mt-6">
            <div class="mb-4 text-sm text-slate-300">
                Signed in as <strong>{{ auth.user?.displayName || 'User' }}</strong>
                <span v-if="auth.user?.email" class="text-slate-400">({{ auth.user.email }})</span>
            </div>

            <div
                v-if="errorMsg"
                role="alert"
                class="mb-4 rounded-xl border border-rose-500/40 bg-rose-950/30 p-4 text-sm text-rose-200"
            >
                <p>{{ errorMsg }}</p>
            </div>

            <template v-if="hasToken">
                <p class="text-sm text-slate-300">
                    Confirm below to accept this organization invitation. The invitation email must match your verified account email.
                </p>
                <div class="mt-6 flex flex-wrap gap-3">
                    <button
                        type="button"
                        class="workspace-button text-cyan-300"
                        :disabled="accepting"
                        @click="handleAccept"
                    >
                        {{ accepting ? 'Accepting invitation…' : 'Accept invitation' }}
                    </button>
                    <router-link class="workspace-button" :to="{ name: 'dashboard' }">
                        Return to dashboard
                    </router-link>
                </div>
            </template>

            <template v-else>
                <p v-if="!terminalTokenError" role="alert" class="text-sm text-amber-200">
                    This invitation link is missing its token or has already been processed. Open the invitation link from your email to try again.
                </p>
                <div class="mt-6">
                    <router-link class="workspace-button inline-block" :to="{ name: 'dashboard' }">
                        Return to dashboard
                    </router-link>
                </div>
            </template>
        </section>
    </div>
</template>
