import { cache } from 'react';
import { getMe } from '@/features/account/api/getMe';
import type { Me } from '@/features/account/types/account.types';
import { authed } from '@/lib/auth/session';

export interface CustomerContext {
  me: Me;
  customerId: string;
}

/**
 * Who is signed in to the portal, resolved once per request.
 *
 * A session with no customer membership (a member of staff who is not also a pet owner) has
 * nothing to see here and is treated as not signed in to it.
 */
export const loadCustomerContext = cache(async (): Promise<CustomerContext | null> => {
  const me = await authed(getMe);
  const customer = me.memberships.find((membership) => membership.type === 'customer');

  return customer?.customer_id ? { me, customerId: customer.customer_id } : null;
});
