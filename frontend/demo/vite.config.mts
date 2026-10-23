import { fileURLToPath } from 'node:url';
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { readFileSync } from 'node:fs';
import { defineConfig, type Plugin } from 'vite';
import { MOCK_API } from './mock-api';
import { nextCompat } from './plugins/next-compat';

/**
 * The browser-only demo of the whole product: the real pages from ../src, with Next's server
 * features swapped for browser ones (./src/shims) and the API answered inside the browser
 * (./src/mock). `npm run build:demo` writes a static site to ./dist for any static host.
 */

const demo = fileURLToPath(new URL('.', import.meta.url));
const src = fileURLToPath(new URL('../src', import.meta.url));
const shim = (file: string) => fileURLToPath(new URL(`./src/shims/${file}`, import.meta.url));

/**
 * Product modules the demo swaps for its own version, by the file they resolve to. Each override
 * re-exports the original (imported with `?original`) and replaces only what the demo needs.
 */
const overrides: Record<string, string> = {
  'features/home/content/hero.ts': fileURLToPath(new URL('./src/overrides/home-hero.ts', import.meta.url)),
  'features/home/components/hero/hero-section.tsx': fileURLToPath(new URL('./src/overrides/hero-section.tsx', import.meta.url)),
  'features/home/content/care.ts': fileURLToPath(new URL('./src/overrides/home-care.ts', import.meta.url)),
  'features/home/content/services.ts': fileURLToPath(new URL('./src/overrides/home-services.ts', import.meta.url)),
  'features/site-banners/content/page-banners.ts': fileURLToPath(new URL('./src/overrides/team-banner.ts', import.meta.url)),
  'features/home/components/line/line-section.tsx': fileURLToPath(new URL('./src/overrides/line-section.tsx', import.meta.url)),
};

function demoOverrides(): Plugin {
  return {
    name: 'happypaws-demo-overrides',
    enforce: 'pre',
    async resolveId(source, importer, options) {
      if (source.endsWith('?original')) {
        const resolved = await this.resolve(source.slice(0, -'?original'.length), importer, { ...options, skipSelf: true });
        return resolved ? `${resolved.id}?original` : null;
      }
      const resolved = await this.resolve(source, importer, { ...options, skipSelf: true });
      if (!resolved) return null;
      const file = resolved.id.replace(/\\/g, '/');
      const match = Object.keys(overrides).find((key) => file.endsWith(`/src/${key}`));
      return match ? overrides[match] : null;
    },
    load(id) {
      // The original module, read from disk as is.
      if (id.endsWith('?original')) return readFileSync(id.slice(0, -'?original'.length), 'utf8');
      return null;
    },
  };
}

export default defineConfig({
  root: demo,
  publicDir: fileURLToPath(new URL('../public', import.meta.url)),
  plugins: [demoOverrides(), nextCompat({ src }), react(), tailwindcss()],
  resolve: {
    alias: [
      { find: /^@\//, replacement: `${src.replace(/\\/g, '/')}/` },
      { find: /^@demo\//, replacement: `${fileURLToPath(new URL('./src/', import.meta.url)).replace(/\\/g, '/')}` },
      { find: /^next\/link$/, replacement: shim('next-link.tsx') },
      { find: /^next\/navigation$/, replacement: shim('next-navigation.ts') },
      { find: /^next\/headers$/, replacement: shim('next-headers.ts') },
      { find: /^next\/cache$/, replacement: shim('next-cache.ts') },
      { find: /^next\/image$/, replacement: shim('next-image.tsx') },
      { find: /^next\/font\/google$/, replacement: shim('next-font.ts') },
      { find: /^next\/server$/, replacement: shim('next-server.ts') },
      { find: /^next\/og$/, replacement: shim('next-server.ts') },
      { find: /^next$/, replacement: shim('next-server.ts') },
    ],
  },
  define: {
    'process.env.API_BASE_URL': JSON.stringify(MOCK_API),
    'process.env.NEXT_PUBLIC_API_BASE_URL': JSON.stringify(MOCK_API),
    // Canonical links and structured data point at wherever the demo is served from.
    'process.env.NEXT_PUBLIC_SITE_URL': 'globalThis.location.origin',
  },
  server: { port: 5199, strictPort: true, fs: { allow: [fileURLToPath(new URL('..', import.meta.url))] } },
  preview: { port: 5199, strictPort: true },
  build: { outDir: fileURLToPath(new URL('./dist', import.meta.url)), emptyOutDir: true, chunkSizeWarningLimit: 4000 },
});
