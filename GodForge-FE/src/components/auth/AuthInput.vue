<script setup lang="ts">
import { computed, ref, useId } from 'vue';

const props = withDefaults(defineProps<{
    modelValue: string;
    label: string;
    id?: string;
    name?: string;
    type?: string;
    placeholder?: string;
    icon?: string;
    error?: string;
    hint?: string;
    autocomplete?: string;
    inputmode?: 'none' | 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url';
    maxlength?: number | string;
    required?: boolean;
    disabled?: boolean;
}>(), {
    type: 'text',
    placeholder: '',
    icon: '',
    error: '',
    hint: '',
    autocomplete: 'off',
    name: '',
    id: '',
    inputmode: 'text',
    maxlength: undefined,
    required: false,
    disabled: false,
});

defineEmits<{
    (e: 'update:modelValue', value: string): void;
}>();

const generatedId = useId();
const inputId = computed(() => props.id || `auth-${generatedId}`);
const passwordVisible = ref(false);
const actualType = computed(() => props.type === 'password' && passwordVisible.value ? 'text' : props.type);
const describedBy = computed(() => props.error ? `${inputId.value}-error` : props.hint ? `${inputId.value}-hint` : undefined);
</script>

<template>
    <div class="auth-field">
        <div class="auth-field__label-row">
            <label :for="inputId">{{ label }}</label>
            <span v-if="!required">Optional</span>
        </div>
        <div class="auth-field__control" :class="{ 'auth-field__control--error': error }">
            <input
                :id="inputId"
                :name="name || inputId"
                :type="actualType"
                :value="modelValue"
                :placeholder="placeholder"
                :autocomplete="autocomplete"
                :inputmode="inputmode"
                :maxlength="maxlength"
                :required="required"
                :disabled="disabled"
                :aria-invalid="Boolean(error)"
                :aria-describedby="describedBy"
                @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
            />
            <button
                v-if="type === 'password'"
                type="button"
                class="auth-field__reveal"
                :aria-label="passwordVisible ? 'Hide password' : 'Show password'"
                :aria-pressed="passwordVisible"
                @click="passwordVisible = !passwordVisible"
            >
                {{ passwordVisible ? 'Hide' : 'Show' }}
            </button>
            <slot name="action"></slot>
        </div>
        <p v-if="error" :id="`${inputId}-error`" class="auth-field__message auth-field__message--error">{{ error }}</p>
        <p v-else-if="hint" :id="`${inputId}-hint`" class="auth-field__message">{{ hint }}</p>
    </div>
</template>

<style scoped>
.auth-field { display: flex; flex-direction: column; gap: .48rem; }
.auth-field__label-row { display: flex; align-items: center; justify-content: space-between; }
.auth-field__label-row label { color: #283143; font-size: .73rem; font-weight: 720; }
.auth-field__label-row span { color: #9aa1ad; font-size: .62rem; }
.auth-field__control { position: relative; display: flex; align-items: center; min-height: 3.05rem; overflow: hidden; border: 1px solid #d9dce3; border-radius: .65rem; background: #fafafa; transition: border-color 140ms ease, background 140ms ease, box-shadow 140ms ease; }
.auth-field__control:hover { border-color: #bfc4ce; background: #fff; }
.auth-field__control:focus-within { border-color: #3157f6; background: #fff; box-shadow: 0 0 0 3px rgb(49 87 246 / .1); }
.auth-field__control--error { border-color: #dc5b5b; }.auth-field__control--error:focus-within { border-color: #dc5b5b; box-shadow: 0 0 0 3px rgb(220 91 91 / .09); }
.auth-field input { width: 100%; min-width: 0; padding: .82rem 3.3rem .82rem .9rem; border: 0; outline: 0; color: #151b28; background: transparent; font: inherit; font-size: .8rem; }.auth-field input::placeholder { color: #a3a8b2; }.auth-field input:disabled { cursor: not-allowed; opacity: .55; }
.auth-field__reveal { position: absolute; right: .45rem; min-width: 2.8rem; height: 2rem; cursor: pointer; border: 0; border-radius: .4rem; color: #697386; background: transparent; font-size: .64rem; font-weight: 720; }.auth-field__reveal:hover { color: #3157f6; background: #eef1ff; }.auth-field__reveal:focus-visible { outline: 2px solid #3157f6; outline-offset: 1px; }
.auth-field__message { margin: -.05rem 0 0; color: #818896; font-size: .64rem; line-height: 1.45; }.auth-field__message--error { color: #c84444; }
</style>
