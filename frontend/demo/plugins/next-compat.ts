import path from 'node:path';
import type { Plugin } from 'vite';

/**
 * Makes the product's server code run in the browser, by rewriting two things at build time.
 *
 * - **Async components.** Pages, layouts and a few sections are `async function` components, which
 *   only a React server renders. Each is renamed and wrapped by `asyncComponent` (src/runtime),
 *   which runs it once per set of props and reads the answer with `use()`.
 * - **Server actions.** Every export of a `'use server'` file is wrapped by `serverAction`, which
 *   turns `redirect()` into a navigation and `revalidatePath()` into a refresh, as Next does.
 *
 * Nothing else in ../src changes: the files on disk stay exactly what the product ships.
 */
export function nextCompat({ src }: { src: string }): Plugin {
  const root = path.resolve(src);

  return {
    name: 'happypaws-next-compat',
    enforce: 'pre',
    transform(code, id) {
      const file = id.split('?')[0]!;
      if (!/\.(tsx?|jsx?)$/.test(file) || !path.resolve(file).startsWith(root)) return null;

      const rel = path.relative(root, file).replace(/\\/g, '/');
      const isServerActions = /^\s*['"]use server['"]/.test(code);
      const tail: string[] = [];
      let out = code;

      if (isServerActions) {
        out = out.replace(/^export async function (\w+)\s*\(/gm, (_m, name: string) => {
          tail.push(`export const ${name} = __serverAction(__sa_${name}, ${JSON.stringify(`${rel}#${name}`)});`);
          return `async function __sa_${name}(`;
        });
        if (tail.length) out = `import { serverAction as __serverAction } from '@demo/runtime/server-action';\n${out}`;
      }

      if (file.endsWith('.tsx')) {
        const before = tail.length;
        out = out.replace(/^export default async function (\w+)\s*\(/gm, (_m, name: string) => {
          tail.push(`export default __asyncComponent(__ac_${name}, ${JSON.stringify(`${rel}#default`)});`);
          return `async function __ac_${name}(`;
        });
        out = out.replace(/^export async function ([A-Z]\w*)\s*\(/gm, (_m, name: string) => {
          tail.push(`export const ${name} = __asyncComponent(__ac_${name}, ${JSON.stringify(`${rel}#${name}`)});`);
          return `async function __ac_${name}(`;
        });
        if (tail.length > before) out = `import { asyncComponent as __asyncComponent } from '@demo/runtime/async-component';\n${out}`;
      }

      if (!tail.length) return null;
      return { code: `${out}\n${tail.join('\n')}\n`, map: null };
    },
  };
}
