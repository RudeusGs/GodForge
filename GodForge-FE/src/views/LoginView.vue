<script setup lang="ts">
import LoginPanel from '../components/auth/LoginPanel.vue';
import LoginHeroVisual from '../components/auth/LoginHeroVisual.vue';
import InteractiveDotGrid from '../components/auth/InteractiveDotGrid.vue';
</script>

<template>
    <div class="min-h-screen bg-[#09090b] flex flex-col lg:flex-row font-sans selection:bg-cyan-500/30 selection:text-cyan-100">
        <InteractiveDotGrid />
        <!-- Left Panel: Auth Form -->
        <div class="w-full lg:w-[480px] xl:w-[560px] bg-[#09090b] flex-shrink-0 flex items-center justify-center border-r border-white/10 z-10 shadow-[20px_0_50px_rgba(0,0,0,0.5)]">
            <LoginPanel />
        </div>
        
        <!-- Right Panel: Visual Hero (hidden on mobile, visible on desktop) -->
        <div class="flex-1 hidden lg:block relative">
            <LoginHeroVisual />
        </div>
    </div>
</template>

<style>
/* Base dark aesthetic body fallback if not applied globally */
body {
    background-color: #020204;
    color: #f8f8f8;
}
</style>
