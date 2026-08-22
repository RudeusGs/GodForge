<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { authApi } from '@/api/auth/auth.api';
import AuthInput from '@/components/auth/AuthInput.vue';
import AuthShell from '@/components/auth/AuthShell.vue';
import { useAuthStore } from '@/stores/auth.store';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const email = computed(() => String(route.query.email ?? ''));
const token = computed(() => String(route.query.token ?? ''));
const newPassword = ref('');
const confirmPassword = ref('');
const errorMessage = ref('');
const passwordError = ref('');
const confirmationError = ref('');
const isSubmitting = ref(false);
const isComplete = ref(false);
const isLinkValid = computed(() => email.value.length > 0 && token.value.length > 0);
const passwordRules = computed(() => ({
    length: newPassword.value.length >= 8,
    upper: /[A-Z]/.test(newPassword.value),
    lower: /[a-z]/.test(newPassword.value),
    number: /[0-9]/.test(newPassword.value),
}));

const validate = () => {
    passwordError.value = '';
    confirmationError.value = '';
    if (!Object.values(passwordRules.value).every(Boolean)) passwordError.value = 'Your password must meet every requirement.';
    if (!confirmPassword.value) confirmationError.value = 'Confirm your new password.';
    else if (newPassword.value !== confirmPassword.value) confirmationError.value = 'Passwords do not match.';
    return !passwordError.value && !confirmationError.value;
};

const submit = async () => {
    errorMessage.value = '';
    if (!isLinkValid.value) {
        errorMessage.value = 'This password reset link is incomplete or invalid.';
        return;
    }
    if (!validate()) return;
    isSubmitting.value = true;
    try {
        await authApi.resetPassword({ email: email.value, token: token.value, newPassword: newPassword.value });
        authStore.invalidateAuthentication('password-changed');
        isComplete.value = true;
        newPassword.value = '';
        confirmPassword.value = '';
    } catch {
        errorMessage.value = 'This reset link is invalid or expired, or the password could not be accepted.';
    } finally {
        isSubmitting.value = false;
    }
};
</script>

<template>
    <AuthShell>
        <div class="reset-panel">
            <div v-if="isComplete" class="reset-panel__complete" role="status" aria-live="polite">
                <div class="reset-panel__success-mark" aria-hidden="true"><span>✓</span></div>
                <span class="reset-panel__kicker">Password updated</span>
                <h1>Access restored.</h1>
                <p>Your password has changed and existing sessions have been revoked for your protection.</p>
                <button type="button" class="reset-panel__primary" @click="router.push({ name: 'login' })">Continue to sign in <span aria-hidden="true">→</span></button>
            </div>

            <template v-else>
                <div class="reset-panel__intro">
                    <span class="reset-panel__kicker">Secure reset</span>
                    <h1>Choose a new password.</h1>
                    <p>Create a password you have not used for this account before.</p>
                </div>

                <div v-if="!isLinkValid || errorMessage" id="reset-error" class="reset-panel__alert" role="alert" aria-live="polite">
                    <span aria-hidden="true">!</span><p>{{ errorMessage || 'This password reset link is incomplete or invalid.' }}</p>
                </div>

                <form class="reset-panel__form" novalidate @submit.prevent="submit">
                    <AuthInput v-model="newPassword" id="reset-password" name="newPassword" label="New password" type="password" placeholder="Create a new password" autocomplete="new-password" required :disabled="isSubmitting || !isLinkValid" :maxlength="256" :error="passwordError" />
                    <div class="reset-panel__rules" aria-label="Password requirements">
                        <span :class="{ ready: passwordRules.length }">{{ passwordRules.length ? '✓' : '·' }} 8+ characters</span>
                        <span :class="{ ready: passwordRules.upper && passwordRules.lower }">{{ passwordRules.upper && passwordRules.lower ? '✓' : '·' }} Mixed case</span>
                        <span :class="{ ready: passwordRules.number }">{{ passwordRules.number ? '✓' : '·' }} One number</span>
                    </div>
                    <AuthInput v-model="confirmPassword" id="reset-confirm-password" name="confirmPassword" label="Confirm password" type="password" placeholder="Repeat your new password" autocomplete="new-password" required :disabled="isSubmitting || !isLinkValid" :maxlength="256" :error="confirmationError" />
                    <button type="submit" class="reset-panel__primary" :disabled="isSubmitting || !isLinkValid">
                        <span v-if="isSubmitting" class="reset-panel__spinner" aria-hidden="true"></span>
                        <span>{{ isSubmitting ? 'Updating password…' : 'Update password' }}</span>
                        <span v-if="!isSubmitting" aria-hidden="true">→</span>
                    </button>
                </form>
                <router-link to="/login" class="reset-panel__back"><span aria-hidden="true">←</span> Back to sign in</router-link>
            </template>
        </div>
    </AuthShell>
