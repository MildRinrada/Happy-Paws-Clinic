import { dirname } from 'path';
import { fileURLToPath } from 'url';
import { FlatCompat } from '@eslint/eslintrc';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const compat = new FlatCompat({ baseDirectory: __dirname });

const eslintConfig = [
  // Generated output is not source. Without this, `eslint` walks into .next and reports thousands
  // of problems in code nobody wrote, which buries the ones that matter.
  { ignores: ['.next/**', 'node_modules/**', 'next-env.d.ts'] },
  ...compat.extends('next/core-web-vitals', 'next/typescript'),
  {
    rules: {
      // Enforces the "no scattered fetch()" rule from the project brief: every call to the
      // backend goes through a feature's api/ module, which builds on src/lib/api.
      'no-restricted-globals': [
        'error',
        { name: 'fetch', message: 'Call the backend through a feature api/ module (see src/lib/api).' },
      ],
    },
  },
  {
    files: ['src/lib/api/**'],
    rules: { 'no-restricted-globals': 'off' },
  },
];

export default eslintConfig;
