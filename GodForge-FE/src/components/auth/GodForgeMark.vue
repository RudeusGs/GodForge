<script setup lang="ts">
withDefaults(defineProps<{ compact?: boolean }>(), { compact: false });
</script>

<template>
    <span class="wordmark" :class="{ 'wordmark--compact': compact }">
        <span class="wordmark__symbol" aria-hidden="true">
            <svg viewBox="0 0 32 32" fill="none">
                <path d="M8 7.5 16 3l8 4.5v9L16 21l-8-4.5v-9Z" fill="currentColor" />
                <path d="M8 16.5V25l8 4 8-4v-8.5L16 21l-8-4.5Z" fill="currentColor" opacity=".44" />
                <path d="m16 9 4 2.2v4.6L16 18l-4-2.2v-4.6L16 9Z" fill="white" />
            </svg>
        </span>
        <strong v-if="!compact">GodForge</strong>
    </span>
</template>

<style scoped>
.wordmark { display: inline-flex; align-items: center; gap: .68rem; color: #3157f6; }
.wordmark__symbol { display: block; width: 2rem; height: 2rem; }.wordmark__symbol svg { width: 100%; height: 100%; }
.wordmark strong { color: #111827; font-size: 1rem; font-weight: 800; letter-spacing: -.035em; }
.wordmark--compact .wordmark__symbol { width: 1.8rem; height: 1.8rem; }
</style>
