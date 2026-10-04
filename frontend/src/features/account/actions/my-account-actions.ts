'use server';

import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';
import { z } from 'zod';
import { qrCodeDataUrl } from '@/features/staff/lib/qr-code';
import { runAction, type ActionError, type ActionResult } from '@/lib/actions/action-result';
import { ApiError } from '@/lib/api/api-error';
import { ONE_TIME_CODE_MAX_LENGTH, oneTimeCodeDigits } from '@/lib/auth/one-time-code';
import { SIGN_IN_PATH, clearSession } from '@/lib/auth/session';
import { changePassword } from '../api/changePassword';
import { beginTwoFactorEnrolment, confirmTwoFactorEnrolment } from '../api/enrolTwoFactor';
import { listSessions, revokeSession } from '../api/sessions';
import { updateMe } from '../api/updateMe';
import { passwordSettings } from '../content/password-settings';
import { twoFactorSettings } from '../content/two-factor-settings';
import { profileValues, type ProfileValues } from '../lib/profile-changes';
import type { AuthenticatorSecret } from '../types/account.types';

/**
 * The staff account page's changes, all about the signed-in person themself. The backend checks
 * every rule (which password is right, which address is taken, whether this session may replace a
 * second factor); these check the shape, make the call, and hand the refusal back to the form.
 *
 * Kept apart from `account-actions.ts`, which serves the customer portal's page with its own
 * result shapes and paths to refresh.
 *
 * Fields that the backend checks for content (an empty password, a short new one) are left to it,
 * so its Thai message lands under the field instead of a generic failure from `parse`.
 */

const PAGE = '/account';

const channels = z.object({ email: z.boolean(), sms: z.boolean(), line: z.boolean() });

const profile = z.object({
  version: z.number().int().nonnegative().optional(),
  changes: z.object({
    display_name: z.string().max(200).optional(),
    email: z.string().trim().max(256).optional(),
    phone: z.string().trim().max(32).optional(),
    language: z.enum(['th', 'en']).optional(),
    notifications: channels.optional(),
    current_password: z.string().max(256).optional(),
  }),
});

/** Answers with the profile as stored, so the form compares its next edit against that. */
export async function updateMyProfileAction(
  payload: z.infer<typeof profile>,
): Promise<ActionResult<{ version?: number; values: ProfileValues }>> {
  const { version, changes } = profile.parse(payload);
  const result = await runAction(() => updateMe(version, changes));

  // The name is also in the console's header, which this page's layout renders.
  revalidatePath(PAGE);
  return result.ok ? { ok: true, data: { version: result.data.version, values: profileValues(result.data) } } : result;
}

const password = z.object({
  current_password: z.string().max(256),
  new_password: z.string().max(256),
});

/**
 * `POST /me/password` answers a wrong current password with `401` (a known mismatch with the newer
 * endpoints, which answer `422`; see docs/features/account.md). `runAction` reads any 401 as "the
 * session is over" and would send the person to sign in, so the 401 is turned into the refusal it
 * means, on the field it is about, before `runAction` sees it. The middleware refreshed the session
 * for this very request, so an expired session is not what the 401 is saying.
 */
async function changePasswordOrRefuse(currentPassword: string, newPassword: string): Promise<void> {
  try {
    await changePassword(currentPassword, newPassword);
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      throw new ApiError(422, {
        code: error.code,
        message: passwordSettings.wrongCurrent,
        field: 'current_password',
        request_id: error.requestId,
      });
    }
    throw error;
  }
}

/**
 * The backend revokes every other session of the account and keeps this one, so the cookies stay
 * as they are. The page is refreshed so the device list shows the others gone.
 */
export async function changeMyPasswordAction(values: z.infer<typeof password>): Promise<ActionResult> {
  const input = password.parse(values);
  const result = await runAction(() => changePasswordOrRefuse(input.current_password, input.new_password));

  revalidatePath(PAGE);
  return result.ok ? { ok: true, data: undefined } : result;
}

/** The backend's 403 and 409 here are generic sentences; on this page they have a specific cause. */
function enrolmentRefusal(error: ActionError): { ok: false; error: ActionError } {
  if (error.status === 403) return { ok: false, error: { ...error, message: twoFactorSettings.errors.needsSecondFactor } };
  if (error.code === 'state_conflict') return { ok: false, error: { ...error, message: twoFactorSettings.errors.notStarted } };
  return { ok: false, error };
}

/**
 * Step 1 of setting up the authenticator. The QR code is drawn here, on the server, and only the
 * image and the secret go to the browser: the person has to see the secret to type it into the app,
 * and the `otpauth://` URI is the same secret in another form, so it is not sent as well.
 */
export async function beginTwoFactorAction(values: { current_password: string }): Promise<ActionResult<AuthenticatorSecret>> {
  const { current_password } = z.object({ current_password: z.string().max(256) }).parse(values);
  const result = await runAction(() => beginTwoFactorEnrolment(current_password));

  if (!result.ok) return enrolmentRefusal(result.error);

  const { secret, otp_auth_uri } = result.data;
  if (!secret || !otp_auth_uri) {
    throw new Error('POST /me/2fa/enroll answered step 1 without a secret');
  }

  return { ok: true, data: { secret, qrDataUrl: await qrCodeDataUrl(otp_auth_uri) } };
}

/**
 * Step 2: a code from the app switches the new factor on and returns the recovery codes. They go
 * back to the form once and are never stored on this side.
 *
 * The session cookies are left alone. The backend marks this session as having passed the second
 * factor, so its next refresh keeps working; other sessions that never passed one must sign in
 * again, which the middleware handles when their refresh is refused.
 */
export async function confirmTwoFactorAction(values: { code: string }): Promise<ActionResult<{ recoveryCodes: string[] }>> {
  const parsed = z.object({ code: z.string().max(ONE_TIME_CODE_MAX_LENGTH) }).parse(values);
  const result = await runAction(() => confirmTwoFactorEnrolment(oneTimeCodeDigits(parsed.code)));

  if (!result.ok) return enrolmentRefusal(result.error);

  revalidatePath(PAGE);
  return { ok: true, data: { recoveryCodes: result.data.recovery_codes ?? [] } };
}

/**
 * Signs one device out. The page never offers this for the device in use (the header's sign-out
 * does that properly, revoking by refresh token), but the list it was drawn from may be stale. If
 * the session revoked turns out to be this one, its cookies are now worthless: drop them and go to
 * the sign-in page rather than leave a page that fails on its next request.
 */
export async function revokeMySessionAction(values: { sessionId: string }): Promise<ActionResult> {
  const { sessionId } = z.object({ sessionId: z.string().min(1).max(64) }).parse(values);

  const result = await runAction(async () => {
    const wasCurrent = (await listSessions()).some((session) => session.id === sessionId && session.current);
    await revokeSession(sessionId);
    return wasCurrent;
  });

  if (!result.ok) return result;

  if (result.data) {
    await clearSession();
    redirect(SIGN_IN_PATH);
  }

  revalidatePath(PAGE);
  return { ok: true, data: undefined };
}
