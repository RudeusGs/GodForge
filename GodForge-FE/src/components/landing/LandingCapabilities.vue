<template>
    <section id="capabilities" class="capabilities">
        <header class="capabilities__heading">
            <span>WHAT GODFORGE REVEALS</span>
            <h2>From project folder<br><em>to engineering system</em></h2>
            <p>One visual language for structure, health and access — with every result tied back to a revision and evidence.</p>
        </header>

        <div class="bento">
            <article class="bento-card bento-card--graph">
                <header class="bento-card__header">
                    <div><span>01</span><small>PROJECT STRUCTURE</small></div>
                    <b>Live dependency map</b>
                </header>
                <div class="graph-copy">
                    <h3>See how everything connects.</h3>
                    <p>Move from a scene to every script, resource and asset it depends on.</p>
                </div>
                <div class="graph-visual" aria-hidden="true">
                    <div class="graph-visual__grid"></div>
                    <svg viewBox="0 0 620 300" fill="none">
                        <path d="M310 52 170 140M310 52l140 88M170 140 80 244m90-104 80 104m200-104-80 104m80-104 90 104" stroke="url(#cap-line)" stroke-width="1.4" />
                        <path d="M170 140h280" stroke="#3157f6" stroke-opacity=".18" stroke-dasharray="5 7" />
                        <defs><linearGradient id="cap-line" x1="310" y1="40" x2="310" y2="260"><stop stop-color="#3157f6"/><stop offset="1" stop-color="#9eaeff" stop-opacity=".45"/></linearGradient></defs>
                    </svg>
                    <span class="graph-node graph-node--root"><i></i>World.tscn<small>root scene</small></span>
                    <span class="graph-node graph-node--left">Player.gd<small>script</small></span>
                    <span class="graph-node graph-node--right">GameState<small>autoload</small></span>
                    <span class="graph-leaf graph-leaf--one">InputMap</span><span class="graph-leaf graph-leaf--two">player.glb</span><span class="graph-leaf graph-leaf--three">SaveData</span><span class="graph-leaf graph-leaf--four">HUD.tscn</span>
                    <div class="graph-pulse graph-pulse--one"></div><div class="graph-pulse graph-pulse--two"></div>
                </div>
                <footer class="bento-card__footer"><span><i></i> 12 nodes mapped</span><span>Depth 3</span><span>Revision 8e4a2d1</span></footer>
            </article>

            <article class="bento-card bento-card--health">
                <header class="bento-card__header">
                    <div><span>02</span><small>PROJECT HEALTH</small></div>
                    <b>Evidence bound</b>
                </header>
                <div class="health-copy"><h3>Know what needs attention.</h3><p>Reproducible rules turn project state into clear, prioritized action.</p></div>
                <div class="health-ring" aria-hidden="true">
                    <svg viewBox="0 0 180 180">
                        <circle cx="90" cy="90" r="70" pathLength="100" />
                        <circle class="health-ring__value" cx="90" cy="90" r="70" pathLength="100" />
                    </svg>
                    <div><strong>92</strong><span>EXCELLENT</span></div>
                </div>
                <dl class="health-stats">
                    <div><dt><i class="health-dot health-dot--safe"></i>Critical</dt><dd>0</dd></div>
                    <div><dt><i class="health-dot health-dot--warn"></i>Warnings</dt><dd>3</dd></div>
                    <div><dt><i class="health-dot health-dot--info"></i>Evidence</dt><dd>18</dd></div>
                </dl>
            </article>

            <article class="bento-card bento-card--access">
                <div class="access-copy">
                    <header class="bento-card__header">
                        <div><span>03</span><small>CONTROLLED COLLABORATION</small></div>
                    </header>
                    <h3>Share the project.<br>Protect what matters.</h3>
                    <p>Keep source collaboration and commercial asset visibility under independent, project-scoped policies.</p>
                    <span class="access-copy__pill"><i></i> Least-privilege access</span>
                </div>
                <div class="access-stack" aria-hidden="true">
                    <div class="access-layer access-layer--source"><span>01</span><b>Git source</b><small>Team visible</small></div>
                    <div class="access-layer access-layer--analysis"><span>02</span><b>Project intelligence</b><small>Role scoped</small></div>
                    <div class="access-layer access-layer--asset"><span>03</span><b>Protected assets</b><small>Policy controlled</small><i>LOCKED</i></div>
                </div>
                <div class="access-policy">
                    <span>POLICY CHECK</span>
                    <div><b>Project member</b><i>Allowed</i></div>
                    <div><b>Organization admin</b><i class="denied">No implicit access</i></div>
                    <div><b>Asset owner</b><i>Allowed</i></div>
                </div>
            </article>
        </div>
    </section>
