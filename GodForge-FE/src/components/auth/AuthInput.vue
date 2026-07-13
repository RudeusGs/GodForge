<script setup lang="ts">
defineProps<{
    modelValue: string;
    label: string;
    type?: string;
    placeholder?: string;
    icon?: string;
    error?: string;
}>();

defineEmits<{
    (e: 'update:modelValue', value: string): void;
}>();
</script>

<template>
    <div class="flex flex-col space-y-1.5 font-sans">
        <label class="text-[11px] font-semibold text-zinc-400 uppercase tracking-wider">{{ label }}</label>
        <div class="relative group">
            <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-zinc-500 group-focus-within:text-cyan-500 transition-colors">
                <i v-if="icon" :class="['bi', icon]"></i>
            </div>
            <input 
                :type="type || 'text'" 
                :value="modelValue"
                @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
                :placeholder="placeholder"
                class="w-full bg-[#121214] border border-white/5 hover:border-white/10 focus:border-cyan-500/50 rounded-lg py-2.5 pl-10 pr-4 text-zinc-100 placeholder-zinc-600 focus:outline-none focus:ring-1 focus:ring-cyan-500/50 transition-all text-sm font-sans"
                :class="{ 'border-red-500/30 focus:border-red-500/50 focus:ring-red-500/30': error }"
            />
        </div>
        <span v-if="error" class="text-xs text-red-400 mt-1 font-mono">{{ error }}</span>
    </div>
</template>
