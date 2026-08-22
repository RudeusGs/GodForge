<script setup lang="ts">
import { computed } from 'vue';
import GodForgeMark from '@/components/auth/GodForgeMark.vue';
import LandingCapabilities from '@/components/landing/LandingCapabilities.vue';
import LandingScene3D from '@/components/landing/LandingScene3D.vue';
import { useAuthStore } from '@/stores/auth.store';

const authStore = useAuthStore();
const isAuthenticated = computed(() => authStore.isAuthenticated);
</script>

<template>
    <div class="landing">
        <div class="landing__ambient" aria-hidden="true"><span></span><span></span><span></span></div>

        <header class="landing__header">
            <router-link to="/" class="landing__brand" aria-label="GodForge home"><GodForgeMark /></router-link>
            <nav class="landing__nav" aria-label="Primary navigation">
                <a href="#capabilities">Capabilities</a>
                <a href="#workflow">How it works</a>
                <a href="#principles">Principles</a>
            </nav>
            <div class="landing__actions">
                <router-link v-if="!isAuthenticated" to="/login" class="landing__text-link">Sign in</router-link>
                <router-link :to="isAuthenticated ? '/dashboard' : '/register'" class="landing__pill">
                    {{ isAuthenticated ? 'Open dashboard' : 'Start exploring' }} <span aria-hidden="true">↗</span>
                </router-link>
            </div>
        </header>

        <main>
            <section class="hero">
                <div class="hero__copy">
                    <div class="hero__label"><span></span> GODOT PROJECT INTELLIGENCE</div>
                    <h1>See every layer.<br><em>Shape what comes next.</em></h1>
                    <p>A specialized Git and project-intelligence platform that transforms Godot scenes, scripts, assets and dependencies into a system your team can understand.</p>
                    <div class="hero__actions">
                        <router-link :to="isAuthenticated ? '/dashboard' : '/register'" class="hero__primary">
                            {{ isAuthenticated ? 'Go to your workspace' : 'Create your workspace' }} <span aria-hidden="true">→</span>
                        </router-link>
                        <a href="#capabilities" class="hero__secondary"><span aria-hidden="true">↓</span> Explore the platform</a>
                    </div>
                    <div class="hero__trust">
                        <span><i>01</i> Deterministic first</span><span><i>02</i> Git native</span><span><i>03</i> Least privilege</span>
                    </div>
                </div>
                <LandingScene3D />
            </section>

            <div class="signal-strip" aria-label="GodForge capability summary">
                <div class="signal-strip__track">
                    <span>SCENE INTELLIGENCE</span><i>✦</i><span>DEPENDENCY GRAPH</span><i>✦</i><span>HEALTH ANALYSIS</span><i>✦</i><span>REVISION EVIDENCE</span><i>✦</i><span>ASSET VISIBILITY</span>
                </div>
            </div>

            <LandingCapabilities />

            <section id="workflow" class="workflow landing-section">
                <div class="workflow__intro">
                    <span>ONE TRACEABLE FLOW</span>
                    <h2>Push code.<br><em>Receive clarity.</em></h2>
                    <p>GodForge connects Git history to a bounded, repeatable project-intelligence pipeline.</p>
                </div>
                <ol class="workflow__steps">
                    <li><b>01</b><div><strong>Connect a revision</strong><span>Hosted or linked Git, resolved to an immutable commit.</span></div></li>
                    <li><b>02</b><div><strong>Validate safely</strong><span>Inspect project structure without executing untrusted code.</span></div></li>
                    <li><b>03</b><div><strong>Map the system</strong><span>Parse scenes, resources, scripts and dependency relationships.</span></div></li>
                    <li><b>04</b><div><strong>Act on evidence</strong><span>Review health findings, impact and optional AI advisory.</span></div></li>
                </ol>
            </section>

            <section id="principles" class="principles landing-section">
                <div class="principles__orb" aria-hidden="true"><span>GF</span></div>
                <div class="principles__content">
                    <span>BUILT ON A CLEAR PRINCIPLE</span>
                    <blockquote>“Deterministic before generative.”</blockquote>
                    <p>The parser and rule engine remain authoritative. AI is optional, bounded and visibly advisory.</p>
                </div>
                <div class="principles__list">
                    <span>No untrusted execution</span><span>Immutable revision analysis</span><span>Evidence over claims</span><span>Least privilege</span>
                </div>
            </section>

            <section class="landing-cta">
                <div><span>READY WHEN YOUR PROJECT IS</span><h2>Turn complexity into<br><em>shared understanding.</em></h2></div>
                <router-link :to="isAuthenticated ? '/dashboard' : '/register'">
                    {{ isAuthenticated ? 'Open GodForge' : 'Build with GodForge' }} <span aria-hidden="true">→</span>
                </router-link>
            </section>
        </main>

        <footer class="landing__footer">
            <GodForgeMark />
            <p>A specialized Git and project-intelligence platform for Godot Engine teams.</p>
            <div><router-link to="/login">Sign in</router-link><router-link to="/register">Create account</router-link></div>
        </footer>
    </div>
