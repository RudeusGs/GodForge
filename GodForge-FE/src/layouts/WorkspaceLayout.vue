<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth.store';
import GodForgeMark from '@/components/auth/GodForgeMark.vue';
import OrganizationSwitcher from '@/features/workspace/OrganizationSwitcher.vue';
const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const navigationOpen = ref(false);
const main = ref<HTMLElement>();
onMounted(() => main.value?.focus());
const selectedOrganization = computed(() => typeof route.params.organizationId === 'string' ? route.params.organizationId : '');
watch(() => route.fullPath, async () => { navigationOpen.value = false; await nextTick(); main.value?.focus(); });
async function logout() {
    try { await auth.logout(); } catch { /* Sign-in explains incomplete server revocation. */ }
    await router.replace({ name: 'login', query: { authReason: auth.lastAuthClearReason ?? 'logout' } });
}
function closeUserMenu(event: KeyboardEvent) {
    const menu = event.currentTarget as HTMLDetailsElement;
    menu.open = false;
    menu.querySelector('summary')?.focus();
}
</script>
<template>
    <div class="godforge-bg min-h-screen text-slate-100">
        <a href="#workspace-main" class="sr-only focus:not-sr-only focus:fixed focus:z-50 focus:bg-slate-900 focus:p-4">Skip to workspace</a>
        <header class="flex min-h-20 flex-wrap items-center justify-between gap-4 border-b border-slate-800 px-4 py-4 sm:px-8">
            <router-link to="/dashboard" class="flex items-center gap-3 font-semibold tracking-wide"><GodForgeMark compact />GodForge</router-link>
            <button class="workspace-button md:hidden" :aria-expanded="navigationOpen" aria-controls="workspace-navigation" @click="navigationOpen = !navigationOpen">Navigation</button>
            <details class="relative" @keydown.esc="closeUserMenu">
                <summary class="workspace-button max-w-64 cursor-pointer break-words">{{ auth.user?.displayName || 'Account' }}</summary>
                <div class="absolute right-0 z-30 mt-2 w-64 rounded-xl border border-slate-700 bg-slate-900 p-4 shadow-xl">
                    <p class="mb-3 break-all text-sm text-slate-400">{{ auth.user?.email }}</p>
                    <router-link class="block rounded p-2 hover:bg-slate-800" :to="{ name: 'accountSessions' }">Security sessions</router-link>
                    <router-link class="block rounded p-2 hover:bg-slate-800" :to="{ name: 'accountPreferences' }">Preferences</router-link>
                    <button class="mt-2 w-full rounded p-2 text-left text-rose-300 hover:bg-slate-800" @click="logout">Logout</button>
                </div>
            </details>
        </header>
        <div class="mx-auto flex max-w-screen-2xl flex-col md:flex-row">
            <aside id="workspace-navigation" :class="navigationOpen ? 'block' : 'hidden'" class="w-full shrink-0 border-b border-slate-800 p-5 md:block md:min-h-[calc(100vh-5rem)] md:w-72 md:border-r md:border-b-0">
                <OrganizationSwitcher :selected="selectedOrganization" />
                <nav aria-label="Workspace" class="mt-8 space-y-2">
                    <router-link class="block rounded-lg px-3 py-3 hover:bg-slate-800" active-class="" exact-active-class="bg-slate-800 text-cyan-300" :to="{ name: 'dashboard' }">Organizations</router-link>
                    <template v-if="selectedOrganization">
                        <router-link class="block rounded-lg px-3 py-2.5 text-sm hover:bg-slate-800" active-class="" exact-active-class="bg-slate-800 text-cyan-300" :to="{ name: 'organizationOverview', params: { organizationId: selectedOrganization } }">Overview</router-link>
                        <router-link class="block rounded-lg px-3 py-2.5 text-sm hover:bg-slate-800" :class="route.name === 'organizationProjects' || route.name === 'projectDetail' ? 'bg-slate-800 text-cyan-300' : ''" :to="{ name: 'organizationProjects', params: { organizationId: selectedOrganization } }">Projects</router-link>
                        <router-link class="block rounded-lg px-3 py-2.5 text-sm hover:bg-slate-800" active-class="" exact-active-class="bg-slate-800 text-cyan-300" :to="{ name: 'organizationMembers', params: { organizationId: selectedOrganization } }">Members</router-link>
                        <router-link class="block rounded-lg px-3 py-2.5 text-sm hover:bg-slate-800" active-class="" exact-active-class="bg-slate-800 text-cyan-300" :to="{ name: 'organizationInvitations', params: { organizationId: selectedOrganization } }">Invitations</router-link>
                        <router-link class="block rounded-lg px-3 py-2.5 text-sm hover:bg-slate-800" active-class="" exact-active-class="bg-slate-800 text-cyan-300" :to="{ name: 'organizationSettings', params: { organizationId: selectedOrganization } }">Settings</router-link>
                    </template>
                    <router-link class="block rounded-lg px-3 py-3 hover:bg-slate-800" :to="{ name: 'accountSessions' }">Account security</router-link>
                    <router-link class="block rounded-lg px-3 py-3 hover:bg-slate-800" :to="{ name: 'accountPreferences' }">Account preferences</router-link>
                </nav>
                <p class="mt-10 px-3 text-xs leading-5 text-slate-500">Workspace foundation<br />Organization and project access</p>
            </aside>
            <main id="workspace-main" ref="main" tabindex="-1" class="min-w-0 flex-1 px-4 py-8 outline-none sm:px-8"><router-view /></main>
        </div>
    </div>
</template>
