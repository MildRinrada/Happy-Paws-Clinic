import { isRedirectError, navigate } from './navigation';

/**
 * Wraps each export of a `'use server'` file (see plugins/next-compat.ts). The function itself runs
 * in the browser unchanged; what Next does around a server action is done here: `redirect()` inside
 * it moves to the new page, and the promise of the call that redirected never settles, because the
 * form that made it is about to be gone. `revalidatePath()` already queued a refresh on its own.
 */
export function serverAction<A extends unknown[], R>(fn: (...args: A) => Promise<R>, _name: string) {
  return async (...args: A): Promise<R> => {
    try {
      return await fn(...args);
    } catch (error) {
      if (isRedirectError(error)) {
        navigate(error.url, { replace: error.mode === 'replace' });
        return new Promise<R>(() => {});
      }
      throw error;
    }
  };
}
