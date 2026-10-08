import { staffApi } from '@/lib/api/server-client';
import type { CustomerLine } from '../types/customer.types';

/**
 * A customer's LINE address (customer:write), a contract extension
 * (docs/contract-extensions.md § 18). LINE is a channel to send on, never a sign-in.
 */

/** `PUT /staff/customers/{id}/line` with an address from the follower list. */
export function linkLineAddress(customerId: string, version: number, lineUserId: string): Promise<CustomerLine> {
  return staffApi().put<CustomerLine>(`/staff/customers/${encodeURIComponent(customerId)}/line`, {
    body: { line_user_id: lineUserId },
    ifMatch: version,
  });
}

/** `DELETE /staff/customers/{id}/line`. Messages go by SMS or email afterwards. */
export function unlinkLineAddress(customerId: string, version: number): Promise<void> {
  return staffApi().delete<void>(`/staff/customers/${encodeURIComponent(customerId)}/line`, { ifMatch: version });
}
