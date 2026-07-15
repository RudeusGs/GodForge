<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { authApi } from '@/api/auth/auth.api';

const route = useRoute();
const router = useRouter();
const email = computed(() => String(route.query.email ?? ''));
const token = computed(() => String(route.query.token ?? ''));
const newPassword = ref('');
const confirmPassword = ref('');
const errorMessage = ref('');
const isSubmitting = ref(false);
const isComplete = ref(false);

const isLinkValid = computed(() => email.value.length > 0 && token.value.length > 0);

const submit = async () => {
    errorMessage.value = '';
    if (!isLinkValid.value) {
        errorMessage.value = 'The password reset link is incomplete or invalid.';
        return;
    }
    if (newPassword.value !== confirmPassword.value) {
        errorMessage.value = 'Password confirmation does not match.';
        return;
    }

    isSubmitting.value = true;
    try {
        await authApi.resetPassword({
            email: email.value,
            token: token.value,
            newPassword: newPassword.value
        });
        isComplete.value = true;
    } catch {
        errorMessage.value = 'The reset link is invalid, expired, or the password does not meet the requirements.';
    } finally {
        isSubmitting.value = false;
    }
};
</script>

<template>
    <main class="min-h-screen bg-slate-950 px-4 py-12 text-slate-100">
        <section class="mx-auto max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-8 shadow-xl">
            <h1 class="text-2xl font-semibold">Reset password</h1>
            <p class="mt-2 text-sm text-slate-400">
                Use at least eight characters with uppercase, lowercase, and a number.
            </p>

            <div v-if="isComplete" class="mt-6 space-y-4">
                <p class="rounded-lg border border-emerald-700 bg-emerald-950/40 p-4 text-emerald-200">
                    Your password has been changed. Existing sessions have been revoked.
                </p>
                <button
                    type="button"
                    class="w-full rounded-lg bg-indigo-500 px-4 py-3 font-medium hover:bg-indigo-400"
                    @click="router.push({ name: 'login' })"
                >
                    Continue to login
                </button>
            </div>

            <form v-else class="mt-6 space-y-4" @submit.prevent="submit">
                <label class="block">
                    <span class="mb-1 block text-sm text-slate-300">New password</span>
                    <input
                        v-model="newPassword"
                        type="password"
                        autocomplete="new-password"
                        minlength="8"
                        required
                        class="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-3 outline-none focus:border-indigo-400"
                    />
                </label>

                <label class="block">
                    <span class="mb-1 block text-sm text-slate-300">Confirm password</span>
                    <input
                        v-model="confirmPassword"
                        type="password"
                        autocomplete="new-password"
                        minlength="8"
                        required
                        class="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-3 outline-none focus:border-indigo-400"
                    />
                </label>

                <p v-if="errorMessage" class="rounded-lg border border-red-800 bg-red-950/40 p-3 text-sm text-red-200">
                    {{ errorMessage }}
                </p>

                <button
                    type="submit"
                    :disabled="isSubmitting || !isLinkValid"
                    class="w-full rounded-lg bg-indigo-500 px-4 py-3 font-medium hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-50"
                >
                    {{ isSubmitting ? 'Updating…' : 'Update password' }}
                </button>
            </form>
        </section>
    </main>
</template>
