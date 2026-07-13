<script setup lang="ts">
// Minimal setup
</script>

<template>
    <div class="relative w-full h-full bg-transparent overflow-hidden hidden lg:flex items-center justify-center p-12 select-none">
        
        <!-- Radial gradient overlay for depth -->
        <div class="absolute inset-0 bg-gradient-to-tr from-[#09090b]/80 via-transparent to-[#09090b]/80 pointer-events-none z-0"></div>
        
        <!-- Decorative Ambient Glows -->
        <div class="absolute top-[20%] right-[20%] w-[350px] h-[350px] bg-cyan-500/5 rounded-full blur-[120px] pointer-events-none z-0"></div>
        <div class="absolute bottom-[20%] left-[10%] w-[300px] h-[300px] bg-emerald-500/5 rounded-full blur-[100px] pointer-events-none z-0"></div>

        <!-- SaaS Style Editor Mockup Dashboard -->
        <div class="relative w-full max-w-[860px] h-[550px] bg-[#0c0c0e]/80 border border-white/5 backdrop-blur-xl rounded-xl shadow-[0_30px_70px_-15px_rgba(0,0,0,0.9)] flex flex-col overflow-hidden z-10 transition-all duration-500 hover:border-white/10">
            
            <!-- Window Top Bar -->
            <div class="flex items-center justify-between px-5 py-3.5 border-b border-white/5 bg-[#101014]/60">
                <div class="flex items-center space-x-3">
                    <!-- Window Controls (minimal dot circles) -->
                    <div class="flex space-x-1.5">
                        <span class="w-2 h-2 rounded-full bg-zinc-800 border border-zinc-700"></span>
                        <span class="w-2 h-2 rounded-full bg-zinc-800 border border-zinc-700"></span>
                        <span class="w-2 h-2 rounded-full bg-zinc-800 border border-zinc-700"></span>
                    </div>
                    
                    <span class="text-zinc-700 font-mono pl-1">|</span>
                    
                    <!-- App Status / Breadcrumb -->
                    <div class="flex items-center space-x-2 font-sans text-xs pl-1">
                        <i class="bi bi-folder-fill text-zinc-500"></i>
                        <span class="text-zinc-400">lost-isles</span>
                        <span class="text-zinc-600">/</span>
                        <span class="text-zinc-300 font-medium">MainScene.tscn</span>
                    </div>
                </div>

                <div class="flex items-center space-x-4 text-xs font-sans">
                    <span class="flex items-center text-emerald-400 bg-emerald-500/10 px-2.5 py-0.5 rounded-md border border-emerald-500/20 text-[11px] font-medium tracking-wide">
                        <span class="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse mr-2"></span>
                        SYNCED
                    </span>
                    <span class="text-zinc-500 text-[11px]">v4.3.stable</span>
                </div>
            </div>

            <!-- Main Panel Workspace -->
            <div class="flex flex-1 overflow-hidden font-sans">
                
                <!-- Left Sidebar: Scene Tree Explorer (1/4 width) -->
                <div class="w-[220px] border-r border-white/5 bg-[#0c0c0e]/40 p-4 flex flex-col justify-between text-xs">
                    <div>
                        <div class="text-[10px] text-zinc-500 uppercase tracking-widest font-bold mb-4">Scene Tree</div>
                        <div class="space-y-1">
                            <div class="flex items-center space-x-2 py-1.5 px-2 bg-white/5 rounded-md border border-white/5 text-zinc-100 font-medium">
                                <i class="bi bi-box text-cyan-400"></i>
                                <span class="truncate">MainScene</span>
                            </div>
                            <div class="pl-3.5 space-y-1">
                                <div class="flex items-center space-x-2 py-1.5 px-2 hover:bg-white/5 rounded-md cursor-pointer transition-colors text-zinc-300">
                                    <i class="bi bi-camera-video text-zinc-500"></i>
                                    <span class="truncate">Camera3D</span>
                                </div>
                                <div class="flex items-center space-x-2 py-1.5 px-2 bg-cyan-500/10 hover:bg-cyan-500/15 rounded-md cursor-pointer transition-colors text-cyan-300 border border-cyan-500/20">
                                    <i class="bi bi-person text-cyan-400"></i>
                                    <span class="truncate">Player</span>
                                </div>
                                <div class="pl-4 space-y-1">
                                    <div class="flex items-center space-x-2 py-1 px-2 text-zinc-400 hover:text-zinc-300 rounded cursor-pointer transition-colors">
                                        <i class="bi bi-file-earmark-code text-zinc-500"></i>
                                        <span class="truncate text-[11px]">Player.gd</span>
                                    </div>
                                    <div class="flex items-center space-x-2 py-1 px-2 text-zinc-400 hover:text-zinc-300 rounded cursor-pointer transition-colors">
                                        <i class="bi bi-shield-exclamation text-zinc-500"></i>
                                        <span class="truncate">Hitbox</span>
                                    </div>
                                </div>
                                <div class="flex items-center space-x-2 py-1.5 px-2 hover:bg-white/5 rounded-md cursor-pointer transition-colors text-zinc-300">
                                    <i class="bi bi-lightbulb text-zinc-500"></i>
                                    <span class="truncate">SunLight</span>
                                </div>
                                <div class="flex items-center space-x-2 py-1.5 px-2 hover:bg-white/5 rounded-md cursor-pointer transition-colors text-zinc-300">
                                    <i class="bi bi-grid-3x3-gap text-zinc-500"></i>
                                    <span class="truncate">TerrainMesh</span>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Git Status -->
                    <div class="border-t border-white/5 pt-4 text-[11px]">
                        <div class="flex items-center justify-between mb-1.5">
                            <span class="text-zinc-500">Branch</span>
                            <span class="text-zinc-300 font-medium">main</span>
                        </div>
                        <div class="flex items-center space-x-2 text-zinc-400">
                            <i class="bi bi-git text-zinc-500"></i>
                            <span class="truncate">feat: impl state machine</span>
                        </div>
                    </div>
                </div>

                <!-- Center Area: Dependency Graph Visualizer -->
                <div class="flex-1 bg-[#09090b]/30 p-5 flex flex-col relative overflow-hidden">
                    <div class="flex justify-between items-center mb-4">
                        <div class="text-[10px] text-zinc-500 uppercase tracking-widest font-bold">Dependency Graph</div>
                        <div class="flex bg-white/5 p-0.5 rounded-md border border-white/5">
                            <button class="px-2.5 py-0.5 bg-zinc-800 text-zinc-200 rounded-md text-[10px] font-medium shadow-sm">Graph</button>
                            <button class="px-2.5 py-0.5 text-zinc-500 hover:text-zinc-300 rounded text-[10px] font-medium">Table</button>
                        </div>
                    </div>

                    <!-- Visual Graph Area with Nodes -->
                    <div class="flex-1 border border-white/5 bg-[#0c0c0e]/50 rounded-lg relative overflow-hidden flex items-center justify-center">
                        <!-- Background Grid Lines -->
                        <div class="absolute inset-0 bg-[linear-gradient(to_right,rgba(255,255,255,0.01)_1px,transparent_1px),linear-gradient(to_bottom,rgba(255,255,255,0.01)_1px,transparent_1px)] bg-[size:16px_16px] pointer-events-none"></div>

                        <!-- Graph connections (SVG) using percentage viewBox -->
                        <svg class="absolute inset-0 w-full h-full stroke-white/10" viewBox="0 0 100 100" preserveAspectRatio="none" fill="none">
                            <path d="M50 20 L50 50" stroke-width="1" vector-effect="non-scaling-stroke" stroke-dasharray="3 3" />
                            <path d="M50 50 L20 80" stroke-width="1" vector-effect="non-scaling-stroke" />
                            <path d="M50 50 L50 80" stroke-width="1" vector-effect="non-scaling-stroke" />
                            <path d="M50 50 L80 80" stroke-width="1" vector-effect="non-scaling-stroke" />
                            
                            <!-- Soft flow pulse -->
                            <circle r="0.8" fill="#06b6d4" opacity="0.6">
                                <animateMotion dur="4s" repeatCount="indefinite" path="M50 20 L50 50" />
                            </circle>
                        </svg>

                        <!-- Selected Node Info Line Bubble -->
                        <div class="absolute top-[20%] left-1/2 -translate-x-1/2 -translate-y-1/2 bg-[#121214] border border-white/10 px-3 py-1.5 rounded-md shadow-md text-xs text-zinc-100 flex items-center space-x-2 z-10">
                            <i class="bi bi-box text-cyan-400"></i>
                            <span class="font-medium">Player.tscn</span>
                        </div>

                        <!-- Script Component Node -->
                        <div class="absolute top-[50%] left-1/2 -translate-x-1/2 -translate-y-1/2 bg-[#121214] border border-cyan-500/20 px-3.5 py-1.5 rounded-md shadow-md text-xs text-zinc-100 flex items-center space-x-2 z-10">
                            <i class="bi bi-file-earmark-code text-emerald-400"></i>
                            <span class="font-medium text-cyan-300">Player.gd</span>
                            <span class="text-zinc-500 text-[10px]">GDScript</span>
                        </div>

                        <!-- Dependency Leaf 1 -->
                        <div class="absolute top-[80%] left-[20%] -translate-x-1/2 -translate-y-1/2 bg-[#121214]/80 backdrop-blur-sm border border-white/5 px-2.5 py-1.5 rounded-md shadow-sm text-[11px] text-zinc-300 flex items-center space-x-2 z-10 w-[110px] justify-center">
                            <i class="bi bi-controller text-purple-400 flex-shrink-0"></i>
                            <span class="truncate">InputMap.gd</span>
                        </div>

                        <!-- Dependency Leaf 2 -->
                        <div class="absolute top-[80%] left-[50%] -translate-x-1/2 -translate-y-1/2 bg-[#121214]/80 backdrop-blur-sm border border-white/5 px-2.5 py-1.5 rounded-md shadow-sm text-[11px] text-zinc-300 flex items-center space-x-2 z-10 w-[110px] justify-center">
                            <i class="bi bi-speaker text-amber-400 flex-shrink-0"></i>
                            <span class="truncate">laser_sfx.wav</span>
                        </div>

                        <!-- Dependency Leaf 3 -->
                        <div class="absolute top-[80%] left-[80%] -translate-x-1/2 -translate-y-1/2 bg-[#121214]/80 backdrop-blur-sm border border-white/5 px-2.5 py-1.5 rounded-md shadow-sm text-[11px] text-zinc-300 flex items-center space-x-2 z-10 w-[110px] justify-center">
                            <i class="bi bi-image text-blue-400 flex-shrink-0"></i>
                            <span class="truncate">sprite.png</span>
                        </div>
                    </div>
                </div>

                <!-- Right Sidebar: Inspector Panel (1/4 width) -->
                <div class="w-[220px] border-l border-white/5 bg-[#0c0c0e]/40 p-4 flex flex-col justify-between text-xs">
                    <div>
                        <div class="text-[10px] text-zinc-500 uppercase tracking-widest font-bold mb-4">Node Inspector</div>
                        
                        <!-- Properties -->
                        <div class="space-y-4">
                            <div class="bg-white/5 border border-white/5 p-2.5 rounded-md">
                                <span class="text-zinc-500 text-[10px] block mb-0.5">RESOURCE</span>
                                <span class="text-zinc-200 font-semibold block truncate">Player.gd</span>
                            </div>

                            <div class="space-y-2 text-[11px]">
                                <div class="flex justify-between border-b border-white/5 py-1.5">
                                    <span class="text-zinc-500">Type</span>
                                    <span class="text-zinc-300">GDScript</span>
                                </div>
                                <div class="flex justify-between border-b border-white/5 py-1.5">
                                    <span class="text-zinc-500">Size</span>
                                    <span class="text-zinc-300">12.4 KB</span>
                                </div>
                                <div class="flex justify-between border-b border-white/5 py-1.5">
                                    <span class="text-zinc-500">Lines</span>
                                    <span class="text-zinc-300">342 lines</span>
                                </div>
                                <div class="flex justify-between py-1.5">
                                    <span class="text-zinc-500">Modified</span>
                                    <span class="text-zinc-300">2 min ago</span>
                                </div>
                            </div>
                        </div>

                        <!-- Sparkline Chart -->
                        <div class="mt-6 border-t border-white/5 pt-4">
                            <span class="text-[10px] text-zinc-500 uppercase tracking-widest font-bold block mb-3">Build Health</span>
                            <div class="h-14 bg-white/5 border border-white/5 rounded-md p-1.5 flex items-end">
                                <svg class="w-full h-full stroke-cyan-500/80" viewBox="0 0 100 30" fill="none">
                                    <path d="M0 25 L10 20 L20 28 L30 15 L40 22 L50 8 L60 14 L70 5 L80 12 L90 2 L100 2" stroke-width="1.2" />
                                    <path d="M0 25 L10 20 L20 28 L30 15 L40 22 L50 8 L60 14 L70 5 L80 12 L90 2 L100 2 L100 30 L0 30 Z" fill="url(#sparkline-grad)" stroke="none" />
                                    <defs>
                                        <linearGradient id="sparkline-grad" x1="0" y1="0" x2="0" y2="1">
                                            <stop offset="0%" stop-color="rgb(6,182,212)" stop-opacity="0.08" />
                                            <stop offset="100%" stop-color="rgb(6,182,212)" stop-opacity="0" />
                                        </linearGradient>
                                    </defs>
                                </svg>
                            </div>
                        </div>
                    </div>

                    <!-- Footer Action Status -->
                    <div class="border-t border-white/5 pt-4">
                        <div class="flex items-center justify-between text-zinc-500 text-[11px]">
                            <span>Parse Time</span>
                            <span class="text-zinc-300 font-medium">12ms</span>
                        </div>
                    </div>
                </div>

            </div>

            <!-- Mock UI Status Bar -->
            <div class="border-t border-white/5 px-5 py-2.5 bg-[#0c0c0e] flex justify-between items-center text-[10px] text-zinc-500 font-sans">
                <div class="flex space-x-5">
                    <span class="flex items-center"><span class="w-1.5 h-1.5 bg-emerald-500 rounded-full mr-2"></span>Parser: Active</span>
                    <span class="flex items-center"><span class="w-1.5 h-1.5 bg-emerald-500 rounded-full mr-2"></span>Watcher: Idle</span>
                </div>
                <div>
                    <span>Workspace Observer v1.0.0</span>
                </div>
            </div>
            
        </div>
        
    </div>
</template>

<style scoped>
/* Clean & Minimal Styles */
</style>
