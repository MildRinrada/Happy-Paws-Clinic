import { staffApi } from '@/lib/api/server-client';
import type { StaffReferralPage } from '../types/referral.types';

/**
 * `GET /staff/referrals?customer=&cursor=` (docs/contract-extensions.md § 39), on `crm:read`: who
 * referred whom, newest first, all of them or where one customer is either side.
 */
export function listStaffReferrals(query: { customer?: string; cursor?: string }): Promise<StaffReferralPage> {
  return staffApi().get<StaffReferralPage>('/staff/referrals', { query: { ...query, limit: 25 } });
}
