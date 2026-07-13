<script setup lang="ts">
defineProps<{
    status: 'online' | 'warning' | 'error' | 'loading';
    label: string;
}>();
</script>

<template>
    <div class="inline-flex items-center space-x-2 bg-[#12121a]/80 backdrop-blur-sm border border-white/5 px-3 py-1.5 rounded-full text-xs font-mono">
        <div class="relative flex h-2 w-2">
            <span v-if="status === 'online'" class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
            <span :class="[
                'relative inline-flex rounded-full h-2 w-2',
                status === 'online' ? 'bg-emerald-500' : '',
                status === 'warning' ? 'bg-amber-500' : '',
                status === 'error' ? 'bg-red-500' : '',
                status === 'loading' ? 'bg-cyan-500 animate-pulse' : ''
            ]"></span>
        </div>
        <span class="text-[#a1a1aa]">{{ label }}</span>
    </div>
</template>
