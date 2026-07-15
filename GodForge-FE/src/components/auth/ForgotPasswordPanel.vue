<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '../../stores/auth.store';
import AuthInput from './AuthInput.vue';

const authStore = useAuthStore();

const email = ref('');
const loading = ref(false);
const errorMsg = ref('');
const successMsg = ref('');

const handleForgotPassword = async () => {
    if (!email.value) {
        errorMsg.value = 'Email is required.';
        successMsg.value = '';
        return;
    }

    try {
        loading.value = true;
        errorMsg.value = '';
        successMsg.value = '';
        await authStore.forgotPassword(email.value);
        successMsg.value = 'If the email exists, a new password has been sent to it.';
    } catch (error: unknown) {
        const err = error as { response?: { data?: { error?: { message?: string } } } };
        errorMsg.value = err.response?.data?.error?.message || 'ERR: Failed to process request.';
    } finally {
        loading.value = false;
    }
};
</script>

<template>
    <div class="w-full max-w-md mx-auto p-6 sm:p-10 flex flex-col justify-center bg-[#09090b] relative z-10 h-full overflow-y-auto custom-scrollbar">

        <!-- Header -->
        <div class="mb-8 font-sans">
            <div class="flex items-center space-x-2.5 mb-3">
                <i class="bi bi-hammer text-xl text-cyan-400"></i>
                <h1 class="text-xl font-semibold text-zinc-100 tracking-tight">GodForge</h1>
            </div>
            <p class="text-zinc-400 text-xs leading-relaxed">
                Reset your GodForge account password.
            </p>
        </div>

        <!-- Form -->
        <form @submit.prevent="handleForgotPassword" class="space-y-5 mb-4">

            <!-- Error Message -->
            <transition
                enter-active-class="transition ease-out duration-200"
                enter-from-class="opacity-0 -translate-y-2"
                enter-to-class="opacity-100 translate-y-0"
                leave-active-class="transition ease-in duration-150"
                leave-from-class="opacity-100 translate-y-0"
                leave-to-class="opacity-0 -translate-y-2"
            >
                <div v-if="errorMsg" class="bg-red-500/10 border border-red-500/20 p-3 flex items-start space-x-3 rounded-lg">
                    <i class="bi bi-exclamation-triangle text-red-400 text-sm mt-0.5"></i>
                    <span class="text-red-400 text-xs font-mono">{{ errorMsg }}</span>
                </div>
            </transition>

            <!-- Success Message -->
            <transition
                enter-active-class="transition ease-out duration-200"
                enter-from-class="opacity-0 -translate-y-2"
                enter-to-class="opacity-100 translate-y-0"
                leave-active-class="transition ease-in duration-150"
                leave-from-class="opacity-100 translate-y-0"
                leave-to-class="opacity-0 -translate-y-2"
            >
                <div v-if="successMsg" class="bg-emerald-500/10 border border-emerald-500/20 p-3 flex items-start space-x-3 rounded-lg">
                    <i class="bi bi-check-circle text-emerald-400 text-sm mt-0.5"></i>
                    <span class="text-emerald-400 text-xs font-mono">{{ successMsg }}</span>
                </div>
            </transition>

            <div class="space-y-4">
                <AuthInput
                    v-model="email"
                    label="User Identity"
                    type="email"
                    icon="bi-terminal"
                    placeholder="dev@domain.com"
                />
            </div>

            <div class="pt-2">
                <button
                    type="submit"
                    :disabled="loading"
                    class="w-full bg-cyan-500 hover:bg-cyan-400 active:bg-cyan-600 text-zinc-950 font-sans font-medium text-sm py-2.5 px-4 rounded-lg transition-colors flex items-center justify-center gap-2 group disabled:opacity-50 disabled:cursor-not-allowed shadow-[0_1px_2px_rgba(0,0,0,0.05)]"
                >
                    <span v-if="!loading" class="flex items-center">
                        Reset Password
                        <i class="bi bi-chevron-right ml-1 text-xs opacity-60 group-hover:opacity-100 group-hover:translate-x-0.5 transition-all"></i>
                    </span>
                    <span v-else class="flex items-center space-x-2">
                        <i class="bi bi-arrow-repeat animate-spin"></i>
                        <span>Processing...</span>
                    </span>
                </button>
            </div>

            <div class="text-center pt-5 border-t border-white/5">
                <div class="mt-4">
                    <router-link to="/login" class="text-xs text-zinc-400 hover:text-zinc-200 font-sans underline decoration-white/20 hover:decoration-white/60 transition-colors">
                        Return to Authentication
                    </router-link>
                </div>
            </div>
        </form>
    </div>
</template>

