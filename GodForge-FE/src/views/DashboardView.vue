<script setup lang="ts">
import { useAuthStore } from '@/stores/auth.store';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();

const handleLogout = async () => {
    await authStore.logout();
    await router.push({ name: 'login' });
};
</script>

<template>
    <div class="min-h-screen bg-gray-50 p-8 flex flex-col items-center">
        <div class="max-w-4xl w-full bg-white rounded-lg shadow-sm p-8">
            <div class="flex justify-between items-center mb-8">
                <h1 class="text-2xl font-bold text-gray-900">Dashboard</h1>
                <button
                    @click="handleLogout"
                    class="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 transition-colors"
                >
                    Logout
                </button>
            </div>

            <div class="prose max-w-none">
                <h2>Welcome to GodForge!</h2>
                <p>Hello {{ authStore.user?.email }}, you have successfully logged in.</p>
                <p>This is the initial dashboard placeholder. Project lists and activities will be implemented here.</p>
            </div>
        </div>
    </div>
</template>

