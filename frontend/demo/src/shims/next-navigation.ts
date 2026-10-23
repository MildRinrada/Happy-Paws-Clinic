import { useContext, useMemo } from 'react';
import { ParamsContext, PlaceContext } from '@demo/runtime/contexts';
import { isNavigationError, navigate, NotFoundError, RedirectError, refresh } from '@demo/runtime/navigation';

/** `next/navigation`, answered by the demo's router. */

const router = {
  push: (href: string, options?: { scroll?: boolean }) => navigate(href, { scroll: options?.scroll }),
  replace: (href: string, options?: { scroll?: boolean }) => navigate(href, { replace: true, scroll: options?.scroll }),
  refresh: () => refresh(),
  back: () => history.back(),
  forward: () => history.forward(),
  prefetch: () => {},
};

export function useRouter() {
  return router;
}

export function usePathname(): string {
  return useContext(PlaceContext).pathname;
}

export function useSearchParams(): URLSearchParams {
  const { search } = useContext(PlaceContext);
  return useMemo(() => new URLSearchParams(search), [search]);
}

export function useParams<T extends Record<string, string> = Record<string, string>>(): T {
  return useContext(ParamsContext) as T;
}

export function useSelectedLayoutSegment(): string | null {
  return null;
}

export const RedirectType = { push: 'push', replace: 'replace' } as const;

export function redirect(url: string, type: 'push' | 'replace' = 'replace'): never {
  throw new RedirectError(url, type);
}

export function permanentRedirect(url: string): never {
  throw new RedirectError(url, 'replace');
}

export function notFound(): never {
  throw new NotFoundError();
}

export function unstable_rethrow(error: unknown): void {
  if (isNavigationError(error)) throw error;
}
