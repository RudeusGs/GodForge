<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import GodForgeMark from './GodForgeMark.vue';

const route = useRoute();
const pageRef = ref<HTMLElement | null>(null);
const cursorLightRef = ref<HTMLElement | null>(null);
const messageRef = ref<HTMLElement | null>(null);
const cardRef = ref<HTMLElement | null>(null);
let animationFrameId: number | undefined;
let pointerX = 0;
let pointerY = 0;
let pointerHandler: ((event: PointerEvent) => void) | undefined;
let pointerLeaveHandler: (() => void) | undefined;
let pointerTarget: HTMLElement | null = null;
const isRegister = computed(() => route.name === 'register');
const isRecovery = computed(() => route.name === 'forgotPassword' || route.name === 'resetPassword');
const pageCopy = computed(() => isRecovery.value ? {
    index: '02 — ACCOUNT RECOVERY',
    title: 'Return safely.',
    accent: 'Keep building.',
    description: 'Recover access without exposing whether an account exists or weakening your active sessions.',
} : {
    index: '01 — PROJECT INTELLIGENCE',
    title: 'Build games.',
    accent: 'Know the system.',
    description: 'GodForge turns complex Godot projects into clear, traceable engineering systems.',
});

onMounted(() => {
    const page = pageRef.value;
    if (!page || window.matchMedia('(prefers-reduced-motion: reduce)').matches || !window.matchMedia('(pointer: fine)').matches) return;
    pointerTarget = page;

    const renderPointerEffects = () => {
        animationFrameId = undefined;
        const light = cursorLightRef.value;
        if (!light) return;
        const rect = page.getBoundingClientRect();
        const localX = pointerX - rect.left;
        const localY = pointerY - rect.top;
        const normalizedX = localX / Math.max(rect.width, 1) - .5;
        const normalizedY = localY / Math.max(rect.height, 1) - .5;
        light.style.transform = `translate3d(${localX - 190}px, ${localY - 190}px, 0)`;
        light.style.opacity = '1';
        if (messageRef.value) messageRef.value.style.translate = `${normalizedX * 8}px ${normalizedY * 6}px`;
        if (cardRef.value) cardRef.value.style.translate = `${normalizedX * -4}px ${normalizedY * -3}px`;
    };

    pointerHandler = (event: PointerEvent) => {
        pointerX = event.clientX;
        pointerY = event.clientY;
        if (animationFrameId === undefined) animationFrameId = window.requestAnimationFrame(renderPointerEffects);
    };
    pointerLeaveHandler = () => {
        if (cursorLightRef.value) cursorLightRef.value.style.opacity = '0';
        if (messageRef.value) messageRef.value.style.translate = '0 0';
        if (cardRef.value) cardRef.value.style.translate = '0 0';
    };
    page.addEventListener('pointermove', pointerHandler, { passive: true });
    page.addEventListener('pointerleave', pointerLeaveHandler);
});

onUnmounted(() => {
    if (pointerTarget && pointerHandler) pointerTarget.removeEventListener('pointermove', pointerHandler);
    if (pointerTarget && pointerLeaveHandler) pointerTarget.removeEventListener('pointerleave', pointerLeaveHandler);
    if (animationFrameId !== undefined) window.cancelAnimationFrame(animationFrameId);
    pointerTarget = null;
});
</script>

