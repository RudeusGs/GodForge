# Frontend Architecture

## Framework
- **Vue 3** with Composition API and `<script setup>`.
- **TypeScript** strictly enforced.

## State Management
- **Pinia** for global state. Do not mutate state directly outside of actions.
- Pinia stores must be modular (e.g., `useAuthStore`, `useProjectStore`).

## Routing
- **Vue Router**. Routes must be declared centrally.
- Use Route Meta fields for RBAC and authentication guards (`meta: { requiresAuth: true }`).

## API Client
- Centralized **Axios** instance.
- Must include request interceptors to attach the JWT token.
- Must include response interceptors to handle `401 Unauthorized` (triggering token refresh or redirect to login).

## Components
- Components should be "dumb" (presentation) or "smart" (container/data-fetching).
- Prefer composables (`useFetchProjects()`) over mixing heavy logic directly in `.vue` files.

## UI/UX Rules
- No raw alert boxes.
- Implement proper Skeleton Loaders for async views.
- Implement explicit Empty States.

## CSS & Styling
- **TailwindCSS** is the mandatory CSS framework for all styling.
- **Bootstrap Icons** (`bootstrap-icons`) is the standard for iconography. Do not use system icons or SVG strings directly unless absolutely necessary.
- Follow a sleek, modern, and minimalist design language without excessive colors.
