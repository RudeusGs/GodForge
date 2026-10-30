<script setup lang="ts">
import { computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from './stores/auth.store';
import AppLoading from './components/AppLoading.vue';

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();
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
