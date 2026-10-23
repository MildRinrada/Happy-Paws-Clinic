import { queueRefresh } from '@demo/runtime/navigation';

/**
 * `next/cache`. Every page read is fresh in each generation, so "drop the cached page" is simply
 * "show the page again with new data": a refresh, run once even when an action revalidates twice.
 */

export function revalidatePath(_path?: string, _type?: 'page' | 'layout'): void {
  queueRefresh();
}

export function revalidateTag(_tag?: string): void {
  queueRefresh();
}

export function unstable_cache<T extends (...args: never[]) => unknown>(fn: T, _keyParts?: string[], _options?: { revalidate?: number | false; tags?: string[] }): T {
  return fn;
}

export function unstable_noStore(): void {}
