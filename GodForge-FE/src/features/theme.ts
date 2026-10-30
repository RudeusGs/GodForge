import type { UserPreferencesInput } from '@/api/settings.api';

// Applied to shared application tokens; no personal values are kept in local storage.
export let themeRevision = 0;
export function applyTheme(theme: UserPreferencesInput['theme']) {
    themeRevision++;
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;
}
