import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError } from 'axios';
import { settingsApi, type UserPreferences } from '@/api/settings.api';
import Preferences from './AccountPreferencesView.vue';

vi.mock('@/api/settings.api');
const defaults: UserPreferences = { theme: 'light', notificationInApp: true, notificationEmail: true, notificationDigest: 'off', version: 1, createdAt: '2026-10-30T00:00:00Z', updatedAt: '2026-10-30T00:00:00Z' };
function render() { return mount(Preferences, { global: { stubs: { RouterLink: { template: '<a><slot /></a>' } } } }); }
const failure = () => new AxiosError('conflict', undefined, undefined, undefined, { status: 409, data: { error: { code: 'CONCURRENCY_CONFLICT' } }, config: {} as never, headers: {}, statusText: '' });
describe('Account preferences', () => {
    beforeEach(() => { vi.resetAllMocks(); vi.mocked(settingsApi.getPreferences).mockResolvedValue({ data: { ...defaults } }); });
    it('renders persisted accessible controls, marks edits and reverts without a write', async () => {
        const wrapper = render(); await flushPromises();
        expect(wrapper.findAll('label')).toHaveLength(4);
        expect(wrapper.get('select').element.value).toBe('light');
        expect(wrapper.get('button').attributes('disabled')).toBeDefined();
        await wrapper.get('select').setValue('dark');
        expect(wrapper.text()).toContain('Unsaved changes');
        const revert = wrapper.findAll('button').find(b => b.text() === 'Revert unsaved edits')!;
        await revert.trigger('click');
        expect(wrapper.get('select').element.value).toBe('light');
        expect(settingsApi.updatePreferences).not.toHaveBeenCalled(); wrapper.unmount();
    });
    it('saves only contract fields, applies persisted theme, increments displayed version and reloads', async () => {
        vi.mocked(settingsApi.updatePreferences).mockResolvedValue({ data: { ...defaults, theme: 'dark', version: 2 } });
        const wrapper = render(); await flushPromises(); await wrapper.get('select').setValue('dark');
        await wrapper.get('form').trigger('submit'); await flushPromises();
        expect(settingsApi.updatePreferences).toHaveBeenCalledWith({ theme: 'dark', notificationInApp: true, notificationEmail: true, notificationDigest: 'off', version: 1 });
        expect(wrapper.text()).toContain('Preferences saved.'); expect(wrapper.text()).toContain('Version 2');
        expect(document.documentElement.dataset.theme).toBe('dark');
        vi.mocked(settingsApi.getPreferences).mockResolvedValue({ data: { ...defaults, theme: 'dark', version: 2 } });
        wrapper.unmount(); const reloaded = render(); await flushPromises();
        expect(reloaded.get('select').element.value).toBe('dark'); reloaded.unmount();
    });
    it('retains edits on conflicts, blocks stale retry and requires explicit current-data reload', async () => {
        vi.mocked(settingsApi.updatePreferences).mockRejectedValue(failure());
        const wrapper = render(); await flushPromises(); await wrapper.get('select').setValue('dark');
        await wrapper.get('form').trigger('submit'); await flushPromises();
        expect(wrapper.get('[role="alert"]').text()).toContain('another session');
        expect(wrapper.get('select').element.value).toBe('dark');
        await wrapper.get('form').trigger('submit'); expect(settingsApi.updatePreferences).toHaveBeenCalledTimes(1);
        await wrapper.findAll('button').find(b => b.text() === 'Load current preferences')!.trigger('click'); await flushPromises();
        expect(wrapper.get('select').element.value).toBe('light'); wrapper.unmount();
    });
    it('prevents duplicate submit and discards responses after unmount', async () => {
        let resolve!: (value: { data: UserPreferences }) => void;
        vi.mocked(settingsApi.updatePreferences).mockReturnValue(new Promise(r => { resolve = r; }));
        const wrapper = render(); await flushPromises(); await wrapper.get('select').setValue('dark');
        await wrapper.get('form').trigger('submit'); await wrapper.get('form').trigger('submit');
        expect(settingsApi.updatePreferences).toHaveBeenCalledTimes(1);
        wrapper.unmount(); resolve({ data: { ...defaults, theme: 'dark', version: 2 } }); await flushPromises();
        expect(document.documentElement.dataset.theme).toBe('light');
    });
    it('shows recoverable loading errors', async () => {
        vi.mocked(settingsApi.getPreferences).mockRejectedValueOnce(new Error('offline'));
        const wrapper = render(); expect(wrapper.text()).toContain('Loading'); await flushPromises();
        expect(wrapper.find('[role="alert"]').exists()).toBe(true);
        await wrapper.findAll('button').find(b => b.text() === 'Retry')!.trigger('click'); await flushPromises();
        expect(wrapper.get('select').element.value).toBe('light'); wrapper.unmount();
    });
});
