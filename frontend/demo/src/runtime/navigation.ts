/**
 * The demo's router state, outside React: where the visitor is, and the "generation" every
 * async component keys its data on. Each navigation and each refresh starts a new generation, so
 * pages read fresh data the way a dynamic Next page does on every request.
 */

export interface Place {
  pathname: string;
  search: string;
  hash: string;
}

type Listener = (change: { place: Place; generation: number; scroll: boolean }) => void;

const listeners = new Set<Listener>();
let generation = 1;
let place: Place = read();

function read(): Place {
  return { pathname: location.pathname || '/', search: location.search, hash: location.hash };
}

export function currentPlace(): Place {
  return place;
}

export function currentGeneration(): number {
  return generation;
}

export function subscribe(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

function emit(scroll: boolean) {
  generation += 1;
  // For tracking down what keeps refreshing a page: set `window.__demoTrace = []` in the console.
  (globalThis as { __demoTrace?: string[] }).__demoTrace?.push(new Error().stack ?? '');
  for (const listener of listeners) listener({ place, generation, scroll });
}

/** Resolves an href against the current page into a same-origin path, or null for another site. */
export function toPath(href: string): string | null {
  const url = new URL(href, location.href);
  if (url.origin !== location.origin) return null;
  return url.pathname + url.search + url.hash;
}

export function navigate(href: string, options: { replace?: boolean; scroll?: boolean } = {}): void {
  const target = toPath(href);
  if (target === null) {
    location.assign(href);
    return;
  }
  const url = new URL(target, location.href);
  const samePage = url.pathname === location.pathname && url.search === location.search;
  if (options.replace) history.replaceState(null, '', target);
  else history.pushState(null, '', target);
  place = read();
  if (samePage && url.hash) {
    document.getElementById(decodeURIComponent(url.hash.slice(1)))?.scrollIntoView({ behavior: 'smooth' });
    return;
  }
  emit(options.scroll ?? true);
}

/** `router.refresh()` and `revalidatePath()`: same page, fresh data, scroll kept. */
export function refresh(): void {
  emit(false);
}

window.addEventListener('popstate', () => {
  place = read();
  emit(false);
});

let refreshQueued = false;

/** Marks the data on screen stale; the refresh runs once the current task is over. */
export function queueRefresh(): void {
  if (refreshQueued) return;
  refreshQueued = true;
  queueMicrotask(() => {
    refreshQueued = false;
    refresh();
  });
}

// ---- Errors thrown by redirect() and notFound(), caught by the router ----------------------------

export class RedirectError extends Error {
  readonly digest = 'NEXT_REDIRECT';
  constructor(
    readonly url: string,
    readonly mode: 'push' | 'replace' = 'replace',
  ) {
    super(`NEXT_REDIRECT;${url}`);
  }
}

export class NotFoundError extends Error {
  readonly digest = 'NEXT_HTTP_ERROR_FALLBACK;404';
  constructor() {
    super('NEXT_NOT_FOUND');
  }
}

export function isRedirectError(error: unknown): error is RedirectError {
  return error instanceof RedirectError;
}

export function isNavigationError(error: unknown): boolean {
  return error instanceof RedirectError || error instanceof NotFoundError;
}
