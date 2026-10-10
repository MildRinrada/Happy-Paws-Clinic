import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { DueBackCustomer, DueBackReason } from '../types/due-back.types';

/**
 * `GET /staff/customers/due-back?reason=&branch=&cursor=`: customers to call, most recently due
 * first. Needs `crm:read`. docs/contract-extensions.md § 33.
 */
export function listDueBack(reason: DueBackReason, branch: string, cursor?: string): Promise<CursorPage<DueBackCustomer>> {
  return staffApi().get<CursorPage<DueBackCustomer>>('/staff/customers/due-back', {
    query: { reason, branch, cursor, limit: 25 },
  });
}
