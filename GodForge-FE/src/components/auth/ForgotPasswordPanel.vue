<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '../../stores/auth.store';
import AuthInput from './AuthInput.vue';

const authStore = useAuthStore();
const email = ref('');
const loading = ref(false);
const errorMsg = ref('');
const emailError = ref('');
const requestComplete = ref(false);

const validate = () => {
    const value = email.value.trim();
    if (!value) emailError.value = 'Enter your email address.';
    else if (!/^\S+@\S+\.\S+$/.test(value)) emailError.value = 'Enter a valid email address.';
    else emailError.value = '';
    return !emailError.value;
};

const handleForgotPassword = async () => {
    errorMsg.value = '';
    if (!validate()) return;
    try {
        loading.value = true;
        await authStore.forgotPassword(email.value.trim());
        requestComplete.value = true;
    } catch (error: unknown) {
        const err = error as { response?: { data?: { error?: { message?: string } } } };
        errorMsg.value = err.response?.data?.error?.message || 'We could not process the request. Please wait a moment and try again.';
    } finally {
        loading.value = false;
    }
};

const tryAnotherEmail = () => {
    requestComplete.value = false;
    errorMsg.value = '';
    emailError.value = '';
};
</script>

<template>
    <div class="recovery-panel">
        <div v-if="requestComplete" class="recovery-panel__complete" role="status" aria-live="polite">
            <div class="recovery-panel__success-mark" aria-hidden="true"><span>✓</span></div>
            <span class="recovery-panel__kicker">Request received</span>
            <h1>Check your inbox.</h1>
            <p>If an account matches <strong>{{ email.trim() }}</strong>, we sent a secure password reset link. It may take a minute to arrive.</p>
            <router-link to="/login" class="recovery-panel__primary">Return to sign in <span aria-hidden="true">→</span></router-link>
            <button type="button" class="recovery-panel__text-button" @click="tryAnotherEmail">Use a different email</button>
        </div>

        <template v-else>
            <div class="recovery-panel__intro">
                <span class="recovery-panel__kicker">Account recovery</span>
                <h1>Reset your password.</h1>
                <p>Enter your account email. We will send the next step if the address is eligible.</p>
            </div>

            <div v-if="errorMsg" id="forgot-error" class="recovery-panel__alert" role="alert" aria-live="polite">
                <span aria-hidden="true">!</span><p>{{ errorMsg }}</p>
            </div>

            <form class="recovery-panel__form" novalidate :aria-describedby="errorMsg ? 'forgot-error' : undefined" @submit.prevent="handleForgotPassword">
                <AuthInput v-model="email" id="forgot-email" name="email" label="Email address" type="email" placeholder="name@company.com" autocomplete="email" inputmode="email" required :disabled="loading" :maxlength="255" :error="emailError" hint="For privacy, the response is the same for every valid email." />
                <button type="submit" class="recovery-panel__primary" :disabled="loading">
                    <span v-if="loading" class="recovery-panel__spinner" aria-hidden="true"></span>
                    <span>{{ loading ? 'Sending link…' : 'Send reset link' }}</span>
                    <span v-if="!loading" aria-hidden="true">→</span>
                </button>
            </form>
            <router-link to="/login" class="recovery-panel__back"><span aria-hidden="true">←</span> Back to sign in</router-link>
        </template>
    </div>
</template>

<style scoped>
.recovery-panel { width: 100%; }.recovery-panel__intro { margin-bottom: 2rem; }.recovery-panel__kicker { display: block; margin-bottom: .75rem; color: #3157f6; font-size: .65rem; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }.recovery-panel h1 { margin: 0; color: #111827; font-size: 2rem; font-weight: 760; letter-spacing: -.05em; }.recovery-panel__intro p, .recovery-panel__complete > p { margin: .65rem 0 0; color: #77808f; font-size: .76rem; line-height: 1.65; }.recovery-panel__complete > p strong { color: #414a5b; font-weight: 720; overflow-wrap: anywhere; }
.recovery-panel__alert { display: flex; align-items: flex-start; gap: .65rem; margin-bottom: 1.1rem; padding: .75rem; border-radius: .55rem; color: #a93434; background: #fff0f0; font-size: .7rem; line-height: 1.5; }.recovery-panel__alert > span { display: grid; width: 1.15rem; height: 1.15rem; flex: 0 0 auto; place-items: center; border-radius: 50%; color: #fff; background: #d75151; font-weight: 800; }.recovery-panel__alert p { margin: 0; }
.recovery-panel__form { display: flex; flex-direction: column; gap: 1.25rem; }.recovery-panel__primary { display: flex; min-height: 3.08rem; cursor: pointer; align-items: center; justify-content: center; gap: .7rem; border: 0; border-radius: .65rem; color: #fff; background: #3157f6; box-shadow: 0 8px 18px rgb(49 87 246 / .18); font: inherit; font-size: .76rem; font-weight: 760; text-decoration: none; transition: background 140ms ease, transform 140ms ease, box-shadow 140ms ease; }.recovery-panel__primary:hover:not(:disabled) { background: #2449e2; box-shadow: 0 11px 24px rgb(49 87 246 / .23); transform: translateY(-1px); }.recovery-panel__primary:focus-visible { outline: 3px solid rgb(49 87 246 / .22); outline-offset: 3px; }.recovery-panel__primary:disabled { cursor: not-allowed; opacity: .62; }
.recovery-panel__back { display: flex; width: fit-content; align-items: center; gap: .4rem; margin: 1.5rem auto 0; color: #747d8d; font-size: .66rem; font-weight: 680; text-decoration: none; }.recovery-panel__back:hover { color: #3157f6; }.recovery-panel__spinner { width: .82rem; height: .82rem; border: 2px solid rgb(255 255 255 / .4); border-top-color: #fff; border-radius: 50%; animation: recovery-spin .7s linear infinite; }
.recovery-panel__complete { text-align: center; animation: recovery-complete 480ms cubic-bezier(.22,.75,.24,1) both; }.recovery-panel__success-mark { position: relative; display: grid; width: 4rem; height: 4rem; margin: 0 auto 1.3rem; place-items: center; border-radius: 50%; color: #fff; background: #3157f6; box-shadow: 0 0 0 9px #eef1ff, 0 15px 30px rgb(49 87 246 / .18); }.recovery-panel__success-mark::after { position: absolute; inset: -.7rem; content: ''; border: 1px solid rgb(49 87 246 / .2); border-radius: 50%; animation: recovery-ring 1.8s ease-out infinite; }.recovery-panel__success-mark span { font-size: 1.3rem; font-weight: 800; }.recovery-panel__complete .recovery-panel__primary { margin-top: 1.6rem; }.recovery-panel__text-button { margin-top: .9rem; cursor: pointer; border: 0; color: #7e8795; background: transparent; font: inherit; font-size: .64rem; font-weight: 680; }.recovery-panel__text-button:hover { color: #3157f6; }
@keyframes recovery-spin { to { transform: rotate(360deg); } } @keyframes recovery-complete { from { opacity: 0; transform: translateY(12px); } to { opacity: 1; transform: none; } } @keyframes recovery-ring { 0% { opacity: .7; transform: scale(.82); } 80%, 100% { opacity: 0; transform: scale(1.18); } }
@media (prefers-reduced-motion: reduce) { .recovery-panel__spinner, .recovery-panel__complete, .recovery-panel__success-mark::after { animation: none; } }
</style>
