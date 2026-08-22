<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { getBrowserDeviceLabel, useAuthStore } from '../../stores/auth.store';
import AuthInput from './AuthInput.vue';
import { safeReturnTo } from '../../utils/safeReturnTo';
import axios from 'axios';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const email = ref(typeof route.query.email === 'string' ? route.query.email : '');
const password = ref('');
const loading = ref(false);
const errorMsg = ref('');
const statusMsg = ref(route.query.registered === 'true'
    ? 'Your account was created. Please sign in.'
    : route.query.authReason === 'password-changed'
        ? 'Your password changed. Sign in with your new password.'
        : route.query.authReason === 'session-revoked'
            ? 'This session was signed out. Sign in again.'
            : route.query.authReason === 'session-expired'
                ? 'Your session expired. Sign in again.'
                : '');
const fieldErrors = ref<{ email?: string; password?: string }>({});

const validate = () => {
    const errors: { email?: string; password?: string } = {};
    const normalizedEmail = email.value.trim();
    if (!normalizedEmail) errors.email = 'Enter your email address.';
    else if (!/^\S+@\S+\.\S+$/.test(normalizedEmail)) errors.email = 'Enter a valid email address.';
    if (!password.value) errors.password = 'Enter your password.';
    fieldErrors.value = errors;
    return Object.keys(errors).length === 0;
};

const handleLogin = async () => {
    errorMsg.value = '';
    if (!validate()) return;
    try {
        loading.value = true;
        await authStore.login({ email: email.value.trim(), password: password.value, deviceName: getBrowserDeviceLabel() });
        await router.push(safeReturnTo(route.query.returnTo) ?? { name: 'dashboard' });
    } catch (error: unknown) {
        if (axios.isAxiosError(error) && error.response?.status === 429) {
            errorMsg.value = 'Too many sign-in attempts. Try again later.';
        } else if (axios.isAxiosError(error) && !error.response) {
            errorMsg.value = 'GodForge could not be reached. Check your connection and try again.';
        } else {
            errorMsg.value = 'We could not sign you in. Check your email and password, then try again.';
        }
    } finally {
        loading.value = false;
    }
};
</script>

<template>
    <div class="auth-panel">
        <div class="auth-panel__intro">
            <span class="auth-panel__number">Member access</span>
            <h1>Welcome back.</h1>
            <p>Sign in with the email connected to your workspace.</p>
        </div>

        <div v-if="errorMsg" id="login-error" class="auth-panel__alert" role="alert" aria-live="polite">
            <span aria-hidden="true">!</span><p>{{ errorMsg }}</p>
        </div>
        <div v-else-if="statusMsg" class="auth-panel__status" role="status" aria-live="polite">
            <span aria-hidden="true">✓</span><p>{{ statusMsg }}</p>
        </div>

        <form class="auth-panel__form" novalidate :aria-describedby="errorMsg ? 'login-error' : undefined" @submit.prevent="handleLogin">
            <AuthInput v-model="email" id="login-email" name="email" label="Email address" type="email" placeholder="name@company.com" autocomplete="email" inputmode="email" required :disabled="loading" :error="fieldErrors.email" />
            <AuthInput v-model="password" id="login-password" name="password" label="Password" type="password" placeholder="Your password" autocomplete="current-password" required :disabled="loading" :error="fieldErrors.password" />
            <div class="auth-panel__options">
                <router-link to="/forgot-password">Forgot password?</router-link>
            </div>
            <button type="submit" class="auth-panel__submit" :disabled="loading">
                <span v-if="loading" class="auth-panel__spinner" aria-hidden="true"></span>
                <span>{{ loading ? 'Signing in…' : 'Sign in' }}</span>
                <span v-if="!loading" aria-hidden="true">→</span>
            </button>
        </form>
        <p class="auth-panel__note"><span></span>Your credentials stay private and are never logged.</p>
    </div>
</template>

<style scoped>
.auth-panel { width: 100%; }
.auth-panel__intro { margin-bottom: 2rem; }.auth-panel__number { display: block; margin-bottom: .75rem; color: #3157f6; font-size: .65rem; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }.auth-panel__intro h1 { margin: 0; color: #111827; font-size: 2rem; font-weight: 760; letter-spacing: -.05em; }.auth-panel__intro p { margin: .65rem 0 0; color: #77808f; font-size: .78rem; line-height: 1.6; }
.auth-panel__alert { display: flex; align-items: flex-start; gap: .65rem; margin-bottom: 1.1rem; padding: .75rem; border-radius: .55rem; color: #a93434; background: #fff0f0; font-size: .7rem; line-height: 1.5; }.auth-panel__alert > span { display: grid; width: 1.15rem; height: 1.15rem; flex: 0 0 auto; place-items: center; border-radius: 50%; color: #fff; background: #d75151; font-weight: 800; }.auth-panel__alert p { margin: 0; }
.auth-panel__status { display: flex; align-items: flex-start; gap: .65rem; margin-bottom: 1.1rem; padding: .75rem; border-radius: .55rem; color: #176b4d; background: #edf9f3; font-size: .7rem; line-height: 1.5; }.auth-panel__status > span { font-weight: 800; }.auth-panel__status p { margin: 0; }
.auth-panel__form { display: flex; flex-direction: column; gap: 1.05rem; }.auth-panel__options { display: flex; justify-content: flex-end; margin-top: -.2rem; font-size: .67rem; }.auth-panel__options a { color: #3157f6; font-weight: 700; text-decoration: none; }.auth-panel__options a:hover { text-decoration: underline; text-underline-offset: 3px; }
.auth-panel__submit { display: flex; min-height: 3.08rem; cursor: pointer; align-items: center; justify-content: center; gap: .7rem; margin-top: .25rem; border: 0; border-radius: .65rem; color: #fff; background: #3157f6; box-shadow: 0 8px 18px rgb(49 87 246 / .18); font: inherit; font-size: .78rem; font-weight: 760; transition: background 140ms ease, transform 140ms ease; }.auth-panel__submit:hover:not(:disabled) { background: #2449e2; transform: translateY(-1px); }.auth-panel__submit:focus-visible { outline: 3px solid rgb(49 87 246 / .22); outline-offset: 3px; }.auth-panel__submit:disabled { cursor: not-allowed; opacity: .62; }
.auth-panel__spinner { width: .86rem; height: .86rem; border: 2px solid rgb(255 255 255 / .4); border-top-color: #fff; border-radius: 50%; animation: spin .7s linear infinite; }.auth-panel__note { display: flex; align-items: center; justify-content: center; gap: .45rem; margin: 1.5rem 0 0; color: #939aa6; font-size: .61rem; }.auth-panel__note span { width: .36rem; height: .36rem; border-radius: 50%; background: #7c8dff; }
@keyframes spin { to { transform: rotate(360deg); } }
@media (prefers-reduced-motion: reduce) { .auth-panel__submit, .auth-panel__spinner { transition: none; animation-duration: 1.5s; } }
</style>
