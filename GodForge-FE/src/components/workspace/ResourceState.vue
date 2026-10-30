<script setup lang="ts">
defineProps<{ loading: boolean; error: string }>();
defineEmits<{ retry: [] }>();
</script>
<template>
    <div v-if="loading" role="status" class="workspace-panel motion-safe:animate-pulse">Loading workspace…</div>
    <div v-else-if="error" role="alert" class="workspace-panel border-rose-800 text-rose-200">
        <p>{{ error }}</p><button class="workspace-button mt-4" @click="$emit('retry')">Retry</button>
    </div>
    <slot v-else />
</template>
