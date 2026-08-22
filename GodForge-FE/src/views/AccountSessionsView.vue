<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { authApi } from '@/api/auth/auth.api';
import type { SessionDto } from '@/api/auth/auth.models';
import { useAuthStore } from '@/stores/auth.store';

const router = useRouter();
const authStore = useAuthStore();
const sessions = ref<SessionDto[]>([]);
const loading = ref(true);
const errorMessage = ref('');
const revokingId = ref<string | null>(null);

const loadSessions = async () => {
    loading.value = true;
    errorMessage.value = '';
    try {
        const response = await authApi.getSessions();
        sessions.value = response.data;
    } catch {
        errorMessage.value = 'Sessions could not be loaded. Please try again.';
    } finally {
        loading.value = false;
    }
};

const revokeSession = async (session: SessionDto) => {
    revokingId.value = session.id;
    errorMessage.value = '';
    try {
        await authApi.revokeSession(session.id);
        if (session.current) {
            authStore.invalidateAuthentication('session-revoked');
            await router.push({ name: 'login' });
            return;
        }
        await loadSessions();
    } catch {
        errorMessage.value = 'The session could not be revoked. Refresh the list and try again.';
    } finally {
        revokingId.value = null;
    }
};

const formatDate = (value: string | null) => value
    ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
    : 'Never';

onMounted(loadSessions);
</script>

<template>
    <main class="min-h-screen bg-slate-950 px-4 py-10 text-slate-100">
        <section class="mx-auto max-w-4xl">
            <div class="mb-8 flex flex-wrap items-center justify-between gap-4">
                <div>
                    <p class="text-sm font-medium text-cyan-400">Account security</p>
                    <h1 class="mt-1 text-3xl font-semibold">Active and recent sessions</h1>
                    <p class="mt-2 text-sm text-slate-400">Review devices and revoke access you no longer recognize.</p>
                </div>
                <router-link :to="{ name: 'dashboard' }" class="rounded-lg border border-slate-700 px-4 py-2 hover:bg-slate-900">
                    Back to dashboard
                </router-link>
            </div>

            <div v-if="errorMessage" role="alert" class="mb-5 flex items-center justify-between gap-4 rounded-lg border border-red-800 bg-red-950/40 p-4 text-red-200">
                <span>{{ errorMessage }}</span>
                <button type="button" class="underline" @click="loadSessions">Retry</button>
            </div>

            <div v-if="loading" aria-live="polite" class="space-y-3">
                <div v-for="index in 3" :key="index" class="h-28 animate-pulse rounded-xl border border-slate-800 bg-slate-900" />
                <span class="sr-only">Loading sessions</span>
            </div>

            <div v-else-if="sessions.length === 0" class="rounded-xl border border-slate-800 bg-slate-900 p-8 text-center text-slate-400">
                No recent sessions are available.
            </div>

            <ul v-else class="space-y-3" aria-label="Account sessions">
                <li v-for="session in sessions" :key="session.id" class="rounded-xl border border-slate-800 bg-slate-900 p-5">
                    <div class="flex flex-wrap items-start justify-between gap-4">
                        <div>
                            <div class="flex items-center gap-2">
                                <h2 class="font-semibold text-slate-100">{{ session.deviceName || 'Unknown device' }}</h2>
                                <span v-if="session.current" class="rounded-full bg-emerald-950 px-2 py-1 text-xs text-emerald-300">Current</span>
                                <span v-else-if="session.revokedAt" class="rounded-full bg-slate-800 px-2 py-1 text-xs text-slate-400">Revoked</span>
                            </div>
                            <dl class="mt-3 grid gap-x-8 gap-y-1 text-sm text-slate-400 sm:grid-cols-2">
                                <div><dt class="inline text-slate-500">Created:</dt> <dd class="inline">{{ formatDate(session.createdAt) }}</dd></div>
                                <div><dt class="inline text-slate-500">Last refreshed:</dt> <dd class="inline">{{ formatDate(session.lastSeenAt) }}</dd></div>
                                <div><dt class="inline text-slate-500">Expires:</dt> <dd class="inline">{{ formatDate(session.expiresAt) }}</dd></div>
                            </dl>
                        </div>
                        <button
                            v-if="!session.revokedAt"
                            type="button"
                            :disabled="revokingId !== null"
                            class="rounded-lg border border-red-800 px-4 py-2 text-sm text-red-300 hover:bg-red-950/50 disabled:cursor-not-allowed disabled:opacity-50"
                            :aria-label="`Revoke session for ${session.deviceName || 'unknown device'}`"
                            @click="revokeSession(session)"
                        >
                            {{ revokingId === session.id ? 'Revoking…' : session.current ? 'Sign out this session' : 'Revoke' }}
                        </button>
                    </div>
                </li>
            </ul>
        </section>
    </main>
</template>
