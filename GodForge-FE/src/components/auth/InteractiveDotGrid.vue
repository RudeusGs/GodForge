<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';

const canvasRef = ref<HTMLCanvasElement | null>(null);

let animationFrameId: number;
let mouse = { x: -1000, y: -1000 };
let width = 0;
let height = 0;

const DOT_RADIUS = 1;
const DOT_SPACING = 24;

let needsRedraw = true;

const handleMouseMove = (e: MouseEvent) => {
    mouse.x = e.clientX;
    mouse.y = e.clientY;
    needsRedraw = true;
};

const handleMouseLeave = () => {
    mouse.x = -1000;
    mouse.y = -1000;
    needsRedraw = true;
};

const resize = () => {
    if (!canvasRef.value) return;
    width = window.innerWidth;
    height = window.innerHeight;
    canvasRef.value.width = width;
    canvasRef.value.height = height;
    needsRedraw = true;
};

const draw = () => {
    animationFrameId = requestAnimationFrame(draw);
    if (!needsRedraw || !canvasRef.value) return;
    needsRedraw = false;
    
    const ctx = canvasRef.value.getContext('2d');
    if (!ctx) return;

    ctx.clearRect(0, 0, width, height);
    
    // Draw background (near-black, refined palette)
    ctx.fillStyle = '#09090b';
    ctx.fillRect(0, 0, width, height);

    // Subtle spotlight gradient overlay following mouse
    if (mouse.x > -1000) {
        const radGrad = ctx.createRadialGradient(mouse.x, mouse.y, 0, mouse.x, mouse.y, 380);
        radGrad.addColorStop(0, 'rgba(6, 182, 212, 0.04)'); // extremely subtle soft cyan/blue spotlight
        radGrad.addColorStop(1, 'rgba(0, 0, 0, 0)');
        ctx.fillStyle = radGrad;
        ctx.fillRect(0, 0, width, height);
    }

    // Draw Grid Dots (clean, VS Code/SaaS styled dots)
    for (let x = 0; x < width; x += DOT_SPACING) {
        for (let y = 0; y < height; y += DOT_SPACING) {
            let dx = x - mouse.x;
            let dy = y - mouse.y;
            let distance = Math.sqrt(dx * dx + dy * dy);
            
            let alpha = 0.06; // static faint grid opacity
            
            if (distance < 240) {
                // soft lighting glow based on distance
                const factor = (240 - distance) / 240;
                alpha += factor * 0.12;
            }

            ctx.fillStyle = `rgba(255, 255, 255, ${alpha})`;
            ctx.beginPath();
            ctx.arc(x, y, DOT_RADIUS, 0, Math.PI * 2);
            ctx.fill();
        }
    }
};

onMounted(() => {
    resize();
    window.addEventListener('resize', resize);
    window.addEventListener('mousemove', handleMouseMove);
    document.body.addEventListener('mouseleave', handleMouseLeave);
    draw();
});

onUnmounted(() => {
    window.removeEventListener('resize', resize);
    window.removeEventListener('mousemove', handleMouseMove);
    document.body.removeEventListener('mouseleave', handleMouseLeave);
    cancelAnimationFrame(animationFrameId);
});
</script>

<template>
    <canvas ref="canvasRef" class="fixed inset-0 w-full h-full pointer-events-none z-0"></canvas>
</template>
