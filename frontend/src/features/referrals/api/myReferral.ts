import { staffApi } from '@/lib/api/server-client';
import type { MyReferral } from '../types/referral.types';

/**
 * `GET /portal/referral` (docs/contract-extensions.md § 39): the signed-in customer's own code,
 * made on the first read, the link to share, and counts of what came of it. The customer comes from
 * the session; nothing here can name anyone else.
 */
export function getMyReferral(): Promise<MyReferral> {
  return staffApi().get<MyReferral>('/portal/referral');
}
