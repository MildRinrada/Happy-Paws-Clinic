import { staffApi } from '@/lib/api/server-client';
import type { PortalInvitation } from '../types/customer.types';

/**
 * `POST /staff/customers/{id}/invite` — "ชวนให้เปิดใช้ portal". Needs `customer:write`. Sending
 * again refreshes the live invitation. A customer who already has an account answers `409`.
 */
export function inviteCustomer(customerId: string): Promise<PortalInvitation> {
  return staffApi().post<PortalInvitation>(`/staff/customers/${encodeURIComponent(customerId)}/invite`);
}
