<script setup lang="ts">

defineProps<{
    type?: 'button' | 'submit' | 'reset';
    variant?: 'primary' | 'secondary' | 'ghost';
    loading?: boolean;
    disabled?: boolean;
}>();
</script>

<template>
    <button
        :type="type || 'button'"
        :disabled="disabled || loading"
        class="relative flex justify-center items-center w-full rounded-xl font-bold transition-all duration-300 py-3.5 px-6 overflow-hidden group focus:outline-none focus:ring-4 focus:ring-offset-2 focus:ring-offset-slate-900"
        :class="[
            variant === 'secondary' 
                ? 'bg-slate-800 hover:bg-slate-700 text-slate-200 focus:ring-slate-700 border border-slate-700 hover:border-slate-600 shadow-lg' 
                : variant === 'ghost'
                ? 'bg-transparent hover:bg-slate-800/50 text-slate-400 hover:text-slate-200 focus:ring-slate-800'
                : 'bg-gradient-to-r from-indigo-600 to-blue-500 hover:from-indigo-500 hover:to-blue-400 text-white focus:ring-indigo-500/50 shadow-lg shadow-indigo-500/25 hover:shadow-indigo-500/40 border border-indigo-400/20',
            (disabled || loading) ? 'opacity-60 cursor-not-allowed' : 'active:scale-[0.98]'
        ]"
    >
        <!-- Shine effect on hover for primary -->
        <div v-if="variant === 'primary' && !disabled && !loading" class="absolute inset-0 -translate-x-full bg-gradient-to-r from-transparent via-white/20 to-transparent group-hover:animate-[shimmer_1.5s_infinite]"></div>

        <span v-if="loading" class="absolute flex items-center justify-center">
            <i class="bi bi-arrow-repeat animate-spin text-xl"></i>
        </span>
        <span :class="{'opacity-0': loading, 'relative z-10 flex items-center gap-2': true}">
            <slot></slot>
        </span>
    </button>
</template>

<style scoped>
@keyframes shimmer {
  100% { transform: translateX(100%); }
}
</style>
