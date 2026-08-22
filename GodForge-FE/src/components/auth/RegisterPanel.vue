<script setup lang="ts">
import { computed, onUnmounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../../stores/auth.store';
import { authApi } from '../../api/auth/auth.api';
import AuthInput from './AuthInput.vue';
import { RegistrationCompletedLoginFailedError } from '../../stores/auth.store';

type RegisterFieldErrors = Partial<Record<'displayName' | 'email' | 'otp' | 'password' | 'confirmPassword', string>>;

const router = useRouter();
const authStore = useAuthStore();
const displayName = ref('');
const email = ref('');
const password = ref('');
const confirmPassword = ref('');
const otp = ref('');
const loading = ref(false);
const errorMsg = ref('');
const statusMsg = ref('');
const fieldErrors = ref<RegisterFieldErrors>({});
const otpDestination = ref('');
const sendingOtp = ref(false);
const otpCooldown = ref(0);
let cooldownTimer: number | undefined;

const normalizedEmail = computed(() => email.value.trim().toLowerCase());
const otpReady = computed(() => Boolean(otpDestination.value && otpDestination.value === normalizedEmail.value));
const passwordRules = computed(() => ({
    length: password.value.length >= 8,
    upper: /[A-Z]/.test(password.value),
    lower: /[a-z]/.test(password.value),
    number: /[0-9]/.test(password.value),
}));
const strongPassword = computed(() => Object.values(passwordRules.value).every(Boolean));

const startCooldown = (seconds: number) => {
    if (cooldownTimer) window.clearInterval(cooldownTimer);
    otpCooldown.value = seconds;
    cooldownTimer = window.setInterval(() => {
        otpCooldown.value = Math.max(0, otpCooldown.value - 1);
        if (otpCooldown.value === 0 && cooldownTimer) window.clearInterval(cooldownTimer);
    }, 1000);
};

onUnmounted(() => { if (cooldownTimer) window.clearInterval(cooldownTimer); });

const isValidEmail = () => /^\S+@\S+\.\S+$/.test(normalizedEmail.value);

const handleSendOtp = async () => {
    errorMsg.value = '';
    statusMsg.value = '';
    if (!normalizedEmail.value) {
        fieldErrors.value = { ...fieldErrors.value, email: 'Enter your email address first.' };
        return;
    }
    if (!isValidEmail()) {
        fieldErrors.value = { ...fieldErrors.value, email: 'Enter a valid email address.' };
        return;
    }
    fieldErrors.value = { ...fieldErrors.value, email: undefined };
    try {
        sendingOtp.value = true;
        const response = await authApi.sendOtp(normalizedEmail.value);
        otpDestination.value = normalizedEmail.value;
        otp.value = '';
        statusMsg.value = 'A 6-digit verification code was sent to your email.';
        startCooldown(response.data.resendAfterSeconds);
    } catch (error: unknown) {
        if (error instanceof RegistrationCompletedLoginFailedError) {
            await router.push({ name: 'login', query: { email: error.email, registered: 'true' } });
            return;
        }
        const err = error as { response?: { data?: { error?: { message?: string } } } };
        errorMsg.value = err.response?.data?.error?.message || 'We could not send the verification code. Please try again.';
    } finally {
        sendingOtp.value = false;
    }
};

const validate = () => {
    const errors: RegisterFieldErrors = {};
    if (!displayName.value.trim()) errors.displayName = 'Enter the name your team will see.';
    else if (displayName.value.trim().length > 120) errors.displayName = 'Name must be 120 characters or fewer.';
    if (!normalizedEmail.value) errors.email = 'Enter your email address.';
    else if (!isValidEmail()) errors.email = 'Enter a valid email address.';
    if (!otpReady.value) errors.otp = 'Request a code for this email address.';
    else if (!/^\d{6}$/.test(otp.value)) errors.otp = 'Enter the 6-digit verification code.';
    if (!strongPassword.value) errors.password = 'Your password must meet all requirements below.';
    if (!confirmPassword.value) errors.confirmPassword = 'Confirm your password.';
    else if (confirmPassword.value !== password.value) errors.confirmPassword = 'Passwords do not match.';
    fieldErrors.value = errors;
    return Object.keys(errors).length === 0;
};

const handleRegister = async () => {
    errorMsg.value = '';
    statusMsg.value = '';
    if (!validate()) return;
    try {
        loading.value = true;
        await authStore.register({ email: normalizedEmail.value, displayName: displayName.value.trim(), password: password.value, otp: otp.value });
        await router.push({ name: 'dashboard' });
    } catch (error: unknown) {
        const err = error as { response?: { data?: { error?: { message?: string } } } };
        errorMsg.value = err.response?.data?.error?.message || 'We could not create your account. Review the details and try again.';
    } finally {
        loading.value = false;
    }
};
</script>

<template>
    <div class="register-panel">
        <div class="register-panel__intro">
            <span>New workspace identity</span>
            <h1>Start with GodForge.</h1>
            <p>Create a verified account for your projects and team.</p>
        </div>

        <div v-if="errorMsg" id="register-error" class="register-panel__alert register-panel__alert--error" role="alert" aria-live="polite">
            <b aria-hidden="true">!</b><p>{{ errorMsg }}</p>
        </div>
        <div v-else-if="statusMsg" class="register-panel__alert register-panel__alert--success" role="status" aria-live="polite">
            <b aria-hidden="true">✓</b><p>{{ statusMsg }}</p>
        </div>

        <form class="register-panel__form" novalidate @submit.prevent="handleRegister">
            <div class="register-panel__grid">
                <AuthInput v-model="displayName" id="register-name" name="displayName" label="Display name" placeholder="Alex Morgan" autocomplete="name" required :disabled="loading" :maxlength="120" :error="fieldErrors.displayName" />
                <AuthInput class="register-panel__email-input" v-model="email" id="register-email" name="email" label="Work email" type="email" placeholder="name@company.com" autocomplete="email" inputmode="email" required :disabled="loading" :maxlength="255" :error="fieldErrors.email">
                    <template #action>
                        <button type="button" class="register-panel__code-button" :disabled="sendingOtp || otpCooldown > 0 || !email || loading" @click="handleSendOtp">
                            <span v-if="sendingOtp" class="register-panel__spinner" aria-hidden="true"></span>
                            {{ sendingOtp ? 'Sending…' : otpCooldown > 0 ? `${otpCooldown}s` : otpReady ? 'Resend' : 'Send code' }}
                        </button>
                    </template>
                </AuthInput>
                <div class="register-panel__wide">
                    <AuthInput v-model="otp" id="register-otp" name="otp" label="Email verification" placeholder="6-digit code" autocomplete="one-time-code" inputmode="numeric" required :disabled="loading || !otpReady" :maxlength="6" :error="fieldErrors.otp" :hint="otpReady ? `Sent to ${otpDestination}` : 'Request a code using your email above.'" />
                </div>
                <AuthInput v-model="password" id="register-password" name="password" label="Password" type="password" placeholder="Create a password" autocomplete="new-password" required :disabled="loading" :maxlength="256" :error="fieldErrors.password" />
                <AuthInput v-model="confirmPassword" id="register-confirm-password" name="confirmPassword" label="Confirm password" type="password" placeholder="Repeat password" autocomplete="new-password" required :disabled="loading" :maxlength="256" :error="fieldErrors.confirmPassword" />
            </div>

            <div class="register-panel__rules" aria-label="Password requirements">
                <span :class="{ ready: passwordRules.length }">{{ passwordRules.length ? '✓' : '·' }} 8+ characters</span>
                <span :class="{ ready: passwordRules.upper && passwordRules.lower }">{{ passwordRules.upper && passwordRules.lower ? '✓' : '·' }} Mixed case</span>
                <span :class="{ ready: passwordRules.number }">{{ passwordRules.number ? '✓' : '·' }} One number</span>
            </div>

            <div class="register-panel__finish">
                <p>By continuing, you agree to protect access to your GodForge workspace.</p>
                <button type="submit" class="register-panel__submit" :disabled="loading">
                    <span v-if="loading" class="register-panel__spinner" aria-hidden="true"></span>
                    <span>{{ loading ? 'Creating account…' : 'Create account' }}</span>
                    <span v-if="!loading" aria-hidden="true">→</span>
                </button>
            </div>
        </form>
    </div>
</template>

<style scoped>
.register-panel { width: 100%; }
.register-panel__intro { margin-bottom: 1.55rem; }.register-panel__intro > span { display: block; margin-bottom: .72rem; color: #3157f6; font-size: .64rem; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }.register-panel__intro h1 { margin: 0; color: #111827; font-size: 1.9rem; font-weight: 760; letter-spacing: -.05em; }.register-panel__intro p { margin: .55rem 0 0; color: #77808f; font-size: .75rem; line-height: 1.55; }
.register-panel__alert { display: flex; align-items: flex-start; gap: .6rem; margin-bottom: 1rem; padding: .7rem; border-radius: .55rem; font-size: .68rem; line-height: 1.45; }.register-panel__alert b { display: grid; width: 1.1rem; height: 1.1rem; flex: 0 0 auto; place-items: center; border-radius: 50%; color: white; }.register-panel__alert p { margin: 0; }.register-panel__alert--error { color: #a93434; background: #fff0f0; }.register-panel__alert--error b { background: #d75151; }.register-panel__alert--success { color: #236347; background: #eefaf4; }.register-panel__alert--success b { background: #3a9b70; }
.register-panel__form { display: flex; flex-direction: column; gap: .9rem; }.register-panel__grid { display: grid; gap: .9rem 1rem; grid-template-columns: repeat(2, minmax(0, 1fr)); }.register-panel__wide { grid-column: 1 / -1; }.register-panel__email-input :deep(input) { padding-right: 5.6rem; }.register-panel__code-button { position: absolute; right: .45rem; min-width: 4.8rem; height: 2.15rem; padding: 0 .55rem; cursor: pointer; border: 0; border-radius: .42rem; color: #3157f6; background: #edf0ff; font: inherit; font-size: .61rem; font-weight: 760; }.register-panel__code-button:hover:not(:disabled) { background: #e0e5ff; }.register-panel__code-button:disabled { cursor: not-allowed; color: #9aa1ad; background: #f0f1f3; }
.register-panel__rules { display: flex; flex-wrap: wrap; gap: .5rem 1rem; color: #989faa; font-size: .61rem; }.register-panel__rules span.ready { color: #3157f6; }.register-panel__finish { display: flex; align-items: center; justify-content: space-between; gap: 1.5rem; margin-top: .35rem; padding-top: 1rem; border-top: 1px solid #e6e8ec; }.register-panel__finish > p { max-width: 16rem; margin: 0; color: #9299a5; font-size: .6rem; line-height: 1.5; }
.register-panel__submit { display: flex; min-width: 10.5rem; min-height: 2.9rem; cursor: pointer; align-items: center; justify-content: center; gap: .6rem; border: 0; border-radius: .62rem; color: #fff; background: #3157f6; box-shadow: 0 8px 18px rgb(49 87 246 / .16); font: inherit; font-size: .74rem; font-weight: 760; transition: background 140ms ease, transform 140ms ease; }.register-panel__submit:hover:not(:disabled) { background: #2449e2; transform: translateY(-1px); }.register-panel__submit:focus-visible { outline: 3px solid rgb(49 87 246 / .22); outline-offset: 3px; }.register-panel__submit:disabled { cursor: not-allowed; opacity: .62; }.register-panel__spinner { display: inline-block; width: .76rem; height: .76rem; border: 2px solid currentColor; border-top-color: transparent; border-radius: 50%; animation: spin .7s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
@media (max-width: 620px) { .register-panel__grid { grid-template-columns: 1fr; }.register-panel__wide { grid-column: auto; }.register-panel__finish { align-items: stretch; flex-direction: column; }.register-panel__finish > p { max-width: none; }.register-panel__submit { width: 100%; } }
@media (prefers-reduced-motion: reduce) { .register-panel__submit, .register-panel__spinner { transition: none; animation-duration: 1.5s; } }
</style>
