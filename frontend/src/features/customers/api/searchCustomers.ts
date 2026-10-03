import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { Customer } from '../types/customer.types';

/**
 * `GET /staff/customers?q=` — "ค้นด้วยชื่อ เบอร์ HN หรือชื่อสัตว์". Needs `customer:read`. With no
 * query it lists the register newest first, which is the counter's "who did we just add".
 */
export function searchCustomers(query: { q?: string; cursor?: string; limit?: number }): Promise<CursorPage<Customer>> {
  return staffApi().get<CursorPage<Customer>>('/staff/customers', {
    query: { q: query.q || undefined, cursor: query.cursor, limit: query.limit ?? 20 },
  });
}
