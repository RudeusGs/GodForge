import pluginVue from 'eslint-plugin-vue'
import ts from 'typescript-eslint'
import vueParser from 'vue-eslint-parser'

export default [
  { ignores: ["dist/**", "node_modules/**"] },
  ...pluginVue.configs['flat/essential'],
  ...ts.configs.recommended,
  {
    files: ['*.vue', '**/*.vue'],
    languageOptions: {
      parser: vueParser,
      parserOptions: {
        parser: ts.parser
      }
    }
  }
]
