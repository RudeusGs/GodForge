<script setup lang="ts">
import { computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from './stores/auth.store';
import AppLoading from './components/AppLoading.vue';
import { settingsApi } from './api/settings.api';
import { applyTheme, themeRevision } from './features/theme';

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();
let themeGeneration = 0;
watch(() => authStore.user?.id, async userId => {
    const current = ++themeGeneration;
    applyTheme('dark');
    const initialRevision = themeRevision;
    if (!userId) return;
    try {
        const preferences = (await settingsApi.getPreferences()).data;
        if (current === themeGeneration && initialRevision === themeRevision) applyTheme(preferences.theme);
    } catch { /* The preferences screen provides retry; a theme fetch never changes authentication. */ }
}, { immediate: true });
const isBootstrapping = computed(() => !authStore.initialized || (route.meta.requiresAuth && !authStore.isAuthenticated));

watch([() => authStore.isAuthenticated, () => authStore.initialized], ([authenticated, initialized]) => {
    if (!authenticated && initialized && route.meta.requiresAuth) {
        void router.replace({ name: 'login', query: { authReason: authStore.lastAuthClearReason ?? 'session-revoked', returnTo: route.fullPath } });
    }
});
</script>

<template>
    <router-view v-slot="{ Component }">
        <Transition name="app-ready" mode="out-in">
            <AppLoading v-if="isBootstrapping" key="loading" />
            <component :is="Component" v-else :key="route.path" />
        </Transition>
    </router-view>
</template>

<style>
.app-ready-leave-active { transition: opacity 240ms ease; }.app-ready-leave-to { opacity: 0; }
@media (prefers-reduced-motion: reduce) { .app-ready-leave-active { transition: none; } }
</style>
