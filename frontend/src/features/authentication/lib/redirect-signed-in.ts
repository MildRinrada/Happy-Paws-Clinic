import { redirect } from 'next/navigation';
import { getMe } from '@/features/account/api/getMe';
import type { Me } from '@/features/account/types/account.types';
import { readAccessToken } from '@/lib/auth/session';
import { destinationFor } from './home-path';

/**
 * Sends someone who is already signed in away from the sign-in page: to `next` when their account
 * may use it, otherwise to where their account belongs. Staff and customers land in different
 * places, and only `GET /me` can say which this is.
 *
 * Any failure to ask (an expired token, the API down) leaves them on the page to sign in again.
 * Not `authed()`: that answers a 401 by redirecting to this very page.
 */
export async function redirectIfSignedIn(next?: string): Promise<void> {
  if (!(await readAccessToken())) {
    return;
  }

  let me: Me;

  try {
    me = await getMe();
  } catch {
    return;
  }

  const staff = me.memberships.some((membership) => membership.type === 'staff' && membership.staff_id);
  const customer = me.memberships.some((membership) => membership.type === 'customer' && membership.customer_id);

  // An account with neither has nowhere to go: both the console and the portal would send it back
  // here, and back again. It stays on the page instead, free to sign in as someone else.
  if (!staff && !customer) {
    return;
  }

  redirect(destinationFor({ staff, customer }, next));
}
