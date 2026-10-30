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
    <div class="landing-alt">
        <header class="site-header">
            <div class="site-header__inner">
                <router-link to="/" class="site-header__brand" aria-label="GodForge home">
                    <GodForgeMark />
                </router-link>

                <nav class="site-header__nav" aria-label="Primary navigation">
                    <a href="#capabilities">Platform</a>
                    <a href="#workflow">How it works</a>
                    <a href="#principles">Our approach</a>
                </nav>

                <div class="site-header__actions">
                    <router-link v-if="!isAuthenticated" to="/login" class="site-header__signin">
                        Sign in
                    </router-link>
                    <router-link
                        :to="isAuthenticated ? '/dashboard' : '/register'"
                        class="site-header__button"
                    >
                        {{ isAuthenticated ? 'Open dashboard' : 'Get started' }}
                        <span aria-hidden="true">↗</span>
                    </router-link>
                </div>

                <details class="mobile-nav">
                    <summary aria-label="Open navigation menu">
                        <span></span><span></span><span></span>
                    </summary>
                    <nav aria-label="Mobile navigation">
                        <a href="#capabilities">Platform</a>
                        <a href="#workflow">How it works</a>
                        <a href="#principles">Our approach</a>
                        <router-link v-if="!isAuthenticated" to="/login">Sign in</router-link>
                        <router-link :to="isAuthenticated ? '/dashboard' : '/register'">
                            {{ isAuthenticated ? 'Open dashboard' : 'Create an account' }}
                        </router-link>
                    </nav>
                </details>
            </div>
        </header>

        <main>
            <!-- Editorial, centered introduction: intentionally different from the original split hero. -->
            <section class="hero" aria-labelledby="hero-title">
                <div class="hero__eyebrow">
                    <span class="hero__eyebrow-dot" aria-hidden="true"></span>
                    THE GODOT INTELLIGENCE WORKSPACE
                    <span class="hero__eyebrow-line" aria-hidden="true"></span>
                    BUILT FOR TEAMS
                </div>

                <h1 id="hero-title">
                    Build the game.<br />
                    <em>Understand the system.</em>
                </h1>

                <p class="hero__description">
                    A specialized Git and project-intelligence platform for Godot Engine teams.
                    Explore how scenes, scripts, assets and revisions fit together —
                    with traceable findings grounded in your project.
                </p>

                <div class="hero__actions">
                    <router-link
                        :to="isAuthenticated ? '/dashboard' : '/register'"
                        class="button button--primary"
                    >
                        {{ isAuthenticated ? 'Open dashboard' : 'Create your workspace' }}
                        <span aria-hidden="true">↗</span>
                    </router-link>
                    <a href="#preview" class="button button--secondary">
                        Explore the experience <span aria-hidden="true">↓</span>
                    </a>
                </div>

                <ul class="hero__highlights" aria-label="Product principles">
                    <li><span aria-hidden="true">✓</span> Git stays the source of truth</li>
                    <li><span aria-hidden="true">✓</span> Analysis tied to a revision</li>
                    <li><span aria-hidden="true">✓</span> AI remains advisory</li>
                </ul>
            </section>

            <!-- This is an illustrative, non-interactive showcase; it does not imply live user data. -->
            <section id="preview" class="preview" aria-labelledby="preview-heading">
                <div class="preview__header">
                    <div class="preview__header-left">
                        <span class="preview__symbol" aria-hidden="true">◇</span>
                        <div>
                            <span class="preview__eyebrow">INSIDE GODFORGE</span>
                            <h2 id="preview-heading">A clearer view of your project.</h2>
                        </div>
                    </div>
                    <span class="preview__example"><span aria-hidden="true"></span> ILLUSTRATIVE PREVIEW</span>
                </div>

                <div class="preview__body">
                    <div class="preview__visual">
                        <div class="preview__visual-label" aria-hidden="true">PROJECT INTELLIGENCE / SCENE MAP</div>
                        <LandingScene3D />
                    </div>
                    <aside class="preview__rail" aria-label="Example product capabilities">
                        <div class="preview__rail-intro">
                            <span>ONE COMMIT. MORE CONTEXT.</span>
                            <p>Follow a revision from its repository to the parts of the game it affects.</p>
                        </div>

                        <div class="insight">
                            <span class="insight__index">01 / REPOSITORY</span>
                            <div class="insight__title">
                                <span class="insight__icon" aria-hidden="true">⌘</span>
                                <strong>Revision-aware</strong>
                            </div>
                            <p>Keep analysis linked to an immutable commit SHA, not a moving branch.</p>
                        </div>

                        <div class="insight">
                            <span class="insight__index">02 / ENGINE</span>
                            <div class="insight__title">
                                <span class="insight__icon" aria-hidden="true">◎</span>
                                <strong>Evidence, not guesses</strong>
                            </div>
                            <p>See structured findings and the scene or resource behind each issue.</p>
                        </div>

                        <div class="insight">
                            <span class="insight__index">03 / COLLABORATION</span>
                            <div class="insight__title">
                                <span class="insight__icon" aria-hidden="true">▣</span>
                                <strong>Access with intent</strong>
                            </div>
                            <p>Keep project collaboration and protected asset visibility separately controlled.</p>
                        </div>
                    </aside>
                </div>

                <div class="preview__footer">
                    <span><span class="preview__footer-dot" aria-hidden="true"></span> Designed for Godot workflows</span>
                    <span>GIT  /  SCENES  /  DEPENDENCIES  /  HEALTH</span>
                </div>
            </section>

            <section class="statement" aria-labelledby="statement-title">
                <div class="section-label"><span>01</span> / THE BIG PICTURE</div>
                <div class="statement__grid">
                    <h2 id="statement-title">Your repository is more than <em>a folder of files.</em></h2>
                    <div class="statement__copy">
                        <p>
                            Godot projects become difficult to reason about as scenes, scripts and assets grow together.
                            GodForge gives teams a shared view of structure, health and change — without replacing Git.
                        </p>
                        <a href="#capabilities">See the capabilities <span aria-hidden="true">↗</span></a>
                    </div>
                </div>
                <div class="statement__tags" aria-label="Supported analysis perspectives">
                    <span>Scene relationships</span>
                    <span>Revision history</span>
                    <span>Health findings</span>
                    <span>Asset policies</span>
                </div>
            </section>

            <!-- Keeps the existing three bento cards and their public-facing copy. -->
            <LandingCapabilities />

            <section id="workflow" class="workflow" aria-labelledby="workflow-title">
                <div class="workflow__heading">
                    <div class="section-label"><span>02</span> / THE WORKFLOW</div>
                    <h2 id="workflow-title">From a commit<br />to <em>clearer decisions.</em></h2>
                    <p>Four connected steps. One traceable path from source to insight.</p>
                </div>
                <ol class="workflow__steps">
                    <li>
                        <span class="workflow__number">01</span>
                        <div class="workflow__content">
                            <h3>Connect your repository</h3>
                            <p>Use a linked or hosted Git repository and select a commit to analyze.</p>
                        </div>
                        <span class="workflow__arrow" aria-hidden="true">↗</span>
                    </li>
                    <li>
                        <span class="workflow__number">02</span>
                        <div class="workflow__content">
                            <h3>Validate without executing</h3>
                            <p>Check Godot project structure within a bounded, safety-first analysis pipeline.</p>
                        </div>
                        <span class="workflow__arrow" aria-hidden="true">↗</span>
                    </li>
                    <li>
                        <span class="workflow__number">03</span>
                        <div class="workflow__content">
                            <h3>Map scenes and dependencies</h3>
                            <p>Turn parsed project metadata into an understandable view of relationships.</p>
                        </div>
                        <span class="workflow__arrow" aria-hidden="true">↗</span>
                    </li>
                    <li>
                        <span class="workflow__number">04</span>
                        <div class="workflow__content">
                            <h3>Review evidence together</h3>
                            <p>Inspect findings, track revisions and use optional AI suggestions as supporting context.</p>
                        </div>
                        <span class="workflow__arrow" aria-hidden="true">↗</span>
                    </li>
                </ol>
            </section>

            <section id="principles" class="principles" aria-labelledby="principles-title">
                <div class="principles__intro">
                    <span class="principles__eyebrow">THE WAY WE THINK ABOUT INTELLIGENCE</span>
                    <h2 id="principles-title">Clarity, with <em>boundaries.</em></h2>
                    <p>
                        Deterministic analysis is the foundation. Optional generative AI adds advice,
                        never an authoritative result or an automatic code change.
                    </p>
                </div>
                <div class="principles__rules">
                    <div><span>01</span><strong>Deterministic before generative</strong></div>
                    <div><span>02</span><strong>Immutable revision evidence</strong></div>
                    <div><span>03</span><strong>No untrusted execution</strong></div>
                    <div><span>04</span><strong>Least-privilege access</strong></div>
                </div>
                <span class="principles__mark" aria-hidden="true">GF</span>
            </section>

            <section class="final-cta" aria-labelledby="final-cta-title">
                <div>
                    <span class="section-label"><span>03</span> / START WITH CONTEXT</span>
                    <h2 id="final-cta-title">Make room for<br /><em>better decisions.</em></h2>
                    <p>Bring your Godot project, your Git history and your team into one clearer workflow.</p>
                </div>
                <router-link :to="isAuthenticated ? '/dashboard' : '/register'" class="button button--primary">
                    {{ isAuthenticated ? 'Open dashboard' : 'Create your workspace' }}
                    <span aria-hidden="true">↗</span>
                </router-link>
            </section>
        </main>

        <footer class="site-footer">
            <div class="site-footer__top">
                <router-link to="/" aria-label="GodForge home"><GodForgeMark /></router-link>
                <p>A specialized Git and project-intelligence platform for Godot Engine teams.</p>
            </div>
            <div class="site-footer__bottom">
                <span>© GodForge · Built for thoughtful game development.</span>
                <div>
                    <a href="#capabilities">Platform</a>
                    <a href="#workflow">Workflow</a>
                    <router-link v-if="!isAuthenticated" to="/login">Sign in</router-link>
                    <router-link :to="isAuthenticated ? '/dashboard' : '/register'">
                        {{ isAuthenticated ? 'Dashboard' : 'Create account' }}
                    </router-link>
                </div>
            </div>
        </footer>
    </div>
