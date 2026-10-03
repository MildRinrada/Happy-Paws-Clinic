import { staffApi } from '@/lib/api/server-client';
import type { CustomerDetail } from '../types/customer.types';

/** `GET /staff/customers/{id}` — needs `customer:read`. */
export function getCustomer(customerId: string): Promise<CustomerDetail> {
  return staffApi().get<CustomerDetail>(`/staff/customers/${encodeURIComponent(customerId)}`);
}
