<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue';

const stageRef = ref<HTMLElement | null>(null);
const worldRef = ref<HTMLElement | null>(null);
let pointerTarget: HTMLElement | null = null;
let frameId: number | undefined;
let pointerX = 0;
let pointerY = 0;
let moveHandler: ((event: PointerEvent) => void) | undefined;
let leaveHandler: (() => void) | undefined;

onMounted(() => {
    const stage = stageRef.value;
    const supportsMotion = typeof window.matchMedia === 'function'
        && !window.matchMedia('(prefers-reduced-motion: reduce)').matches
        && window.matchMedia('(pointer: fine)').matches;
    if (!stage || !supportsMotion) return;
    pointerTarget = stage;

    const render = () => {
        frameId = undefined;
        if (!worldRef.value) return;
        const rect = stage.getBoundingClientRect();
        const x = (pointerX - rect.left) / Math.max(rect.width, 1) - .5;
        const y = (pointerY - rect.top) / Math.max(rect.height, 1) - .5;
        worldRef.value.style.transform = `rotateX(${-8 - y * 8}deg) rotateY(${x * 12}deg) translate3d(${x * 6}px, ${y * 5}px, 0)`;
        stage.style.setProperty('--light-x', `${(x + .5) * 100}%`);
        stage.style.setProperty('--light-y', `${(y + .5) * 100}%`);
    };

    moveHandler = (event: PointerEvent) => {
        pointerX = event.clientX;
        pointerY = event.clientY;
        if (frameId === undefined) frameId = window.requestAnimationFrame(render);
    };
    leaveHandler = () => {
        if (worldRef.value) worldRef.value.style.transform = '';
        stage.style.setProperty('--light-x', '50%');
        stage.style.setProperty('--light-y', '38%');
    };
    stage.addEventListener('pointermove', moveHandler, { passive: true });
    stage.addEventListener('pointerleave', leaveHandler);
});

onUnmounted(() => {
    if (pointerTarget && moveHandler) pointerTarget.removeEventListener('pointermove', moveHandler);
    if (pointerTarget && leaveHandler) pointerTarget.removeEventListener('pointerleave', leaveHandler);
    if (frameId !== undefined) window.cancelAnimationFrame(frameId);
    pointerTarget = null;
});
</script>

<template>
    <div ref="stageRef" class="scene-stage" aria-hidden="true">
        <div class="scene-stage__glow"></div>
        <div ref="worldRef" class="scene-world">
            <div class="scene-grid"></div>

            <div class="scene-card scene-card--main">
                <div class="scene-card__top">
                    <span><i></i><i></i><i></i></span>
                    <b>emberfall / world.tscn</b>
                    <em>LIVE</em>
                </div>
                <div class="scene-card__body">
                    <aside>
                        <span>SCENE TREE</span>
                        <b>◇ World</b><small>⌞ Player</small><small>⌞ Environment</small><small>⌞ HUD</small>
                    </aside>
                    <div class="scene-map">
                        <svg viewBox="0 0 320 190" fill="none">
                            <path d="M160 30 75 90m85-60 85 60M75 90l45 70m-45-70-42 70m212-70-45 70m45-70 42 70" stroke="url(#landing-line)" stroke-width="1.2" />
                            <defs><linearGradient id="landing-line"><stop stop-color="#3157f6"/><stop offset="1" stop-color="#9bacff" stop-opacity=".35"/></linearGradient></defs>
                        </svg>
                        <span class="scene-node scene-node--root">World</span>
                        <span class="scene-node scene-node--left">Player.gd</span>
                        <span class="scene-node scene-node--right">GameState</span>
                        <i class="scene-dot scene-dot--one"></i><i class="scene-dot scene-dot--two"></i><i class="scene-dot scene-dot--three"></i><i class="scene-dot scene-dot--four"></i>
                    </div>
                </div>
                <div class="scene-card__status"><span><i></i> Analysis complete</span><b>92 / 100</b></div>
            </div>

            <div class="scene-float scene-float--health"><small>PROJECT HEALTH</small><strong>92</strong><span>Excellent</span><div><i></i></div></div>
            <div class="scene-float scene-float--commit"><small>LATEST REVISION</small><strong>8e4a2d1</strong><span>deterministic · verified</span></div>
            <div class="scene-badge scene-badge--one">24 scenes</div>
            <div class="scene-badge scene-badge--two">0 critical</div>
        </div>
    </div>
