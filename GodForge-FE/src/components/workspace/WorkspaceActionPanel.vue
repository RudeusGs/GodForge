<script setup lang="ts">
import { onMounted, onScopeDispose, useTemplateRef } from 'vue';
const panel = useTemplateRef<HTMLElement>('panel');
let previous: HTMLElement | null = null;
onMounted(() => {
    previous = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    panel.value?.focus();
});
onScopeDispose(() => { if (previous?.isConnected) previous.focus(); });
</script>
<template><section ref="panel" role="dialog" aria-modal="false" tabindex="-1" class="workspace-panel my-5"><slot /></section></template>
