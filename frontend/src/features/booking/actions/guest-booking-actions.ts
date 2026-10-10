'use server';

import { z } from 'zod';
import { isValidPhone, phoneDigits } from '@/features/authentication/lib/phone-number';
import { readReferralCode } from '@/features/referrals/lib/read-referral-code';
import { runAction, type ActionError, type ActionResult } from '@/lib/actions/action-result';
import { oneTimeCodeDigits } from '@/lib/auth/one-time-code';
import { writeSession } from '@/lib/auth/session';
import { startGuestBooking } from '../api/startGuestBooking';
import { verifyGuestBooking } from '../api/verifyGuestBooking';
import { guestBookingPage } from '../content/guest-booking-page';
import type { GuestBookingStarted, GuestVerified } from '../types/booking.types';

/**
 * Booking without signing up first (docs/contract-extensions.md § 27), as two server actions for
 * the two backend calls: hold the time and text a code, then prove the number.
 *
 * Who the number belongs to is the backend's to decide, and only after the code. These check the
 * shape of the input, make the call, and hand any refusal back as data for the form. The tokens a
 * successful check returns never reach the browser: they go into the httpOnly session cookies
 * here, exactly as at sign-up, and the visitor is from then on an ordinary signed-in customer.
 */

/** A refusal made here, before the backend is asked, in the same shape as one from the backend. */
function invalid(field: string, message: string): { ok: false; error: ActionError } {
  return { ok: false, error: { code: 'validation_failed', message, field, status: 422 } };
}

const id = z.string().min(1).max(64);

const startInput = z.object({
  phone: z.string().max(32),
  serviceId: id,
  branchId: id,
  startsAt: z.iso.datetime({ offset: true }),
  staffId: id.optional(),
});

/** Holds the picked time for ten minutes and sends a six-digit code to the phone. */
export async function startGuestBookingAction(input: z.input<typeof startInput>): Promise<ActionResult<GuestBookingStarted>> {
  const parsed = startInput.safeParse(input);

  if (!parsed.success || !isValidPhone(parsed.data.phone)) {
    return invalid('phone', guestBookingPage.phone.badPhone);
  }

  return runAction(() => startGuestBooking({ ...parsed.data, phone: phoneDigits(parsed.data.phone) }));
}

const verifyInput = z.object({
  holdId: id,
  phone: z.string().refine(isValidPhone),
  code: z.string().transform(oneTimeCodeDigits).pipe(z.string().regex(/^\d{6}$/)),
  displayName: z.string().trim().min(1).max(200),
  acceptPrivacy: z.boolean().refine((accepted) => accepted),
  marketingConsent: z.boolean(),
  // A friend's referral code (docs/contract-extensions.md § 39). One that is not a code is dropped
  // rather than refused: it must never stand between the visitor and the booking.
  referralCode: z.string().max(32).optional().catch(undefined),
});

export type VerifyGuestInput = z.input<typeof verifyInput>;

/**
 * Proves the number and signs the visitor in. Answers their pets and the hold, now theirs, for the
 * last step; the booking itself is then `bookAction`, as on the portal's booking page. No notice
 * version is sent: the backend stamps the consents with the notice it has published
 * (docs/contract-extensions.md § 35).
 */
export async function verifyGuestBookingAction(input: VerifyGuestInput): Promise<ActionResult<GuestVerified>> {
  const parsed = verifyInput.safeParse(input);

  if (!parsed.success) {
    const field = String(parsed.error.issues[0]?.path[0] ?? '');
    const text = guestBookingPage.code;
    const messages: Record<string, [string, string]> = {
      code: ['code', text.badCode],
      displayName: ['display_name', text.missingName],
      acceptPrivacy: ['accept_privacy_notice', text.mustAccept],
    };
    const [wireField, message] = messages[field] ?? ['code', text.badCode];

    return invalid(wireField, message);
  }

  const { holdId, phone, code, displayName, marketingConsent, referralCode } = parsed.data;
  const result = await runAction(() =>
    verifyGuestBooking({
      holdId,
      phone: phoneDigits(phone),
      code,
      displayName,
      marketingConsent,
      referralCode: readReferralCode(referralCode),
    }),
  );

  if (!result.ok) {
    return result;
  }

  await writeSession(result.data.signedIn.tokens);

  return { ok: true, data: result.data.payload };
}
