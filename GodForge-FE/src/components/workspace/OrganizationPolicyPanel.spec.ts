import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, expect, it, vi } from 'vitest';
import { settingsApi } from '@/api/settings.api';
import { AxiosError } from 'axios';
import Panel from './OrganizationPolicyPanel.vue';
vi.mock('@/api/settings.api');
const policy = { organizationId: 'org', aiAdvisoryAllowed: true, platformAiAdvisoryAllowed: false, version: 4, createdAt: '', updatedAt: '' };
beforeEach(() => { vi.resetAllMocks(); vi.mocked(settingsApi.getPolicy).mockResolvedValue({ data: policy }); });
function failure(status?: number, code?: string, message = 'Policy value rejected.') {
    return new AxiosError('request failed', status ? undefined : 'ECONNABORTED', undefined, undefined,
        status ? { status, data: { error: { code, message } }, config: {} as never, headers: {}, statusText: '' } : undefined);
}
async function edited() {
    const wrapper = mount(Panel, { props: { organizationId: 'org', mutable: true } });
    await flushPromises(); await wrapper.get('input').setValue(false); return wrapper;
}
it('reloads a conflict while preserving edits for deliberate resubmission with the latest version', async () => {
    const wrapper = await edited();
    vi.mocked(settingsApi.updatePolicy).mockRejectedValueOnce(failure(409, 'CONCURRENCY_CONFLICT'));
    vi.mocked(settingsApi.getPolicy).mockResolvedValue({ data: { ...policy, version: 6 } });
    await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(wrapper.text()).toContain('changed by another administrator');
    expect(wrapper.get('input').element.checked).toBe(false);
    expect(wrapper.text()).toContain('Policy version 6');
    expect(settingsApi.updatePolicy).toHaveBeenCalledTimes(1);
    vi.mocked(settingsApi.updatePolicy).mockResolvedValue({ data: { ...policy, aiAdvisoryAllowed: false, version: 7 } });
    await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(settingsApi.updatePolicy).toHaveBeenLastCalledWith('org', { aiAdvisoryAllowed: false, version: 6 }); wrapper.unmount();
});
it('reconciles a timeout before completion and permits only a user initiated versioned retry', async () => {
    const wrapper = await edited();
    vi.mocked(settingsApi.updatePolicy).mockRejectedValueOnce(failure());
    await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(wrapper.text()).toContain('outcome is unknown');
    expect(settingsApi.getPolicy).toHaveBeenCalledTimes(2);
    expect(settingsApi.updatePolicy).toHaveBeenCalledTimes(1);
    vi.mocked(settingsApi.updatePolicy).mockResolvedValue({ data: { ...policy, aiAdvisoryAllowed: false, version: 5 } });
    await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(wrapper.text()).toContain('Organization policy saved.'); wrapper.unmount();
});
it('recognizes authoritative matching values after the server commits but the response is lost', async () => {
    const wrapper = await edited();
    vi.mocked(settingsApi.updatePolicy).mockRejectedValueOnce(failure());
    vi.mocked(settingsApi.getPolicy).mockResolvedValue({ data: { ...policy, aiAdvisoryAllowed: false, version: 5 } });
    await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(wrapper.text()).toContain('current policy matches');
    await wrapper.get('form').trigger('submit');
    expect(settingsApi.updatePolicy).toHaveBeenCalledTimes(1); wrapper.unmount();
});
it('blocks uncertain writes until a successful reconciliation, retaining edits across a failed read', async () => {
    const wrapper = await edited();
    vi.mocked(settingsApi.updatePolicy).mockRejectedValueOnce(failure(503));
    vi.mocked(settingsApi.getPolicy).mockRejectedValueOnce(failure());
    await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(wrapper.text()).toContain('outcome is unknown');
    await wrapper.get('form').trigger('submit'); expect(settingsApi.updatePolicy).toHaveBeenCalledTimes(1);
    await wrapper.findAll('button').find(b => b.text() === 'Load current capability policy')!.trigger('click'); await flushPromises();
    expect(wrapper.get('input').element.checked).toBe(false);
    vi.mocked(settingsApi.updatePolicy).mockResolvedValue({ data: { ...policy, aiAdvisoryAllowed: false, version: 5 } });
    await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(wrapper.text()).toContain('Organization policy saved.'); wrapper.unmount();
});
it('shows forbidden separately and requires an authorized reload before another attempt', async () => {
    const wrapper = await edited(); vi.mocked(settingsApi.updatePolicy).mockRejectedValueOnce(failure(403, 'SECURITY_FORBIDDEN'));
    await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(wrapper.text()).toContain('permission'); expect(wrapper.text()).not.toContain('changed by another administrator');
    await wrapper.get('form').trigger('submit'); expect(settingsApi.updatePolicy).toHaveBeenCalledTimes(1); wrapper.unmount();
});
it.each([[400, 'VALIDATION_ERROR'], [409, 'SETTINGS_POLICY_RESTRICTED']])('preserves edits and the safe rejection message for %s %s', async (status, code) => {
    const wrapper = await edited(); vi.mocked(settingsApi.updatePolicy).mockRejectedValueOnce(failure(status, code));
    await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(wrapper.text()).toContain('Policy value rejected.'); expect(wrapper.get('input').element.checked).toBe(false);
    expect(settingsApi.getPolicy).toHaveBeenCalledTimes(1); wrapper.unmount();
});
it('handles a rejected request with an empty error body without throwing or losing edits', async () => {
    const wrapper = await edited();
    vi.mocked(settingsApi.updatePolicy).mockRejectedValueOnce(new AxiosError('bad request', undefined, undefined, undefined,
        { status: 400, data: null, config: {} as never, headers: {}, statusText: '' }));
    await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(wrapper.get('[role="alert"]').text()).toContain('Check');
    expect(wrapper.get('input').element.checked).toBe(false);
    expect(wrapper.get('fieldset').attributes('disabled')).toBeUndefined(); wrapper.unmount();
});
it('prevents duplicates and ignores save completion after rapid tenant switches', async () => {
    const wrapper = await edited(); let resolve!: (value: { data: typeof policy }) => void;
    vi.mocked(settingsApi.updatePolicy).mockReturnValue(new Promise(r => { resolve = r; }));
    await wrapper.get('form').trigger('submit'); await wrapper.get('form').trigger('submit');
    expect(settingsApi.updatePolicy).toHaveBeenCalledTimes(1);
    vi.mocked(settingsApi.getPolicy).mockResolvedValue({ data: { ...policy, organizationId: 'b', version: 12 } });
    await wrapper.setProps({ organizationId: 'b' }); await flushPromises();
    resolve({ data: { ...policy, aiAdvisoryAllowed: false, version: 5 } }); await flushPromises();
    expect(wrapper.text()).toContain('Policy version 12'); expect(wrapper.get('input').element.checked).toBe(true); wrapper.unmount();
});
it('does not reconcile or publish a save failure after unmount', async () => {
    const wrapper = await edited(); let reject!: (cause: unknown) => void;
    vi.mocked(settingsApi.updatePolicy).mockReturnValue(new Promise((_r, r) => { reject = r; }));
    await wrapper.get('form').trigger('submit'); wrapper.unmount(); reject(failure()); await flushPromises();
    expect(settingsApi.getPolicy).toHaveBeenCalledTimes(1);
});
it('ignores an authoritative reconciliation response after a tenant switch', async () => {
    const wrapper = await edited(); let resolve!: (value: { data: typeof policy }) => void;
    vi.mocked(settingsApi.updatePolicy).mockRejectedValueOnce(failure());
    vi.mocked(settingsApi.getPolicy).mockReturnValueOnce(new Promise(r => { resolve = r; }));
    await wrapper.get('form').trigger('submit'); await flushPromises();
    vi.mocked(settingsApi.getPolicy).mockResolvedValue({ data: { ...policy, organizationId: 'b', version: 12 } });
    await wrapper.setProps({ organizationId: 'b' }); await flushPromises();
    resolve({ data: { ...policy, aiAdvisoryAllowed: false, version: 5 } }); await flushPromises();
    expect(wrapper.text()).toContain('Policy version 12'); expect(wrapper.text()).not.toContain('outcome is unknown');
    expect(wrapper.get('input').element.checked).toBe(true); wrapper.unmount();
});
it('discards a successful save completion after unmount', async () => {
    const wrapper = await edited(); let resolve!: (value: { data: typeof policy }) => void;
    vi.mocked(settingsApi.updatePolicy).mockReturnValue(new Promise(r => { resolve = r; }));
    await wrapper.get('form').trigger('submit'); wrapper.unmount();
    resolve({ data: { ...policy, aiAdvisoryAllowed: false, version: 5 } }); await flushPromises();
    expect(wrapper.text()).not.toContain('Organization policy saved.'); expect(settingsApi.getPolicy).toHaveBeenCalledTimes(1);
});
it('explains platform restriction and saves an independent policy version', async () => {
    vi.mocked(settingsApi.updatePolicy).mockResolvedValue({ data: { ...policy, aiAdvisoryAllowed: false, version: 5 } });
    const wrapper = mount(Panel, { props: { organizationId: 'org', mutable: true } }); await flushPromises();
    expect(wrapper.text()).toContain('Platform restriction always wins');
    await wrapper.get('input').setValue(false); await wrapper.get('form').trigger('submit'); await flushPromises();
    expect(settingsApi.updatePolicy).toHaveBeenCalledWith('org', { aiAdvisoryAllowed: false, version: 4 });
    expect(wrapper.text()).toContain('Policy version 5'); wrapper.unmount();
});
it('ignores a response from an unmounted tenant panel', async () => {
    let resolve!: (value: { data: typeof policy }) => void;
    vi.mocked(settingsApi.getPolicy).mockReturnValue(new Promise(r => { resolve = r; }));
    const wrapper = mount(Panel, { props: { organizationId: 'org', mutable: true } }); wrapper.unmount();
    resolve({ data: policy }); await flushPromises(); expect(settingsApi.updatePolicy).not.toHaveBeenCalled();
});