</template>

<style scoped>
.capabilities { position: relative; z-index: 1; width: min(100% - 3rem, 72rem); margin: 0 auto; padding: 8rem 0; scroll-margin-top: 2rem; }.capabilities__heading { display: grid; align-items: end; gap: 2rem; grid-template-columns: 2fr 1fr; }.capabilities__heading > span { grid-column: 1 / -1; color: #3157f6; font-size: .62rem; font-weight: 820; letter-spacing: .15em; }.capabilities__heading h2 { margin: 0; color: #111827; font-size: clamp(2.8rem, 5vw, 5.1rem); line-height: .95; letter-spacing: -.065em; }.capabilities__heading h2 em { color: #3157f6; font-family: Georgia, 'Times New Roman', serif; font-weight: 500; }.capabilities__heading p { margin: 0; color: #6f7888; font-size: .8rem; line-height: 1.7; }
.bento { display: grid; gap: 1rem; margin-top: 4rem; grid-template-columns: minmax(0, 1.45fr) minmax(18rem, .75fr); }.bento-card { position: relative; overflow: hidden; border: 1px solid #dfe2e8; border-radius: 1.55rem; color: #111827; background: rgb(255 255 255 / .72); box-shadow: 0 18px 48px rgb(31 45 92 / .06), inset 0 1px rgb(255 255 255 / .85); backdrop-filter: blur(14px); transition: border-color 220ms ease, box-shadow 260ms ease, transform 260ms cubic-bezier(.22,.75,.24,1); }.bento-card:hover { border-color: #cbd1df; box-shadow: 0 28px 68px rgb(31 45 92 / .12); transform: translateY(-5px); }.bento-card--graph { min-height: 35rem; padding: 1.35rem; }.bento-card--health { min-height: 35rem; padding: 1.35rem; background: linear-gradient(160deg, rgb(255 255 255 / .9), rgb(239 242 255 / .85)); }.bento-card--access { display: grid; min-height: 23rem; padding: 1.5rem; grid-column: 1 / -1; grid-template-columns: .8fr 1.15fr .75fr; gap: 2rem; background: linear-gradient(120deg, rgb(255 255 255 / .85), rgb(244 245 251 / .9)); }
.bento-card__header { position: relative; z-index: 3; display: flex; align-items: center; justify-content: space-between; }.bento-card__header > div { display: flex; align-items: center; gap: .55rem; }.bento-card__header span { display: grid; width: 1.65rem; height: 1.65rem; place-items: center; border-radius: .5rem; color: #3157f6; background: #edf0ff; font-size: .54rem; font-weight: 820; }.bento-card__header small { color: #7e8796; font-size: .48rem; font-weight: 800; letter-spacing: .11em; }.bento-card__header > b { padding: .38rem .55rem; border: 1px solid #e0e3ea; border-radius: 999px; color: #8b93a0; background: rgb(255 255 255 / .65); font-size: .48rem; font-weight: 720; }
.graph-copy { position: relative; z-index: 3; max-width: 20rem; margin-top: 1.8rem; }.graph-copy h3, .health-copy h3, .access-copy h3 { margin: 0; font-size: 1.35rem; line-height: 1.08; letter-spacing: -.045em; }.graph-copy p, .health-copy p, .access-copy p { margin: .65rem 0 0; color: #778191; font-size: .67rem; line-height: 1.65; }.graph-visual { position: absolute; inset: 9.5rem 1.35rem 3.5rem; overflow: hidden; border: 1px solid #e8eaf0; border-radius: 1rem; background: rgb(248 249 252 / .8); perspective: 800px; }.graph-visual__grid { position: absolute; inset: 0; opacity: .55; background-image: radial-gradient(#c9cfdd .65px, transparent .65px); background-size: 1rem 1rem; mask-image: radial-gradient(circle, black, transparent 82%); }.graph-visual svg { position: absolute; inset: 4% 3%; width: 94%; height: 92%; }.graph-node { position: absolute; z-index: 2; display: flex; min-width: 6rem; padding: .55rem .65rem; border: 1px solid #d9deea; border-radius: .62rem; flex-direction: column; color: #3c4659; background: rgb(255 255 255 / .94); box-shadow: 0 12px 24px rgb(44 61 122 / .1); font-size: .55rem; font-weight: 770; }.graph-node small { margin-top: .18rem; color: #9aa2af; font-size: .42rem; font-weight: 600; }.graph-node--root { left: 50%; top: 7%; color: #fff; border-color: #3157f6; background: #3157f6; transform: translateX(-50%) translateZ(2rem); box-shadow: 0 14px 30px rgb(49 87 246 / .25); }.graph-node--root i { position: absolute; width: .4rem; height: .4rem; right: .5rem; top: .5rem; border-radius: 50%; background: #bfc9ff; }.graph-node--left { left: 18%; top: 39%; }.graph-node--right { right: 18%; top: 39%; }.graph-leaf { position: absolute; z-index: 2; bottom: 8%; padding: .42rem .5rem; border: 1px solid #e0e4ec; border-radius: .48rem; color: #687386; background: rgb(255 255 255 / .88); box-shadow: 0 8px 18px rgb(44 61 122 / .07); font-size: .45rem; font-weight: 700; }.graph-leaf--one { left: 4%; }.graph-leaf--two { left: 30%; }.graph-leaf--three { right: 30%; }.graph-leaf--four { right: 4%; }.graph-pulse { position: absolute; z-index: 4; width: .42rem; height: .42rem; border-radius: 50%; background: #3157f6; box-shadow: 0 0 0 .3rem rgb(49 87 246 / .1); animation: graph-pulse 2.8s ease-in-out infinite; }.graph-pulse--one { left: 35%; top: 30%; }.graph-pulse--two { right: 34%; top: 55%; animation-delay: -1.4s; }.bento-card__footer { position: absolute; display: flex; left: 1.35rem; right: 1.35rem; bottom: 1.15rem; justify-content: space-between; color: #939ba8; font-size: .46rem; }.bento-card__footer span:first-child { display: flex; align-items: center; gap: .3rem; }.bento-card__footer i { width: .32rem; height: .32rem; border-radius: 50%; background: #35b987; }
.health-copy { margin-top: 1.8rem; }.health-ring { position: relative; display: grid; width: 13rem; height: 13rem; margin: 1.6rem auto 1.2rem; place-items: center; }.health-ring svg { position: absolute; inset: 0; width: 100%; height: 100%; overflow: visible; transform: rotate(-90deg); }.health-ring circle { fill: none; stroke: #e4e7ee; stroke-width: 8; }.health-ring .health-ring__value { stroke: #3157f6; stroke-linecap: round; stroke-dasharray: 92 100; filter: drop-shadow(0 5px 8px rgb(49 87 246 / .18)); }.health-ring > div { display: flex; align-items: center; flex-direction: column; }.health-ring strong { color: #1b2440; font-size: 3.4rem; line-height: 1; letter-spacing: -.09em; }.health-ring span { margin-top: .4rem; color: #3157f6; font-size: .45rem; font-weight: 820; letter-spacing: .13em; }.health-stats { margin: 0; }.health-stats > div { display: flex; align-items: center; justify-content: space-between; padding: .62rem 0; border-top: 1px solid #e3e6ed; font-size: .55rem; }.health-stats dt { display: flex; align-items: center; gap: .42rem; color: #758092; }.health-stats dd { margin: 0; color: #303a4f; font-weight: 780; }.health-dot { width: .4rem; height: .4rem; border-radius: 50%; }.health-dot--safe { background: #35b987; }.health-dot--warn { background: #e2a13c; }.health-dot--info { background: #7c91f5; }
.access-copy { display: flex; justify-content: center; flex-direction: column; }.access-copy .bento-card__header { margin-bottom: 1.6rem; }.access-copy__pill { display: flex; width: fit-content; align-items: center; gap: .4rem; margin-top: 1.25rem; padding: .48rem .62rem; border: 1px solid #dfe3eb; border-radius: 999px; color: #657084; background: #fff; font-size: .48rem; font-weight: 720; }.access-copy__pill i { width: .36rem; height: .36rem; border-radius: 50%; background: #35b987; }.access-stack { position: relative; min-height: 18rem; perspective: 900px; transform-style: preserve-3d; }.access-layer { position: absolute; display: grid; width: 80%; height: 6.2rem; left: 10%; align-items: center; padding: 0 1.1rem; border: 1px solid #dce1eb; border-radius: 1rem; background: rgb(255 255 255 / .9); box-shadow: 0 18px 36px rgb(36 52 105 / .1); grid-template-columns: 2rem 1fr auto; transform: rotateX(58deg) rotateZ(-3deg); transition: transform 260ms ease; }.access-stack:hover .access-layer { transform: rotateX(50deg) rotateZ(-1deg) translateY(-.35rem); }.access-layer > span { color: #a1a8b4; font-size: .5rem; font-weight: 800; }.access-layer b { font-size: .61rem; }.access-layer small { color: #8b94a3; font-size: .45rem; }.access-layer--source { top: 1rem; }.access-layer--analysis { top: 5rem; z-index: 2; }.access-layer--asset { top: 9rem; z-index: 3; color: #fff; border-color: #3157f6; background: #3157f6; box-shadow: 0 22px 42px rgb(49 87 246 / .22); }.access-layer--asset > span, .access-layer--asset small { color: #cdd5ff; }.access-layer--asset i { position: absolute; right: 1rem; top: .65rem; padding: .24rem .35rem; border-radius: 999px; color: #dce2ff; background: rgb(255 255 255 / .13); font-size: .38rem; font-style: normal; font-weight: 820; letter-spacing: .1em; }.access-policy { display: flex; justify-content: center; flex-direction: column; }.access-policy > span { margin-bottom: .75rem; color: #969daa; font-size: .44rem; font-weight: 820; letter-spacing: .12em; }.access-policy > div { display: flex; align-items: center; justify-content: space-between; padding: .7rem 0; border-bottom: 1px solid #e1e4ea; gap: .75rem; }.access-policy b { color: #606b7e; font-size: .5rem; }.access-policy i { flex: 0 0 auto; color: #24835f; font-size: .44rem; font-style: normal; font-weight: 760; }.access-policy i.denied { color: #9b6b43; }
@keyframes graph-pulse { 50% { opacity: .35; transform: scale(.7); } }
@media (max-width: 940px) { .bento { grid-template-columns: 1fr 1fr; }.bento-card--graph { grid-column: 1 / -1; }.bento-card--access { grid-template-columns: 1fr 1.2fr; }.access-policy { grid-column: 1 / -1; display: grid; align-items: center; gap: 1rem; grid-template-columns: auto repeat(3, 1fr); }.access-policy > span { margin: 0; }.access-policy > div { border: 0; flex-direction: column; align-items: flex-start; } }
@media (max-width: 700px) { .capabilities { width: min(100% - 2rem, 72rem); padding: 6rem 0; }.capabilities__heading { grid-template-columns: 1fr; }.capabilities__heading > span { grid-column: auto; }.bento { grid-template-columns: 1fr; }.bento-card--graph { min-height: 32rem; grid-column: auto; }.bento-card--health { min-height: 34rem; }.bento-card--access { display: block; grid-column: auto; }.access-stack { margin: 1.5rem 0; }.access-policy { display: flex; }.access-policy > div { flex-direction: row; align-items: center; border-bottom: 1px solid #e1e4ea; }.graph-node--left { left: 8%; }.graph-node--right { right: 8%; }.graph-leaf--two, .graph-leaf--three { display: none; } }
@media (prefers-reduced-motion: reduce) { .bento-card, .access-layer { transition: none; }.graph-pulse { animation: none; } }
</style>
