'use server';

import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { fieldErrors } from '@/lib/actions/field-errors';
import { requestPasswordResetLink, setPasswordFromLink } from '../api/resetPassword';
import { forgotPasswordPage } from '../content/password-reset-page';

/**
 * A forgotten password, as two server actions for the two calls to `POST /auth/password/reset`:
 * ask for a link, then use the link's token to set a new password.
 *
 * Neither creates a session. After a reset the person signs in on `/login` with the new password,
 * and every session the account had elsewhere has been ended by the backend.
 */

/** The forgot-password form's state. `sent` is shown the same way whoever the identifier belongs to. */
export type ForgotPasswordState = { status: 'idle' | 'sent'; identifier?: string; error?: string };

const identifier = z.string().trim().min(1).max(256);

export async function requestPasswordReset(_previous: ForgotPasswordState, form: FormData): Promise<ForgotPasswordState> {
  const typed = String(form.get('identifier') ?? '');
  const parsed = identifier.safeParse(typed);

  if (!parsed.success) {
    return { status: 'idle', identifier: typed, error: forgotPasswordPage.errors.missing };
  }

  let result: ActionResult<unknown>;

  try {
    result = await runAction(() => requestPasswordResetLink(parsed.data));
  } catch {
    // The API is down or unreachable. Saying "sent" here would be a lie the person waits on.
    return { status: 'idle', identifier: typed, error: forgotPasswordPage.errors.unreachable };
  }

  if (!result.ok) {
    // The backend answers every well-formed request with `link_sent`, so a refusal is about the
    // request itself (too many requests, a malformed value) and says nothing about any account.
    const perField = Object.values(fieldErrors(result.error))[0];
    return { status: 'idle', identifier: typed, error: perField ?? result.error.message };
  }

  return { status: 'sent', identifier: parsed.data };
}

const newPassword = z.object({
  token: z.string().min(1).max(512),
  // The same bounds the backend applies. It checks again; this saves a round trip.
  password: z.string().min(8).max(256),
});

/** Sets the new password with the token from the link. A dead link comes back as a refusal on `token`. */
export async function setNewPassword(input: z.infer<typeof newPassword>): Promise<ActionResult> {
  const parsed = newPassword.parse(input);
  const result = await runAction(() => setPasswordFromLink(parsed.token, parsed.password));

  return result.ok ? { ok: true, data: undefined } : result;
}
