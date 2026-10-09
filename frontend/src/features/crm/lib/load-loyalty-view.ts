import { getCustomer } from '@/features/customers/api/getCustomer';
import { ApiError, optional } from '@/lib/api/api-error';
import { authed } from '@/lib/auth/session';
import { getLoyalty } from '../api/getLoyalty';
import type { CustomerLoyalty } from '../types/crm.types';

export type LoyaltyView =
  | { kind: 'found'; loyalty: CustomerLoyalty; customerName?: string; customerPhone?: string }
  | { kind: 'not_found' }
  | { kind: 'refused'; message: string };

/**
 * One customer's loyalty page. The name comes from the register under `customer:read` and is
 * optional: without it the page still shows the points, under the customer id.
 */
export async function loadLoyaltyView(customerId: string, canReadCustomer: boolean): Promise<LoyaltyView> {
  try {
    const [loyalty, detail] = await Promise.all([
      authed(() => getLoyalty(customerId)),
      canReadCustomer ? optional(getCustomer(customerId)).catch(() => null) : Promise.resolve(null),
    ]);

    return { kind: 'found', loyalty, customerName: detail?.customer.display_name, customerPhone: detail?.customer.phone };
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return { kind: 'not_found' };
    // 403, or an id the API will not take: said on the page, under the search box, to try again.
    if (error instanceof ApiError && error.status < 500) return { kind: 'refused', message: error.message };
    throw error;
  }
}
