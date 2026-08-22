<script setup lang="ts">
import { useAuthStore } from '../stores/auth.store';
const authStore = useAuthStore();
</script>

<template>
    <main class="app-loading" role="status" aria-live="polite" aria-label="Loading GodForge">
        <div class="app-loading__ambient" aria-hidden="true"><span></span><span></span></div>
        <div class="app-loading__content">
            <div class="app-loading__mark" aria-hidden="true" :class="{ 'app-loading__mark--error': authStore.initializationError }">
                <svg viewBox="0 0 32 32" fill="none">
                    <path d="M8 7.5 16 3l8 4.5v9L16 21l-8-4.5v-9Z" fill="currentColor" />
                    <path d="M8 16.5V25l8 4 8-4v-8.5L16 21l-8-4.5Z" fill="currentColor" opacity=".44" />
                    <path d="m16 9 4 2.2v4.6L16 18l-4-2.2v-4.6L16 9Z" fill="white" />
                </svg>
            </div>
            <strong>GodForge</strong>
            
            <template v-if="authStore.initializationError">
                <p class="app-loading__error">Unable to verify session.</p>
                <button class="app-loading__retry" @click="authStore.initialize()" type="button">Retry connection</button>
            </template>
            <template v-else>
                <p>Preparing your workspace</p>
                <div class="app-loading__track" aria-hidden="true"><span></span></div>
            </template>
        </div>
    </main>
</template>

<style scoped>
.app-loading { position: fixed; inset: 0; z-index: 9999; display: grid; overflow: hidden; place-items: center; color: #111827; color-scheme: light; background: #f3f3ef; }.app-loading__ambient { position: absolute; inset: 0; pointer-events: none; }.app-loading__ambient span:first-child { position: absolute; width: 26rem; height: 26rem; right: -10rem; top: -12rem; border: 6rem solid rgb(49 87 246 / .06); border-radius: 50%; animation: loading-drift 5s ease-in-out infinite alternate; }.app-loading__ambient span:last-child { position: absolute; width: 10rem; height: 10rem; left: -5rem; bottom: 2rem; border-radius: 50%; background: #dce3ff; animation: loading-drift 4s ease-in-out infinite alternate-reverse; }.app-loading__content { position: relative; width: 13rem; text-align: center; animation: loading-in 420ms ease-out both; }.app-loading__mark { width: 3rem; height: 3rem; margin: 0 auto .8rem; color: #3157f6; animation: loading-mark 1.8s ease-in-out infinite; }.app-loading__mark--error { animation: none; color: #dc2626; opacity: 0.8; }.app-loading__mark svg { width: 100%; height: 100%; }.app-loading strong { display: block; font-size: 1.05rem; font-weight: 800; letter-spacing: -.04em; }.app-loading p { margin: .45rem 0 1rem; color: #838b98; font-size: .65rem; }.app-loading__error { color: #dc2626 !important; font-weight: 500; }.app-loading__retry { margin-top: 1rem; padding: 0.5rem 1rem; border-radius: 0.375rem; background: #3157f6; color: white; border: none; font-weight: 600; cursor: pointer; font-size: 0.875rem; transition: background-color 0.2s; }.app-loading__retry:hover { background: #2544c9; }.app-loading__retry:active { transform: translateY(1px); }.app-loading__track { height: 2px; overflow: hidden; border-radius: 1rem; background: #dfe2e8; }.app-loading__track span { display: block; width: 45%; height: 100%; background: #3157f6; animation: loading-progress 1.15s ease-in-out infinite; }
@keyframes loading-progress { from { transform: translateX(-110%); } to { transform: translateX(330%); } } @keyframes loading-mark { 50% { opacity: .72; transform: translateY(-4px) rotate(2deg); } } @keyframes loading-drift { to { transform: translate(1rem, 1rem); } } @keyframes loading-in { from { opacity: 0; transform: translateY(8px); } }
@media (prefers-reduced-motion: reduce) { .app-loading__ambient span, .app-loading__mark, .app-loading__track span, .app-loading__content { animation: none; }.app-loading__track span { width: 100%; } }
</style>