</template>

<style scoped>
.landing { --ink: #111827; --muted: #6f7888; --blue: #3157f6; position: relative; min-height: 100vh; overflow: hidden; color: var(--ink); color-scheme: light; background: #f3f3ef; }.landing__ambient { position: absolute; inset: 0; z-index: 0; pointer-events: none; }.landing__ambient span { position: absolute; border-radius: 50%; }.landing__ambient span:nth-child(1) { width: 36rem; height: 36rem; right: -20rem; top: 4rem; border: 8rem solid rgb(49 87 246 / .045); }.landing__ambient span:nth-child(2) { width: 18rem; height: 18rem; left: -10rem; top: 42rem; background: #e1e6ff; }.landing__ambient span:nth-child(3) { width: 7rem; height: 7rem; right: 5%; top: 68rem; border: 1px solid rgb(49 87 246 / .18); }
.landing__header { position: relative; z-index: 20; display: grid; width: min(100% - 3rem, 78rem); margin: 0 auto; align-items: center; padding: 1.55rem 0; grid-template-columns: 1fr auto 1fr; }.landing__brand { width: fit-content; color: inherit; text-decoration: none; }.landing__nav { display: flex; gap: 1.8rem; }.landing__nav a, .landing__text-link { color: #727b8a; font-size: .68rem; font-weight: 680; text-decoration: none; }.landing__nav a:hover, .landing__text-link:hover { color: var(--blue); }.landing__actions { display: flex; justify-self: end; align-items: center; gap: 1rem; }.landing__pill { display: inline-flex; align-items: center; gap: .45rem; padding: .68rem .95rem; border: 1px solid #cfd3dc; border-radius: 999px; color: var(--ink); background: rgb(255 255 255 / .55); font-size: .66rem; font-weight: 760; text-decoration: none; transition: background 150ms ease, transform 150ms ease; }.landing__pill:hover { background: #fff; transform: translateY(-1px); }
.hero { position: relative; z-index: 1; display: grid; width: min(100% - 3rem, 78rem); min-height: calc(100svh - 5.5rem); margin: 0 auto; align-items: center; gap: clamp(2rem, 4vw, 5rem); grid-template-columns: minmax(26rem, .9fr) minmax(32rem, 1.1fr); padding: 3rem 0 5rem; }.hero__copy { animation: hero-in 700ms cubic-bezier(.22,.75,.24,1) both; }.hero__label { display: flex; align-items: center; gap: .55rem; color: var(--blue); font-size: .62rem; font-weight: 820; letter-spacing: .15em; }.hero__label span { width: .45rem; height: .45rem; border-radius: 50%; background: var(--blue); box-shadow: 0 0 0 .35rem rgb(49 87 246 / .1); }.hero h1 { margin: 1.5rem 0 0; font-size: clamp(3.7rem, 6.6vw, 7rem); font-weight: 780; line-height: .9; letter-spacing: -.075em; }.hero h1 em, .workflow h2 em, .landing-cta h2 em { color: var(--blue); font-family: Georgia, 'Times New Roman', serif; font-weight: 500; }.hero__copy > p { max-width: 35rem; margin: 1.8rem 0 0; color: var(--muted); font-size: .92rem; line-height: 1.75; }.hero__actions { display: flex; flex-wrap: wrap; gap: .8rem; margin-top: 2rem; }.hero__primary, .hero__secondary { display: inline-flex; min-height: 3.2rem; align-items: center; justify-content: center; gap: .65rem; padding: 0 1.15rem; border-radius: .68rem; font-size: .7rem; font-weight: 770; text-decoration: none; transition: transform 150ms ease, box-shadow 150ms ease; }.hero__primary { color: #fff; background: var(--blue); box-shadow: 0 12px 26px rgb(49 87 246 / .2); }.hero__primary:hover { box-shadow: 0 16px 34px rgb(49 87 246 / .27); transform: translateY(-2px); }.hero__secondary { border: 1px solid #d5d8df; color: #586273; background: rgb(255 255 255 / .42); }.hero__secondary:hover { background: #fff; transform: translateY(-2px); }.hero__trust { display: flex; gap: 1.1rem; margin-top: 2.2rem; color: #858d9b; font-size: .59rem; }.hero__trust span { display: flex; align-items: center; gap: .35rem; }.hero__trust i { color: #3157f6; font-style: normal; font-weight: 800; }
.signal-strip { position: relative; z-index: 2; overflow: hidden; border-top: 1px solid #d9dce2; border-bottom: 1px solid #d9dce2; background: rgb(255 255 255 / .45); }.signal-strip__track { display: flex; width: max-content; align-items: center; gap: 1.5rem; padding: 1rem 2rem; color: #657083; font-size: .57rem; font-weight: 800; letter-spacing: .14em; animation: signal-drift 22s linear infinite alternate; }.signal-strip__track i { color: #3157f6; font-style: normal; }
.landing-section { position: relative; z-index: 1; width: min(100% - 3rem, 72rem); margin: 0 auto; padding: 8rem 0; scroll-margin-top: 2rem; }.workflow__intro > span, .principles__content > span, .landing-cta > div > span { grid-column: 1 / -1; color: var(--blue); font-size: .62rem; font-weight: 820; letter-spacing: .15em; }.workflow h2, .landing-cta h2 { margin: 0; font-size: clamp(2.8rem, 5vw, 5.1rem); line-height: .95; letter-spacing: -.065em; }
.workflow { display: grid; gap: 7rem; grid-template-columns: .8fr 1.2fr; }.workflow__intro { align-self: start; position: sticky; top: 3rem; }.workflow__intro > span { display: block; margin-bottom: 1.5rem; }.workflow__intro p { max-width: 26rem; margin: 1.5rem 0 0; color: var(--muted); font-size: .8rem; line-height: 1.7; }.workflow__steps { margin: 0; padding: 0; list-style: none; }.workflow__steps li { display: grid; gap: 1.3rem; padding: 2rem 0; border-bottom: 1px solid #d5d9e0; grid-template-columns: 2.2rem 1fr; }.workflow__steps > li > b { color: #3157f6; font-size: .62rem; }.workflow__steps div { display: flex; flex-direction: column; gap: .5rem; }.workflow__steps strong { font-size: 1.08rem; letter-spacing: -.025em; }.workflow__steps span { color: var(--muted); font-size: .7rem; line-height: 1.6; }
.principles { display: grid; overflow: hidden; width: min(100% - 3rem, 72rem); padding: 5rem; border-radius: 1.6rem; color: #fff; background: #3157f6; grid-template-columns: .55fr 1.2fr .7fr; gap: 4rem; }.principles__orb { display: grid; width: 11rem; height: 11rem; place-items: center; border: 1px solid rgb(255 255 255 / .25); border-radius: 50%; background: radial-gradient(circle at 35% 30%, rgb(255 255 255 / .3), transparent 30%), rgb(255 255 255 / .08); box-shadow: inset -20px -20px 45px rgb(28 55 184 / .35), 0 25px 55px rgb(17 37 130 / .3); animation: principle-orb 6s ease-in-out infinite alternate; }.principles__orb span { font-family: Georgia, serif; font-size: 2.4rem; }.principles__content > span { color: #cdd5ff; }.principles blockquote { margin: 1.2rem 0; font-family: Georgia, serif; font-size: clamp(2rem, 3vw, 3.2rem); line-height: 1; letter-spacing: -.04em; }.principles__content p { margin: 0; color: #d7ddff; font-size: .75rem; line-height: 1.65; }.principles__list { display: flex; justify-content: center; flex-direction: column; gap: .9rem; }.principles__list span { padding-bottom: .9rem; border-bottom: 1px solid rgb(255 255 255 / .18); color: #e0e4ff; font-size: .65rem; }
.landing-cta { position: relative; z-index: 1; display: flex; width: min(100% - 3rem, 72rem); margin: 4rem auto 8rem; align-items: end; justify-content: space-between; gap: 3rem; padding-top: 5rem; border-top: 1px solid #d3d7de; }.landing-cta > div > span { display: block; margin-bottom: 1.2rem; }.landing-cta a { display: inline-flex; min-width: 11rem; min-height: 3.6rem; flex: 0 0 auto; align-items: center; justify-content: center; gap: .7rem; border-radius: .75rem; color: #fff; background: #3157f6; box-shadow: 0 14px 30px rgb(49 87 246 / .22); font-size: .72rem; font-weight: 780; text-decoration: none; transition: transform 160ms ease, box-shadow 160ms ease; }.landing-cta a:hover { box-shadow: 0 18px 38px rgb(49 87 246 / .3); transform: translateY(-3px); }
.landing__footer { position: relative; z-index: 1; display: grid; width: min(100% - 3rem, 78rem); margin: 0 auto; align-items: center; padding: 2rem 0; border-top: 1px solid #d5d9df; grid-template-columns: 1fr 2fr 1fr; }.landing__footer p { margin: 0; color: #8b93a0; font-size: .6rem; text-align: center; }.landing__footer > div { display: flex; justify-self: end; gap: 1rem; }.landing__footer a { color: #697386; font-size: .62rem; font-weight: 690; text-decoration: none; }.landing__footer a:hover { color: #3157f6; }
@keyframes hero-in { from { opacity: 0; transform: translateX(-18px); } } @keyframes signal-drift { to { transform: translateX(-6rem); } } @keyframes principle-orb { to { transform: translateY(-.8rem) rotate(5deg); } }
@media (max-width: 1020px) { .landing__nav { display: none; }.landing__header { grid-template-columns: 1fr 1fr; }.hero { grid-template-columns: 1fr; padding-top: 5rem; }.hero__copy { max-width: 45rem; }.principles { padding: 4rem 3rem; grid-template-columns: auto 1fr; }.principles__list { grid-column: 1 / -1; display: grid; grid-template-columns: 1fr 1fr; }.workflow { gap: 4rem; } }
@media (max-width: 720px) { .landing__header { width: min(100% - 2rem, 78rem); }.landing__text-link { display: none; }.landing__pill { padding: .62rem .75rem; }.hero, .landing-section, .landing-cta, .landing__footer { width: min(100% - 2rem, 72rem); }.hero { min-height: auto; grid-template-columns: minmax(0, 1fr); padding: 5rem 0 3rem; }.hero h1 { font-size: clamp(3.3rem, 17vw, 5.2rem); }.hero__trust { flex-wrap: wrap; }.workflow { display: block; }.workflow__intro { position: static; margin-bottom: 3rem; }.principles { display: block; width: min(100% - 2rem, 72rem); padding: 2.2rem; }.principles__orb { width: 7rem; height: 7rem; margin-bottom: 2.5rem; }.principles__list { margin-top: 2.5rem; }.landing-cta { align-items: stretch; flex-direction: column; }.landing-cta a { width: 100%; }.landing__footer { grid-template-columns: 1fr auto; }.landing__footer p { display: none; } }
@media (prefers-reduced-motion: reduce) { .hero__copy, .signal-strip__track, .principles__orb { animation: none; }.hero__primary, .hero__secondary, .landing-cta a { transition: none; } }
</style>
