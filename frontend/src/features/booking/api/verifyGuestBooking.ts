import { postGuestBookingVerify, type SignedIn } from '@/lib/api/auth-transport';
import type { GuestVerified } from '../types/booking.types';

export interface VerifyGuestBookingInput {
  holdId: string;
  phone: string;
  code: string;
  displayName: string;
  policyVersion: string;
  marketingConsent: boolean;
}

/**
 * `POST /booking/guest/verify` (docs/contract-extensions.md § 27) — the code proves the number and
 * the visitor comes back a signed-in customer. Goes through `lib/api/auth-transport.ts`, the one
 * place allowed to read the refresh token out of `Set-Cookie`. Server-side only.
 *
 * Refusals are thrown as `ApiError`: a wrong or expired code is `422` on `code`, a lapsed hold
 * `409 hold_expired`, and a staff number `409 contact_taken`.
 */
export function verifyGuestBooking(input: VerifyGuestBookingInput): Promise<{ signedIn: SignedIn; payload: GuestVerified }> {
  return postGuestBookingVerify<GuestVerified>({
    hold_id: input.holdId,
    phone: input.phone,
    code: input.code,
    display_name: input.displayName,
    accept_privacy_notice: true,
    policy_version: input.policyVersion,
    marketing_consent: input.marketingConsent,
  });
}
