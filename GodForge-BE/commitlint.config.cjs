module.exports = {
  extends: ['@commitlint/config-conventional'],

  rules: {
    'type-enum': [
      2,
      'always',
      [
        'feat',
        'fix',
        'docs',
        'style',
        'refactor',
        'perf',
        'test',
        'build',
        'ci',
        'chore',
        'revert'
      ]
    ],

    'scope-enum': [
      2,
      'always',
      [
        'auth',
        'user',
        'project',
        'repository',
        'git',
        'scene',
        'asset',
        'dependency',
        'health',
        'diff',
        'dashboard',
        'notification',
        'activity',
        'job',
        'worker',
        'queue',
        'cache',
        'storage',
        'database',
        'api',
        'config',
        'security',
        'infra',
        'test'
      ]
    ],

    'subject-case': [
      2,
      'never',
      [
        'sentence-case',
        'start-case',
        'pascal-case',
        'upper-case'
      ]
    ],

    'subject-empty': [2, 'never'],
    'subject-full-stop': [2, 'never', '.'],
    'header-max-length': [2, 'always', 100]
  }
};