</template>

<style scoped>
.reset-panel { width: 100%; }.reset-panel__intro { margin-bottom: 1.8rem; }.reset-panel__kicker { display: block; margin-bottom: .75rem; color: #3157f6; font-size: .65rem; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }.reset-panel h1 { margin: 0; color: #111827; font-size: 1.9rem; font-weight: 760; letter-spacing: -.05em; }.reset-panel__intro p, .reset-panel__complete > p { margin: .65rem 0 0; color: #77808f; font-size: .75rem; line-height: 1.65; }
.reset-panel__alert { display: flex; align-items: flex-start; gap: .65rem; margin-bottom: 1rem; padding: .75rem; border-radius: .55rem; color: #a93434; background: #fff0f0; font-size: .69rem; line-height: 1.5; }.reset-panel__alert > span { display: grid; width: 1.15rem; height: 1.15rem; flex: 0 0 auto; place-items: center; border-radius: 50%; color: #fff; background: #d75151; font-weight: 800; }.reset-panel__alert p { margin: 0; }
.reset-panel__form { display: flex; flex-direction: column; gap: 1rem; }.reset-panel__rules { display: flex; flex-wrap: wrap; gap: .5rem 1rem; margin-top: -.35rem; color: #989faa; font-size: .61rem; }.reset-panel__rules span.ready { color: #3157f6; }.reset-panel__primary { display: flex; min-height: 3.08rem; cursor: pointer; align-items: center; justify-content: center; gap: .7rem; margin-top: .2rem; border: 0; border-radius: .65rem; color: #fff; background: #3157f6; box-shadow: 0 8px 18px rgb(49 87 246 / .18); font: inherit; font-size: .76rem; font-weight: 760; transition: background 140ms ease, transform 140ms ease, box-shadow 140ms ease; }.reset-panel__primary:hover:not(:disabled) { background: #2449e2; box-shadow: 0 11px 24px rgb(49 87 246 / .23); transform: translateY(-1px); }.reset-panel__primary:focus-visible { outline: 3px solid rgb(49 87 246 / .22); outline-offset: 3px; }.reset-panel__primary:disabled { cursor: not-allowed; opacity: .55; }.reset-panel__back { display: flex; width: fit-content; align-items: center; gap: .4rem; margin: 1.35rem auto 0; color: #747d8d; font-size: .66rem; font-weight: 680; text-decoration: none; }.reset-panel__back:hover { color: #3157f6; }.reset-panel__spinner { width: .82rem; height: .82rem; border: 2px solid rgb(255 255 255 / .4); border-top-color: #fff; border-radius: 50%; animation: reset-spin .7s linear infinite; }
.reset-panel__complete { text-align: center; animation: reset-complete 480ms cubic-bezier(.22,.75,.24,1) both; }.reset-panel__success-mark { position: relative; display: grid; width: 4rem; height: 4rem; margin: 0 auto 1.3rem; place-items: center; border-radius: 50%; color: #fff; background: #3157f6; box-shadow: 0 0 0 9px #eef1ff, 0 15px 30px rgb(49 87 246 / .18); }.reset-panel__success-mark::after { position: absolute; inset: -.7rem; content: ''; border: 1px solid rgb(49 87 246 / .2); border-radius: 50%; animation: reset-ring 1.8s ease-out infinite; }.reset-panel__success-mark span { font-size: 1.3rem; font-weight: 800; }.reset-panel__complete .reset-panel__primary { width: 100%; margin-top: 1.6rem; }
@keyframes reset-spin { to { transform: rotate(360deg); } } @keyframes reset-complete { from { opacity: 0; transform: translateY(12px); } to { opacity: 1; transform: none; } } @keyframes reset-ring { 0% { opacity: .7; transform: scale(.82); } 80%, 100% { opacity: 0; transform: scale(1.18); } }
@media (prefers-reduced-motion: reduce) { .reset-panel__spinner, .reset-panel__complete, .reset-panel__success-mark::after { animation: none; } }
</style>