<template>
    <main ref="pageRef" class="auth-page" :class="{ 'auth-page--register': isRegister }">
        <div class="auth-page__vfx" aria-hidden="true">
            <span ref="cursorLightRef" class="auth-page__cursor-light"></span>
            <span class="auth-page__halo"></span>
            <span class="auth-page__beam"></span>
            <span class="auth-page__orb auth-page__orb--one"></span>
            <span class="auth-page__orb auth-page__orb--two"></span>
            <span class="auth-page__spark auth-page__spark--one">+</span>
            <span class="auth-page__spark auth-page__spark--two">+</span>
        </div>
        <header class="auth-page__nav">
            <router-link to="/" class="auth-page__brand" aria-label="GodForge home">
                <GodForgeMark />
            </router-link>
            <div class="auth-page__nav-action">
                <span>{{ isRecovery ? 'Remembered your password?' : isRegister ? 'Already a member?' : 'New to GodForge?' }}</span>
                <router-link :to="isRecovery || isRegister ? '/login' : '/register'">
                    {{ isRecovery || isRegister ? 'Sign in' : 'Create account' }}
                </router-link>
            </div>
        </header>

        <div class="auth-page__main">
            <section ref="messageRef" class="auth-page__message" aria-label="About GodForge">
                <div class="auth-page__index">{{ pageCopy.index }}</div>
                <h2>{{ pageCopy.title }}<br><em>{{ pageCopy.accent }}</em></h2>
                <p>{{ pageCopy.description }}</p>
                <div class="auth-page__signals">
                    <div><strong>Scenes</strong><span>Mapped clearly</span></div>
                    <div><strong>Dependencies</strong><span>Tracked end to end</span></div>
                    <div><strong>Health</strong><span>Measured with evidence</span></div>
                </div>
            </section>

            <section ref="cardRef" class="auth-page__card" aria-label="Account form">
                <slot />
            </section>
        </div>

        <footer class="auth-page__footer">
            <span>GodForge · Built for Godot teams</span>
            <span>Private by design</span>
        </footer>
    </main>
</template>

