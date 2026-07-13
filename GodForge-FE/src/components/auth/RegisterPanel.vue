<script setup lang="ts">
import { ref, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../../stores/auth.store';
import { authApi } from '../../api/auth/auth.api';
import AuthInput from './AuthInput.vue';

const router = useRouter();
const authStore = useAuthStore();

const displayName = ref('');
const email = ref('');
const password = ref('');
const confirmPassword = ref('');
const otp = ref('');
const loading = ref(false);
const errorMsg = ref('');

const otpSent = ref(false);
const sendingOtp = ref(false);
const otpCooldown = ref(0);
let cooldownTimer: number | undefined = undefined;

const startCooldown = () => {
    otpCooldown.value = 60;
    cooldownTimer = window.setInterval(() => {
        if (otpCooldown.value > 0) {
            otpCooldown.value--;
        } else {
            window.clearInterval(cooldownTimer);
        }
    }, 1000);
};

onUnmounted(() => {
    if (cooldownTimer) window.clearInterval(cooldownTimer);
});

const handleSendOtp = async () => {
    if (!email.value) {
        errorMsg.value = 'Email is required to request verification credentials.';
        return;
    }
    
    try {
        sendingOtp.value = true;
        errorMsg.value = '';
        await authApi.sendOtp(email.value);
        otpSent.value = true;
        startCooldown();
    } catch (error: unknown) {
        const err = error as { response?: { data?: { error?: { message?: string } } } };
        errorMsg.value = err.response?.data?.error?.message || 'Failed to send OTP verification code.';
    } finally {
        sendingOtp.value = false;
    }
};

const handleRegister = async () => {
    if (!displayName.value || !email.value || !password.value) {
        errorMsg.value = 'All fields are required for workspace access allocation.';
        return;
    }
    if (!otpSent.value) {
        errorMsg.value = 'Please request and verify an OTP code before registering.';
        return;
    }
    if (!otp.value) {
        errorMsg.value = 'OTP verification code is required.';
        return;
    }
    if (password.value !== confirmPassword.value) {
        errorMsg.value = 'Token verification failed. Inputs do not match.';
        return;
    }
    
    try {
        loading.value = true;
        errorMsg.value = '';
        await authStore.register({ 
            email: email.value, 
            displayName: displayName.value,
            password: password.value,
            otp: otp.value
        });
        router.push('/');
    } catch (error: unknown) {
        const err = error as { response?: { data?: { error?: { message?: string } } } };
        errorMsg.value = err.response?.data?.error?.message || 'ERR_ALLOCATION: Workspace provisioning failed.';
    } finally {
        loading.value = false;
    }
};
</script>

<template>
    <div class="w-full max-w-md mx-auto p-6 sm:p-10 flex flex-col justify-center bg-[#09090b] relative z-10 h-full overflow-y-auto custom-scrollbar">
        
        <!-- Header -->
        <div class="mb-8 mt-4 font-sans">
            <div class="flex items-center space-x-2.5 mb-3">
                <i class="bi bi-person-plus text-xl text-emerald-400"></i>
                <h1 class="text-xl font-semibold text-zinc-100 tracking-tight">GodForge</h1>
            </div>
            <p class="text-zinc-400 text-xs leading-relaxed">
                Initialize a new developer profile and request secure workspace credentials.
            </p>
        </div>

        <!-- Form -->
        <form @submit.prevent="handleRegister" class="space-y-5 mb-4">
            
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

            <div class="space-y-4">
                <AuthInput 
                    v-model="displayName" 
                    label="Alias / Callsign" 
                    type="text" 
                    icon="bi-person-badge" 
                    placeholder="dev_alpha"
                />

                <div class="flex flex-col space-y-1.5 font-sans">
                    <label class="text-[11px] font-semibold text-zinc-400 uppercase tracking-wider">Contact Protocol</label>
                    <div class="flex gap-2">
                        <div class="relative group flex-1">
                            <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-zinc-500 group-focus-within:text-cyan-500 transition-colors">
                                <i class="bi bi-envelope"></i>
                            </div>
                            <input 
                                type="email" 
                                v-model="email"
                                placeholder="dev@domain.com"
                                class="w-full bg-[#121214] border border-white/5 hover:border-white/10 focus:border-cyan-500/50 rounded-lg py-2.5 pl-10 pr-4 text-zinc-100 placeholder-zinc-600 focus:outline-none focus:ring-1 focus:ring-cyan-500/50 transition-all text-sm font-sans"
                            />
                        </div>
                        <button 
                            type="button" 
                            @click="handleSendOtp"
                            :disabled="otpCooldown > 0 || !email || sendingOtp"
                            class="px-4 py-2.5 bg-emerald-500/10 hover:bg-emerald-500/20 disabled:bg-zinc-900 disabled:text-zinc-600 text-emerald-400 border border-emerald-500/20 disabled:border-white/5 text-xs font-medium rounded-lg transition-colors flex items-center justify-center gap-1.5 whitespace-nowrap min-w-[90px]"
                        >
                            <i v-if="sendingOtp" class="bi bi-arrow-repeat animate-spin"></i>
                            <span v-if="otpCooldown > 0">Resend in {{ otpCooldown }}s</span>
                            <span v-else>Send OTP</span>
                        </button>
                    </div>
                </div>
                
                <!-- OTP Verification Code Field -->
                <transition 
                    enter-active-class="transition ease-out duration-200" 
                    enter-from-class="opacity-0 -translate-y-2" 
                    enter-to-class="opacity-100 translate-y-0" 
                    leave-active-class="transition ease-in duration-150" 
                    leave-from-class="opacity-100 translate-y-0" 
                    leave-to-class="opacity-0 -translate-y-2"
                >
                    <AuthInput 
                        v-if="otpSent"
                        v-model="otp" 
                        label="Verification Code (OTP)" 
                        type="text" 
                        icon="bi-shield-lock" 
                        placeholder="••••••"
                        maxlength="6"
                    />
                </transition>

                <AuthInput 
                    v-model="password" 
                    label="Access Token" 
                    type="password" 
                    icon="bi-key" 
                    placeholder="••••••••••••"
                />

                <AuthInput 
                    v-model="confirmPassword" 
                    label="Verify Token" 
                    type="password" 
                    icon="bi-shield-check" 
                    placeholder="••••••••••••"
                />
            </div>

            <div class="pt-2">
                <button 
                    type="submit" 
                    :disabled="loading"
                    class="w-full bg-emerald-500 hover:bg-emerald-400 active:bg-emerald-600 text-zinc-950 font-sans font-medium text-sm py-2.5 px-4 rounded-lg transition-colors flex items-center justify-center gap-2 group disabled:opacity-50 disabled:cursor-not-allowed shadow-[0_1px_2px_rgba(0,0,0,0.05)]"
                >
                    <span v-if="!loading" class="flex items-center">
                        Request Access
                        <i class="bi bi-chevron-right ml-1 text-xs opacity-60 group-hover:opacity-100 group-hover:translate-x-0.5 transition-all"></i>
                    </span>
                    <span v-else class="flex items-center space-x-2">
                        <i class="bi bi-arrow-repeat animate-spin"></i>
                        <span>Provisioning...</span>
                    </span>
                </button>
            </div>
            
            <div class="text-center pt-6 border-t border-white/5">
                <div class="mt-2">
                    <router-link to="/login" class="text-xs text-zinc-400 hover:text-zinc-200 font-sans underline decoration-white/20 hover:decoration-white/60 transition-colors">
                        Return to authentication console
                    </router-link>
                </div>
            </div>
        </form>
    </div>
</template>
