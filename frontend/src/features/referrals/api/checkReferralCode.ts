import { publicApi } from '@/lib/api/server-client';
import type { ReferralCodeCheck } from '../types/referral.types';

/**
 * `GET /referrals/{code}` (docs/contract-extensions.md § 39): whether a code can be used. Public and
 * rate-limited per address. Never cached, since the clinic can switch the programme off.
 */
export function checkReferralCode(code: string): Promise<ReferralCodeCheck> {
  return publicApi().get<ReferralCodeCheck>(`/referrals/${encodeURIComponent(code)}`);
}
