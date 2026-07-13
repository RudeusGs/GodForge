<script setup lang="ts">
import { computed, ref } from 'vue';

const props = defineProps<{
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

const inputId = computed(() => `input-${props.label.replace(/\s+/g, '-').toLowerCase()}`);
const isFocused = ref(false);
</script>

<template>
    <div class="flex flex-col space-y-1.5 mb-5 text-left w-full relative">
        <label :for="inputId" class="text-sm font-semibold text-slate-300 transition-colors duration-300" :class="{'text-indigo-400': isFocused}">
            {{ label }}
        </label>
        <div class="relative group">
            <span v-if="icon" class="absolute inset-y-0 left-0 flex items-center pl-3.5 text-slate-400 transition-colors duration-300" :class="{'text-indigo-400': isFocused}">
                <i :class="['bi', icon, 'text-lg']"></i>
            </span>
            <input 
                :id="inputId"
                :type="type || 'text'"
                :value="modelValue"
                @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
                @focus="isFocused = true"
                @blur="isFocused = false"
                :placeholder="placeholder"
                class="w-full bg-slate-950/50 text-slate-100 rounded-xl border transition-all duration-300 placeholder:text-slate-500"
                :class="[
                    icon ? 'pl-11 pr-4 py-3' : 'px-4 py-3',
                    error 
                        ? 'border-rose-500/50 focus:border-rose-500 focus:ring-4 focus:ring-rose-500/10' 
                        : 'border-slate-700/50 hover:border-slate-600 focus:border-indigo-500 focus:ring-4 focus:ring-indigo-500/20'
                ]"
            />
        </div>
        <span v-if="error" class="text-xs text-rose-400 mt-1 flex items-center animate-pulse">
            <i class="bi bi-exclamation-circle-fill mr-1.5"></i> {{ error }}
        </span>
    </div>
</template>