</template>

<style scoped>
.landing-alt {
    --ink: #192236;
    --muted: #687486;
    --blue: #3157f6;
    --line: #e0e3e9;
    --paper: #f5f5f1;
    --surface: #fff;
    min-height: 100vh;
    overflow: clip;
    background: var(--paper);
    color: var(--ink);
    color-scheme: light;
}

.landing-alt a { text-decoration: none; }
.landing-alt a:focus-visible,
.landing-alt summary:focus-visible { outline: 3px solid #7b93ff; outline-offset: 4px; }
.landing-alt em { color: var(--blue); font-family: Georgia, 'Times New Roman', serif; font-style: italic; font-weight: 500; }

/* Header */
.site-header { position: relative; z-index: 20; border-bottom: 1px solid rgb(25 34 54 / .065); background: rgb(245 245 241 / .92); }
.site-header__inner { display: flex; width: min(100% - 3rem, 76rem); min-height: 5.25rem; margin: 0 auto; align-items: center; justify-content: space-between; gap: 2rem; }
.site-header__brand { flex: 0 0 auto; }
.site-header__nav { display: flex; gap: 2.25rem; }
.site-header__nav a, .site-header__signin { color: #586375; font-size: .77rem; font-weight: 660; transition: color 160ms ease; }
.site-header__nav a:hover, .site-header__signin:hover { color: var(--blue); }
.site-header__actions { display: flex; align-items: center; gap: 1.6rem; }
.site-header__button { display: inline-flex; min-height: 2.7rem; align-items: center; gap: .85rem; padding: 0 1rem; border: 1px solid #cad0db; border-radius: .6rem; background: #fff; color: var(--ink); font-size: .75rem; font-weight: 760; transition: border-color 160ms ease, transform 160ms ease; }
.site-header__button:hover { border-color: var(--blue); transform: translateY(-1px); }
.site-header__button span { color: var(--blue); font-size: 1rem; }
.mobile-nav { display: none; }

/* Editorial hero */
.hero { position: relative; width: min(100% - 3rem, 72rem); margin: 0 auto; padding: clamp(5.5rem, 10vw, 9rem) 0 4.2rem; text-align: center; }
.hero::before { position: absolute; z-index: 0; width: 52rem; height: 37rem; left: 50%; top: -12rem; border-radius: 50%; background: radial-gradient(ellipse, rgb(49 87 246 / .07), transparent 65%); content: ''; pointer-events: none; transform: translateX(-50%); }
.hero > * { position: relative; }
.hero__eyebrow { display: flex; justify-content: center; align-items: center; gap: .7rem; color: #5c6a83; font-size: .66rem; font-weight: 800; letter-spacing: .135em; }
.hero__eyebrow-dot { width: .5rem; height: .5rem; border-radius: 50%; background: var(--blue); box-shadow: 0 0 0 .32rem rgb(49 87 246 / .09); }
.hero__eyebrow-line { width: 1.7rem; height: 1px; margin: 0 .15rem; background: #bcc5d4; }
.hero h1 { margin: 2rem auto 0; font-size: clamp(3.9rem, 7.5vw, 7.3rem); font-weight: 790; line-height: .99; letter-spacing: -.078em; text-wrap: balance; }
.hero h1 em { letter-spacing: -.067em; }
.hero__description { max-width: 43rem; margin: 2rem auto 0; color: var(--muted); font-size: clamp(.95rem, 1.3vw, 1.05rem); line-height: 1.75; text-wrap: pretty; }
.hero__actions { display: flex; justify-content: center; flex-wrap: wrap; gap: .85rem; margin-top: 2.1rem; }
.button { display: inline-flex; min-height: 3.35rem; align-items: center; justify-content: center; gap: 1.15rem; padding: 0 1.3rem; border: 1px solid transparent; border-radius: .68rem; font-size: .79rem; font-weight: 780; transition: transform 170ms ease, background 170ms ease, box-shadow 170ms ease; }
.button:hover { transform: translateY(-2px); }
.button--primary { background: var(--blue); color: #fff; box-shadow: 0 12px 25px rgb(49 87 246 / .18); }
.button--primary:hover { background: #2448e5; box-shadow: 0 15px 30px rgb(49 87 246 / .23); }
.button--secondary { border-color: #d3d8e1; background: #fff; color: #334055; }
.button--secondary:hover { background: #f9faff; }
.hero__highlights { display: flex; justify-content: center; flex-wrap: wrap; gap: .7rem 1.8rem; margin: 2.35rem 0 0; padding: 0; list-style: none; color: #758092; font-size: .69rem; font-weight: 590; }
.hero__highlights li { display: inline-flex; align-items: center; gap: .48rem; }
.hero__highlights li span { color: var(--blue); font-size: .85rem; }

/* Large workspace showcase */
.preview { position: relative; width: min(100% - 3rem, 77rem); margin: 0 auto; overflow: hidden; border: 1px solid #e1e5ed; border-radius: 1.1rem; background: #fff; box-shadow: 0 38px 90px rgb(38 54 88 / .075), 0 3px 12px rgb(38 54 88 / .035); scroll-margin-top: 2rem; }
.preview__header { display: flex; min-height: 5.2rem; align-items: center; justify-content: space-between; gap: 1rem; padding: 1rem 2rem; border-bottom: 1px solid #e9ebf0; }
.preview__header-left { display: flex; align-items: center; gap: .9rem; }
.preview__symbol { display: grid; width: 2.55rem; height: 2.55rem; flex: 0 0 auto; place-items: center; border-radius: .6rem; background: #eef0ff; color: var(--blue); font-size: 1.5rem; }
.preview__eyebrow { color: #8791a1; font-size: .59rem; font-weight: 820; letter-spacing: .12em; }
.preview__header h2 { margin: .26rem 0 0; color: #273147; font-size: .91rem; font-weight: 750; letter-spacing: -.025em; }
.preview__example { display: flex; align-items: center; gap: .45rem; padding: .4rem .7rem; border: 1px solid #e0e5ef; border-radius: 999px; background: #fbfbfd; color: #7b8596; font-size: .59rem; font-weight: 800; letter-spacing: .065em; white-space: nowrap; }
.preview__example span { width: .38rem; height: .38rem; border-radius: 50%; background: #8c9df9; }
.preview__body { display: grid; grid-template-columns: minmax(0, 1fr) 18rem; }
.preview__visual { position: relative; display: grid; min-width: 0; align-items: center; background: radial-gradient(circle at 48% 45%, #eef1ff 0, #fafbff 53%, #f5f6fa 95%); }
.preview__visual::after { position: absolute; inset: 0; border-right: 1px solid #e8ebf2; content: ''; pointer-events: none; }
.preview__visual-label { position: absolute; z-index: 1; left: 1.8rem; top: 1.6rem; color: #8c96a9; font-size: .58rem; font-weight: 800; letter-spacing: .115em; }
.preview__visual :deep(.scene-stage) { width: 100%; min-height: 36.5rem; }
.preview__rail { display: flex; flex-direction: column; padding: 2.1rem 1.65rem 1.6rem; background: #fff; }
.preview__rail-intro { padding-bottom: 1.8rem; }
.preview__rail-intro span, .insight__index { color: var(--blue); font-size: .59rem; font-weight: 820; letter-spacing: .09em; }
.preview__rail-intro p { margin: .8rem 0 0; color: #677387; font-size: .9rem; font-weight: 680; line-height: 1.55; letter-spacing: -.018em; }
.insight { padding: 1.45rem 0; border-top: 1px solid #e8ebf0; }
.insight__index { color: #8793a6; }
.insight__title { display: flex; align-items: center; gap: .58rem; margin-top: .85rem; }
.insight__icon { display: grid; width: 1.8rem; height: 1.8rem; flex: 0 0 auto; place-items: center; border-radius: .48rem; background: #eff1ff; color: var(--blue); font-size: .97rem; }
.insight__title strong { font-size: .8rem; font-weight: 780; }
.insight p { margin: .64rem 0 0; color: #7b8595; font-size: .72rem; line-height: 1.65; }
.preview__footer { display: flex; min-height: 3rem; align-items: center; justify-content: space-between; gap: 1rem; padding: .7rem 2rem; border-top: 1px solid #e9ebf0; color: #8993a2; font-size: .61rem; font-weight: 680; letter-spacing: .06em; }
.preview__footer > span:first-child { display: flex; align-items: center; gap: .5rem; letter-spacing: 0; }
.preview__footer-dot { width: .44rem; height: .44rem; border-radius: 50%; background: #39b787; }

/* Short product statement before the existing bento cards */
.statement { width: min(100% - 3rem, 72rem); margin: 0 auto; padding: 9rem 0 1rem; }
.section-label { display: inline-flex; align-items: center; gap: .5rem; color: #6b7890; font-size: .65rem; font-weight: 800; letter-spacing: .13em; }
.section-label > span { color: var(--blue); }
.statement__grid { display: grid; align-items: end; gap: 5rem; margin-top: 1.8rem; grid-template-columns: 1.25fr .75fr; }
.statement h2 { max-width: 41rem; margin: 0; font-size: clamp(3rem, 5vw, 5rem); line-height: 1.03; letter-spacing: -.063em; }
.statement__copy { max-width: 24rem; }
.statement__copy p { margin: 0 0 1.35rem; color: var(--muted); font-size: .87rem; line-height: 1.75; }
.statement__copy a { color: var(--blue); font-size: .76rem; font-weight: 800; }
.statement__copy a span { padding-left: .35rem; }
.statement__tags { display: flex; flex-wrap: wrap; gap: .6rem; margin-top: 3rem; }
.statement__tags span { padding: .55rem .82rem; border: 1px solid #dce1e9; border-radius: 999px; background: rgb(255 255 255 / .72); color: #647083; font-size: .68rem; font-weight: 680; }

/* Traceable workflow */
.workflow { display: grid; width: min(100% - 3rem, 72rem); margin: 0 auto; padding: 9rem 0; gap: 6rem; grid-template-columns: .83fr 1.17fr; scroll-margin-top: 2rem; }
.workflow__heading h2 { margin: 1.6rem 0 0; font-size: clamp(2.85rem, 4.5vw, 4.9rem); line-height: 1.04; letter-spacing: -.065em; }
.workflow__heading p { max-width: 20rem; margin-top: 1.5rem; color: var(--muted); font-size: .84rem; line-height: 1.7; }
.workflow__steps { margin: 0; padding: 0; list-style: none; }
.workflow__steps li { display: grid; align-items: start; gap: 1.25rem; padding: 1.8rem 0; border-top: 1px solid #d9dee6; grid-template-columns: 2rem minmax(0, 1fr) 1.5rem; }
.workflow__steps li:last-child { border-bottom: 1px solid #d9dee6; }
.workflow__number { padding-top: .25rem; color: var(--blue); font-size: .7rem; font-weight: 820; }
.workflow__content h3 { margin: 0; font-size: 1.1rem; font-weight: 765; letter-spacing: -.025em; }
.workflow__content p { max-width: 28rem; margin: .7rem 0 0; color: var(--muted); font-size: .75rem; line-height: 1.7; }
.workflow__arrow { justify-self: end; color: #9aa5b5; }

/* Security and analysis principles */
.principles { position: relative; display: grid; width: min(100% - 3rem, 77rem); min-height: 26rem; margin: 0 auto; overflow: hidden; padding: clamp(2.5rem, 5vw, 5rem); border-radius: 1rem; background: #192642; color: #fff; gap: 5rem; grid-template-columns: 1fr 1fr; scroll-margin-top: 2rem; }
.principles::before { position: absolute; width: 30rem; height: 30rem; left: -13rem; bottom: -21rem; border: 1px solid rgb(145 165 255 / .22); border-radius: 50%; box-shadow: 0 0 0 6rem rgb(145 165 255 / .045), 0 0 0 12rem rgb(145 165 255 / .028); content: ''; pointer-events: none; }
.principles__intro, .principles__rules { position: relative; z-index: 1; }
.principles__eyebrow { color: #a4b6ff; font-size: .64rem; font-weight: 800; letter-spacing: .1em; }
.principles h2 { max-width: 32rem; margin: 1.6rem 0 0; font-size: clamp(2.7rem, 4.8vw, 5rem); line-height: 1.02; letter-spacing: -.066em; }
.principles h2 em { color: #a8b8ff; }
.principles p { max-width: 26rem; margin: 1.6rem 0 0; color: #b6c1d5; font-size: .82rem; line-height: 1.75; }
.principles__rules { align-self: center; }
.principles__rules div { display: grid; gap: 1.35rem; padding: 1.15rem 0; border-bottom: 1px solid rgb(255 255 255 / .14); grid-template-columns: 2rem 1fr; }
.principles__rules div:first-child { border-top: 1px solid rgb(255 255 255 / .14); }
.principles__rules span { color: #a1b1ff; font-size: .68rem; font-weight: 760; }
.principles__rules strong { font-size: .85rem; font-weight: 700; }
.principles__mark { position: absolute; right: -1.5rem; bottom: -5.5rem; color: rgb(255 255 255 / .035); font-family: Georgia, serif; font-size: 20rem; font-weight: 800; letter-spacing: -.1em; pointer-events: none; }

/* Final call to action and footer */
.final-cta { display: flex; width: min(100% - 3rem, 72rem); margin: 0 auto; align-items: end; justify-content: space-between; gap: 2rem; padding: 9rem 0; }
.final-cta h2 { margin: 1.5rem 0 0; font-size: clamp(3.2rem, 5.7vw, 5.8rem); line-height: 1.03; letter-spacing: -.068em; }
.final-cta p { max-width: 28rem; margin: 1.4rem 0 0; color: var(--muted); font-size: .85rem; line-height: 1.75; }
.final-cta .button { flex: 0 0 auto; }
.site-footer { border-top: 1px solid #dfe3e9; background: #f0f1ee; }
.site-footer__top, .site-footer__bottom { display: flex; width: min(100% - 3rem, 76rem); margin: 0 auto; align-items: center; justify-content: space-between; gap: 2rem; }
.site-footer__top { padding: 2.7rem 0; }
.site-footer__top p { max-width: 21rem; margin: 0; color: #7b8594; font-size: .74rem; line-height: 1.55; text-align: right; }
.site-footer__bottom { padding: 1.2rem 0; border-top: 1px solid #dce0e7; color: #929baa; font-size: .69rem; }
.site-footer__bottom > div { display: flex; flex-wrap: wrap; gap: 1.4rem; }
.site-footer__bottom a { color: #677387; font-size: .7rem; font-weight: 660; }
.site-footer__bottom a:hover { color: var(--blue); }

@media (max-width: 1060px) {
    .preview__body { grid-template-columns: minmax(0, 1fr) 15rem; }
    .preview__rail { padding: 1.8rem 1.2rem; }
    .preview__visual :deep(.scene-stage) { min-height: 33rem; }
    .statement__grid { gap: 2rem; }
    .workflow { gap: 3rem; }
}
@media (max-width: 820px) {
    .site-header__nav { display: none; }
    .preview__body { grid-template-columns: minmax(0, 1fr); }
    .preview__visual :deep(.scene-stage) { min-height: 34rem; }
    .preview__visual::after { display: none; }
    .preview__rail { display: grid; gap: 1.2rem; padding: 1.5rem 2rem; border-top: 1px solid #e8ebf2; grid-template-columns: repeat(3, minmax(0, 1fr)); }
    .preview__rail-intro { grid-column: 1 / -1; padding-bottom: .3rem; }
    .preview__rail-intro p { max-width: 28rem; }
    .insight { padding: 1rem 0 0; }
    .statement__grid { grid-template-columns: minmax(0, 1fr); }
    .statement__copy { max-width: 38rem; }
    .workflow { grid-template-columns: minmax(0, 1fr); }
    .workflow__heading p { max-width: 35rem; }
    .principles { gap: 2.5rem; }
}
@media (max-width: 620px) {
    .site-header__inner, .hero, .preview, .statement, .workflow, .principles, .final-cta, .site-footer__top, .site-footer__bottom { width: min(100% - 2rem, 76rem); }
    .site-header__inner { min-height: 4.75rem; gap: .75rem; }
    .site-header__nav, .site-header__actions { display: none; }
    .mobile-nav { position: relative; display: block; margin-left: auto; }
    .mobile-nav summary { display: flex; width: 2.6rem; height: 2.6rem; align-items: center; justify-content: center; gap: .2rem; border: 1px solid #d7dce5; border-radius: .5rem; background: #fff; cursor: pointer; flex-direction: column; list-style: none; }
    .mobile-nav summary::-webkit-details-marker { display: none; }
    .mobile-nav summary span { width: 1.05rem; height: 1.5px; background: #34415b; }
    .mobile-nav nav { position: absolute; z-index: 30; width: min(17rem, calc(100vw - 2rem)); right: 0; top: calc(100% + .7rem); display: flex; padding: .65rem; border: 1px solid #dce1ea; border-radius: .8rem; background: #fff; box-shadow: 0 12px 35px rgb(27 39 73 / .12); flex-direction: column; }
    .mobile-nav nav a { padding: .72rem .8rem; border-radius: .4rem; color: #364159; font-size: .82rem; font-weight: 710; }
    .mobile-nav nav a:hover { background: #eff1ff; color: var(--blue); }
    .hero { padding: 5rem 0 3.2rem; }
    .hero__eyebrow { gap: .45rem; font-size: .53rem; letter-spacing: .08em; }
    .hero__eyebrow-line { width: .8rem; }
    .hero h1 { font-size: clamp(3rem, 11vw, 4.8rem); letter-spacing: -.07em; }
    .hero__description { font-size: .9rem; }
    .hero__actions { align-items: stretch; flex-direction: column; }
    .hero__actions .button { width: 100%; }
    .hero__highlights { justify-content: flex-start; text-align: left; }
    .preview { border-radius: .8rem; }
    .preview__header { min-height: 4.7rem; padding: .85rem 1rem; }
    .preview__header h2 { font-size: .75rem; }
    .preview__example { display: none; }
    .preview__visual-label { left: 1rem; top: 1rem; font-size: .5rem; }
    .preview__visual :deep(.scene-stage) { min-height: 25rem; }
    .preview__rail { padding: 1rem 1.2rem; grid-template-columns: 1fr; }
    .preview__rail-intro { grid-column: auto; }
    .insight { padding: 1rem 0; }
    .preview__footer { display: block; padding: .9rem 1rem; line-height: 1.6; }
    .preview__footer > span:last-child { display: none; }
    .statement { padding-top: 6rem; }
    .statement h2 { font-size: clamp(2.65rem, 10vw, 3.75rem); }
    .workflow { padding: 6rem 0; }
    .principles { padding: 2.5rem 1.5rem; grid-template-columns: minmax(0, 1fr); }
    .principles h2 { font-size: 3rem; }
    .principles__mark { font-size: 12rem; bottom: -3rem; }
    .final-cta { align-items: stretch; flex-direction: column; padding: 6rem 0; }
    .final-cta .button { width: 100%; }
    .site-footer__top { align-items: start; flex-direction: column; }
    .site-footer__top p { text-align: left; }
    .site-footer__bottom { align-items: start; flex-direction: column; }
}
@media (prefers-reduced-motion: reduce) {
    .button, .site-header__button, .site-header__nav a, .site-header__signin { transition: none; }
    .button:hover, .site-header__button:hover { transform: none; }
}
</style>