<style scoped>
.auth-page { --ink: #111827; --muted: #697386; --line: #dfe2e8; --blue: #3157f6; position: relative; display: flex; min-height: 100svh; overflow: hidden; flex-direction: column; color: var(--ink); color-scheme: light; background: #f3f3ef; isolation: isolate; }
.auth-page__vfx { position: absolute; inset: 0; z-index: -1; overflow: hidden; pointer-events: none; }
.auth-page__cursor-light { position: absolute; width: 23.75rem; height: 23.75rem; left: 0; top: 0; opacity: 0; border-radius: 50%; background: radial-gradient(circle, rgb(87 112 255 / .16) 0, rgb(124 142 255 / .07) 34%, transparent 70%); filter: blur(8px); will-change: transform, opacity; transition: opacity 220ms ease; }
.auth-page__halo { position: absolute; width: 34rem; height: 34rem; right: -13rem; top: -15rem; border: 7rem solid rgb(49 87 246 / .055); border-radius: 50%; animation: halo-drift 14s ease-in-out infinite alternate; }
.auth-page__beam { position: absolute; width: 16rem; height: 120%; left: 43%; top: -10%; opacity: .48; background: linear-gradient(90deg, transparent, rgb(255 255 255 / .72), transparent); filter: blur(12px); transform: rotate(13deg) translateX(-30vw); animation: beam-sweep 11s ease-in-out infinite; }
.auth-page__orb { position: absolute; border-radius: 50%; filter: blur(1px); }.auth-page__orb--one { width: 12rem; height: 12rem; left: -6rem; bottom: 4rem; background: #dce3ff; animation: orb-float 9s ease-in-out infinite alternate; }.auth-page__orb--two { width: 4rem; height: 4rem; right: 8%; bottom: 13%; border: 1px solid rgb(49 87 246 / .2); background: rgb(255 255 255 / .35); animation: orb-float 7s 1s ease-in-out infinite alternate-reverse; }
.auth-page__spark { position: absolute; color: rgb(49 87 246 / .22); font-family: Georgia, serif; font-size: 1.4rem; font-weight: 300; animation: spark-float 6s ease-in-out infinite alternate; }.auth-page__spark--one { left: 7%; top: 24%; }.auth-page__spark--two { right: 4%; top: 56%; animation-delay: -2.5s; }
.auth-page__nav { display: flex; width: min(100% - 3rem, 76rem); margin: 0 auto; align-items: center; justify-content: space-between; padding: 1.7rem 0; }
.auth-page__brand { color: inherit; text-decoration: none; }
.auth-page__nav-action { display: flex; align-items: center; gap: .95rem; color: var(--muted); font-size: .78rem; }
.auth-page__nav-action a { padding: .62rem .9rem; border: 1px solid #cfd3dc; border-radius: 999px; color: var(--ink); background: rgb(255 255 255 / .5); font-weight: 700; text-decoration: none; transition: border-color 140ms ease, background 140ms ease; }
.auth-page__nav-action a:hover { border-color: #aeb5c2; background: #fff; }
.auth-page__main { display: grid; width: min(100% - 3rem, 68rem); margin: auto; align-items: center; gap: clamp(4rem, 10vw, 9rem); grid-template-columns: minmax(0, 1fr) minmax(23rem, 28rem); padding: 3rem 0; }
.auth-page--register .auth-page__main { width: min(100% - 3rem, 74rem); grid-template-columns: minmax(18rem, .75fr) minmax(32rem, 1.25fr); gap: clamp(3rem, 7vw, 7rem); }
.auth-page__index { margin-bottom: 2rem; color: var(--blue); font-size: .66rem; font-weight: 800; letter-spacing: .15em; }
.auth-page__message { will-change: translate; transition: translate 240ms cubic-bezier(.22,.75,.24,1); animation: content-in 650ms cubic-bezier(.22,.75,.24,1) both; }.auth-page__message h2 { margin: 0; font-size: clamp(3rem, 5.2vw, 5.8rem); font-weight: 760; line-height: .93; letter-spacing: -.065em; }
.auth-page__message h2 em { color: var(--blue); font-family: Georgia, 'Times New Roman', serif; font-weight: 500; }
.auth-page__message > p { max-width: 31rem; margin: 1.8rem 0 0; color: var(--muted); font-size: .98rem; line-height: 1.75; }
.auth-page__signals { display: grid; margin-top: clamp(2.5rem, 7vh, 5rem); gap: 1rem; grid-template-columns: repeat(3, 1fr); }
.auth-page__signals div { padding-top: .9rem; border-top: 1px solid #cfd3da; }
.auth-page__signals strong, .auth-page__signals span { display: block; }.auth-page__signals strong { font-size: .72rem; }.auth-page__signals span { margin-top: .28rem; color: #8a93a3; font-size: .62rem; line-height: 1.4; }
.auth-page__card { min-width: 0; padding: clamp(2rem, 4vw, 3.25rem); border: 1px solid #e1e3e8; border-radius: 1.4rem; background: rgb(255 255 255 / .94); box-shadow: 0 24px 70px rgb(23 31 54 / .09); backdrop-filter: blur(10px); will-change: translate; transition: translate 260ms cubic-bezier(.22,.75,.24,1), box-shadow 220ms ease; animation: card-in 720ms 80ms cubic-bezier(.22,.75,.24,1) both; }.auth-page__card:hover { box-shadow: 0 28px 78px rgb(23 31 54 / .12); }
.auth-page__footer { display: flex; width: min(100% - 3rem, 76rem); margin: 0 auto; justify-content: space-between; padding: 1.5rem 0; color: #949aa7; font-size: .65rem; }
@keyframes card-in { from { opacity: 0; transform: translateY(22px) scale(.985); } to { opacity: 1; transform: none; } }
@keyframes content-in { from { opacity: 0; transform: translateX(-18px); } to { opacity: 1; transform: none; } }
@keyframes halo-drift { to { transform: translate(-2rem, 2rem) rotate(8deg); } }
@keyframes orb-float { to { transform: translate(1.1rem, -1.4rem); } }
@keyframes spark-float { to { opacity: .7; transform: translateY(-1rem) rotate(45deg); } }
@keyframes beam-sweep { 0%, 18% { opacity: 0; transform: rotate(13deg) translateX(-45vw); } 42%, 58% { opacity: .45; } 82%, 100% { opacity: 0; transform: rotate(13deg) translateX(55vw); } }
@media (max-width: 900px) {
    .auth-page { overflow: auto; }
    .auth-page__main, .auth-page--register .auth-page__main { display: block; width: min(100% - 2rem, 35rem); padding: 2rem 0 3rem; }
    .auth-page__message { margin-bottom: 2.2rem; }.auth-page__message h2 { font-size: clamp(2.65rem, 12vw, 4.2rem); }.auth-page__message > p { margin-top: 1rem; }.auth-page__index { margin-bottom: 1rem; }.auth-page__signals { display: none; }
    .auth-page__card { padding: clamp(1.5rem, 6vw, 2.5rem); }
}
@media (max-width: 520px) { .auth-page__nav-action span { display: none; }.auth-page__footer span:last-child { display: none; } }
@media (prefers-reduced-motion: reduce) { .auth-page__cursor-light { display: none; }.auth-page__halo, .auth-page__beam, .auth-page__orb, .auth-page__spark, .auth-page__message, .auth-page__card { animation: none; transition: none; } }
</style>
