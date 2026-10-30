import { flushPromises, mount } from '@vue/test-utils';
import { describe, expect, it, vi } from 'vitest';
import CreateWorkspaceForm from './CreateWorkspaceForm.vue';

async function fill(wrapper: ReturnType<typeof mount>, name = 'Game') {
    await wrapper.find('input').setValue(name);
    await wrapper.findAll('input')[1]!.setValue('game');
}
describe('workspace creation forms', () => {
    it('validates names/slugs without sending an invalid request', async () => {
        const submit = vi.fn();
        const wrapper = mount(CreateWorkspaceForm, { props: { kind: 'organization', submit } });
        await wrapper.find('form').trigger('submit');
        expect(submit).not.toHaveBeenCalled();
        expect(wrapper.get('[role="alert"]').text()).toContain('Enter a name');
        expect(wrapper.find('textarea').exists()).toBe(false);
    });
    it('sends real project options, prevents duplicate submits and announces success', async () => {
        let resolve!: () => void;
        const submit = vi.fn(() => new Promise<void>(done => { resolve = done; }));
        const wrapper = mount(CreateWorkspaceForm, { props: { kind: 'project', submit } });
        await fill(wrapper);
        await wrapper.find('textarea').setValue('Godot game');
        await wrapper.find('select').setValue('internal');
        await wrapper.find('form').trigger('submit');
        await wrapper.find('form').trigger('submit');
        expect(submit).toHaveBeenCalledTimes(1);
        expect(submit.mock.calls[0]).toEqual([{ name: 'Game', slug: 'game', description: 'Godot game', visibility: 'internal' }, expect.any(String)]);
        expect(wrapper.get('fieldset').attributes('disabled')).toBeDefined();
        resolve(); await flushPromises();
        expect(wrapper.emitted('created')).toHaveLength(1);
    });
    it('keeps the idempotency key for uncertain retries and changes it for a changed payload', async () => {
        const submit = vi.fn().mockRejectedValue(new Error('offline'));
        const wrapper = mount(CreateWorkspaceForm, { props: { kind: 'project', submit } });
        await fill(wrapper);
        await wrapper.find('form').trigger('submit'); await flushPromises();
        expect(wrapper.get('[role="alert"]').text()).toContain('retry');
        await wrapper.find('form').trigger('submit'); await flushPromises();
        expect(submit.mock.calls[1]![1]).toBe(submit.mock.calls[0]![1]);
        await wrapper.find('input').setValue('Changed');
        await wrapper.find('form').trigger('submit'); await flushPromises();
        expect(submit.mock.calls[2]![1]).not.toBe(submit.mock.calls[0]![1]);
    });
});
