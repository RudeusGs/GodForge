<script setup lang="ts">
defineProps<{ page: number; total: number; pageSize?: number }>();
defineEmits<{ change: [page: number] }>();
</script>
<template>
    <nav v-if="total > (pageSize ?? 20) || page > 1" aria-label="Pagination" class="mt-5 flex items-center gap-4">
        <button class="workspace-button" :disabled="page <= 1" @click="$emit('change', page - 1)">Previous</button>
        <span>Page {{ page }}</span>
        <button class="workspace-button" :disabled="page * (pageSize ?? 20) >= total" @click="$emit('change', page + 1)">Next</button>
    </nav>
</template>
