import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/auth.store';

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/',
            redirect: '/dashboard' // Default redirect
        },
        {
            path: '/dashboard',
            name: 'dashboard',
            component: () => import('../views/DashboardView.vue'),
            meta: { requiresAuth: true }
        },
        {
            path: '/login',
            name: 'login',
            component: () => import('../views/LoginView.vue'),
            meta: { requiresGuest: true }
        },
        {
            path: '/register',
            name: 'register',
            component: () => import('../views/RegisterView.vue'),
            meta: { requiresGuest: true }
        },
        {
            path: '/forgot-password',
            name: 'forgotPassword',
            component: () => import('../views/ForgotPasswordView.vue'),
            meta: { requiresGuest: true }
        },
        {
            path: '/reset-password',
            name: 'resetPassword',
            component: () => import('../views/ResetPasswordView.vue'),
            meta: { requiresGuest: true }
        }
    ]
});

// Navigation Guards
router.beforeEach((to) => {
    const authStore = useAuthStore();
    const isAuthenticated = authStore.isAuthenticated;

    if (to.meta.requiresAuth && !isAuthenticated) {
        return { name: 'login' };
    } else if (to.meta.requiresGuest && isAuthenticated) {
        // Redirect away from login/register if already logged in
        return { name: 'dashboard' };
    }
});

export default router;

