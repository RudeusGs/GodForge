import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import AuthInput from './AuthInput.vue';

describe('AuthInput', () => {
    it('connects the label and validation message to the input', () => {
        const wrapper = mount(AuthInput, {
            props: {
                id: 'account-email',
                modelValue: '',
                label: 'Email address',
                type: 'email',
                error: 'Enter a valid email address.',
                required: true,
            },
        });

        expect(wrapper.get('label').attributes('for')).toBe('account-email');
        expect(wrapper.get('input').attributes('aria-invalid')).toBe('true');
        expect(wrapper.get('input').attributes('aria-describedby')).toBe('account-email-error');
        expect(wrapper.find('button').exists()).toBe(false);
        expect(wrapper.text()).toContain('Enter a valid email address.');
    });

    it('emits changes and toggles password visibility accessibly', async () => {
        const wrapper = mount(AuthInput, {
            props: {
                id: 'account-password',
                modelValue: '',
                label: 'Password',
                type: 'password',
                required: true,
            },
        });

        await wrapper.get('input').setValue('ForgePass1');
        expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['ForgePass1']);

        const toggle = wrapper.get('button[aria-label="Show password"]');
        await toggle.trigger('click');
        expect(wrapper.get('input').attributes('type')).toBe('text');
        expect(toggle.attributes('aria-pressed')).toBe('true');
        expect(toggle.attributes('aria-label')).toBe('Hide password');
    });
});
