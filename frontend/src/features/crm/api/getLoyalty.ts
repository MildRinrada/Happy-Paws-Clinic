import { staffApi } from '@/lib/api/server-client';
import type { CustomerLoyalty } from '../types/crm.types';

/** `GET /staff/loyalty/{customer_id}`. Needs `crm:read`. 404 when the customer does not exist. */
export function getLoyalty(customerId: string): Promise<CustomerLoyalty> {
  return staffApi().get<CustomerLoyalty>(`/staff/loyalty/${encodeURIComponent(customerId)}`);
}
