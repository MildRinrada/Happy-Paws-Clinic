/**
 * Where someone lands once signed in. Staff go to the console; a customer to the portal's home,
 * which opens on their visit and queue.
 *
 * Plain constants and functions, with no server imports, so the middleware and client code can
 * use them too.
 */

export const STAFF_HOME_PATH = '/queue';

export const CUSTOMER_HOME_PATH = '/portal';

/** The query parameter that carries the page someone was on their way to when asked to sign in. */
export const NEXT_PARAM = 'next';

export function homePathFor(isStaff: boolean): string {
  return isStaff ? STAFF_HOME_PATH : CUSTOMER_HOME_PATH;
}

/** Which sides of the site an account may use. One account can be both (a vet who owns a pet). */
export interface AccountKinds {
  staff: boolean;
  customer: boolean;
}

/**
 * The page to go to after signing in: where they were heading, if that is a page of this site
 * their account may use, and their home otherwise.
 *
 * `next` comes from the address bar, so it is checked, not trusted. Only a path on this site is
 * accepted (`/x`, never `//host` or `https://...`), which keeps the sign-in page from being used to
 * send someone to another site. A portal page needs a customer and any other page needs staff, so
 * nobody lands on a page that would bounce them straight back here.
 */
export function destinationFor(kinds: AccountKinds, next?: string | null): string {
  if (typeof next === 'string' && /^\/(?![/\\])/.test(next) && next !== '/login') {
    const inPortal = next === CUSTOMER_HOME_PATH || next.startsWith(`${CUSTOMER_HOME_PATH}/`) || next.startsWith(`${CUSTOMER_HOME_PATH}?`);

    if (inPortal ? kinds.customer : kinds.staff) {
      return next;
    }
  }

  return homePathFor(kinds.staff);
}
