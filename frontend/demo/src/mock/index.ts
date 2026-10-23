import { needsSeed, reset } from './core/db';

/**
 * The demo's backend, assembled: every module under ./modules registers its routes from its
 * `handlers.ts` and plants its sample data from an optional `seed.ts`.
 *
 * A seed exports `order` (lower runs first; the foundation is 0, modules that read other
 * modules' rows use 10, 20, …) and `seed()`. Seeds run once per day per browser, and again after
 * "ล้างข้อมูลเดโม", so "today" in the sample data is always today.
 */

interface SeedModule {
  order?: number;
  seed: () => void;
}

import.meta.glob('./modules/*/handlers.ts', { eager: true });
const seeds = import.meta.glob<SeedModule>('./modules/*/seed.ts', { eager: true });

export function plantSeed(): void {
  reset();
  const ordered = Object.entries(seeds).sort(([a, x], [b, y]) => (x.order ?? 50) - (y.order ?? 50) || a.localeCompare(b));
  for (const [file, module] of ordered) {
    try {
      module.seed();
    } catch (error) {
      console.error(`[mock] seed ${file} failed`, error);
    }
  }
}

export function ensureSeeded(): void {
  if (needsSeed()) plantSeed();
}
