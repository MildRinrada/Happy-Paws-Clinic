'use server';

import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { acceptInvitation } from '../api/acceptInvitation';
import { qrCodeDataUrl } from '../lib/qr-code';
import type { AuthenticatorSetup } from '../types/invitation.types';

/**
 * Accepting a staff invitation, as two server actions for the two backend calls.
 *
 * Nothing about the invitation is decided here. Whether the link is live, whether the code
 * matches the secret, whether the email already has an account: the backend answers all of it.
 * These check the shape of the input, make the call, and hand back the result as data.
 */

const link = z.object({
  invitationId: z.string().min(1).max(64),
  token: z.string().min(1).max(128),
});

/** First call: asks for the authenticator secret and draws it as a QR code. */
export async function beginInvitationSetup(input: z.infer<typeof link>): Promise<ActionResult<AuthenticatorSetup>> {
  const parsed = link.parse(input);
  const result = await runAction(() => acceptInvitation(parsed));

  if (!result.ok) {
    return result;
  }

  if (result.data.status !== 'two_factor_setup_required') {
    // The backend only answers `accepted` to a call that carries a code, and this one does not.
    throw new Error('The API accepted an invitation without a code.');
  }

  const { email, two_factor } = result.data;

  return {
    ok: true,
    data: { email, secret: two_factor.secret, qrDataUrl: await qrCodeDataUrl(two_factor.otp_auth_uri) },
  };
}

const completion = link.extend({
  // The same bounds the backend applies. It checks again; this saves a round trip.
  password: z.string().min(8).max(256),
  code: z.string().trim().regex(/^\d{6,8}$/),
});

/** Second call: sets the password and proves the authenticator works. Creates the account. */
export async function completeInvitation(input: z.infer<typeof completion>): Promise<ActionResult<{ email: string }>> {
  const parsed = completion.parse(input);
  const result = await runAction(() => acceptInvitation(parsed));

  return result.ok ? { ok: true, data: { email: result.data.email } } : result;
}