</template>

<style scoped>
.scene-stage { --light-x: 50%; --light-y: 38%; position: relative; min-height: 38rem; perspective: 1200px; perspective-origin: 50% 42%; }.scene-stage__glow { position: absolute; inset: 0; background: radial-gradient(circle at var(--light-x) var(--light-y), rgb(85 111 255 / .25), transparent 42%); filter: blur(6px); transition: background 120ms linear; }.scene-world { position: absolute; inset: 7% 2% 5%; transform: rotateX(-8deg); transform-style: preserve-3d; transition: transform 480ms cubic-bezier(.22,.75,.24,1); will-change: transform; }
.scene-grid { position: absolute; width: 100%; height: 75%; left: 0; bottom: -15%; opacity: .35; background-image: linear-gradient(rgb(49 87 246 / .22) 1px, transparent 1px), linear-gradient(90deg, rgb(49 87 246 / .22) 1px, transparent 1px); background-size: 2rem 2rem; mask-image: linear-gradient(to bottom, transparent, black 30%, transparent 92%); transform: rotateX(72deg) translateZ(-7rem); transform-origin: bottom; }
.scene-card { position: absolute; overflow: hidden; border: 1px solid rgb(255 255 255 / .7); border-radius: 1.2rem; color: #394257; background: rgb(255 255 255 / .78); box-shadow: 0 32px 70px rgb(32 52 120 / .17), inset 0 1px rgb(255 255 255 / .9); backdrop-filter: blur(16px); }.scene-card--main { width: 82%; height: 63%; left: 9%; top: 14%; transform: translateZ(2rem); }
.scene-card__top { display: grid; height: 2.7rem; align-items: center; padding: 0 1rem; border-bottom: 1px solid #e9ebf1; grid-template-columns: 1fr 2fr 1fr; font-size: .56rem; }.scene-card__top > span { display: flex; gap: .28rem; }.scene-card__top i { width: .36rem; height: .36rem; border-radius: 50%; background: #c9ced9; }.scene-card__top b { color: #606a7d; font-weight: 650; text-align: center; }.scene-card__top em { justify-self: end; padding: .25rem .42rem; border-radius: 999px; color: #3157f6; background: #edf0ff; font-size: .48rem; font-style: normal; font-weight: 800; letter-spacing: .08em; }
.scene-card__body { display: grid; height: calc(100% - 4.9rem); grid-template-columns: 8rem 1fr; }.scene-card__body aside { display: flex; padding: 1rem .8rem; border-right: 1px solid #eceef3; flex-direction: column; gap: .55rem; background: rgb(247 248 250 / .65); font-size: .55rem; }.scene-card__body aside > span { margin-bottom: .4rem; color: #9aa2b0; font-size: .45rem; font-weight: 800; letter-spacing: .12em; }.scene-card__body aside b { padding: .42rem; border-radius: .38rem; color: #3157f6; background: #edf0ff; }.scene-card__body aside small { padding-left: .55rem; color: #788295; }
.scene-map { position: relative; overflow: hidden; background-image: radial-gradient(#ccd2df .7px, transparent .7px); background-size: 1rem 1rem; }.scene-map svg { position: absolute; inset: 8% 4% 2%; width: 92%; height: 90%; }.scene-node { position: absolute; z-index: 2; padding: .42rem .58rem; border: 1px solid #dfe3eb; border-radius: .45rem; color: #3c465a; background: #fff; box-shadow: 0 8px 18px rgb(32 52 120 / .08); font-size: .5rem; font-weight: 720; }.scene-node--root { left: 50%; top: 13%; color: #fff; border-color: #3157f6; background: #3157f6; transform: translateX(-50%); }.scene-node--left { left: 14%; top: 46%; }.scene-node--right { right: 14%; top: 46%; }.scene-dot { position: absolute; width: .45rem; height: .45rem; border-radius: 50%; background: #96a9ff; box-shadow: 0 0 0 .3rem rgb(49 87 246 / .08); }.scene-dot--one { left: 10%; bottom: 16%; }.scene-dot--two { left: 35%; bottom: 16%; }.scene-dot--three { right: 35%; bottom: 16%; }.scene-dot--four { right: 10%; bottom: 16%; }
.scene-card__status { display: flex; height: 2.2rem; align-items: center; justify-content: space-between; padding: 0 1rem; border-top: 1px solid #eceef3; color: #818a9a; font-size: .5rem; }.scene-card__status span { display: flex; align-items: center; gap: .35rem; }.scene-card__status span i { width: .35rem; height: .35rem; border-radius: 50%; background: #34b986; }.scene-card__status b { color: #3157f6; }
.scene-float { position: absolute; z-index: 4; display: flex; padding: .9rem; border: 1px solid rgb(255 255 255 / .75); border-radius: .9rem; flex-direction: column; background: rgb(255 255 255 / .88); box-shadow: 0 18px 40px rgb(32 52 120 / .14); backdrop-filter: blur(14px); animation: scene-float 5s ease-in-out infinite alternate; }.scene-float small { color: #9aa2b1; font-size: .42rem; font-weight: 800; letter-spacing: .11em; }.scene-float strong { margin-top: .35rem; color: #3157f6; font-size: 1.65rem; letter-spacing: -.08em; }.scene-float span { color: #7d8797; font-size: .48rem; }.scene-float--health { width: 7.5rem; right: 0; top: 5%; transform: translateZ(6rem); }.scene-float--health div { height: .22rem; margin-top: .65rem; border-radius: 1rem; background: #e7eaf0; }.scene-float--health div i { display: block; width: 92%; height: 100%; border-radius: inherit; background: #3157f6; }.scene-float--commit { width: 9rem; left: 0; bottom: 6%; transform: translateZ(5rem); animation-delay: -2.5s; }.scene-float--commit strong { color: #252e40; font-family: ui-monospace, monospace; font-size: .85rem; letter-spacing: 0; }
.scene-badge { position: absolute; z-index: 5; padding: .55rem .72rem; border: 1px solid rgb(255 255 255 / .7); border-radius: 999px; color: #566175; background: rgb(255 255 255 / .8); box-shadow: 0 10px 24px rgb(32 52 120 / .12); backdrop-filter: blur(12px); font-size: .5rem; font-weight: 750; }.scene-badge--one { left: 4%; top: 10%; transform: translateZ(4rem); }.scene-badge--two { right: 6%; bottom: 10%; color: #25815f; transform: translateZ(7rem); }
@keyframes scene-float { to { translate: 0 -.65rem; } }
@media (max-width: 900px) { .scene-stage { min-height: 31rem; }.scene-world { inset: 2% 0; }.scene-card--main { width: 88%; left: 6%; }.scene-float--health { right: 2%; }.scene-float--commit { left: 2%; } }
@media (max-width: 560px) { .scene-stage { min-height: 25rem; }.scene-card--main { height: 68%; top: 13%; }.scene-card__body { grid-template-columns: 5rem 1fr; }.scene-card__body aside { padding: .7rem .45rem; }.scene-card__body aside small:nth-of-type(n+3) { display: none; }.scene-float { transform: scale(.82); }.scene-float--health { transform-origin: right top; }.scene-float--commit { transform-origin: left bottom; }.scene-badge { display: none; } }
@media (prefers-reduced-motion: reduce) { .scene-world { transition: none; }.scene-float { animation: none; } }
</style>
