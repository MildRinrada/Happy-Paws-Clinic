'use server';

import { redirect } from 'next/navigation';
import { z } from 'zod';
import { runAction, type ActionError, type ActionResult } from '@/lib/actions/action-result';
import { oneTimeCodeDigits } from '@/lib/auth/one-time-code';
import { SIGN_IN_PATH, writeSession } from '@/lib/auth/session';
import { postRegister } from '../api/register';
import { requestRegisterOtp } from '../api/requestOtp';
import { registerPage } from '../content/register-page';
import { CUSTOMER_HOME_PATH, destinationFor } from '../lib/home-path';
import { isValidPhone, phoneDigits } from '../lib/phone-number';

/**
 * Customer sign-up, as two server actions for the two backend calls: send a code to the phone,
 * then open the account with that code.
 *
 * Nothing about the account is decided here. Whether the code is right, whether the number already
 * has an account, whether too many codes were asked for: the backend answers all of it. These
 * check the shape of the input, make the call, and hand any refusal back as data for the form.
 *
 * The tokens a successful sign-up returns never reach the browser. They go into the httpOnly
 * session cookies here, and the page gets a redirect.
 */

/** A refusal made here, before the backend is asked, in the same shape as one from the backend. */
function invalid(field: string, message: string): { ok: false; error: ActionError } {
  return { ok: false, error: { code: 'validation_failed', message, field, status: 422 } };
}

/** Sends a six-digit code by SMS. Answers when it stops working, for the form to show. */
export async function requestRegisterCode(input: { phone: string }): Promise<ActionResult<{ expiresAt: string }>> {
  if (!isValidPhone(String(input.phone ?? ''))) {
    return invalid('phone', registerPage.errors.badPhone);
  }

  const result = await runAction(() => requestRegisterOtp(phoneDigits(input.phone)));

  return result.ok ? { ok: true, data: { expiresAt: result.data.expires_at } } : result;
}

// The same bounds the backend applies. It checks again; this saves a round trip.
const registration = z.object({
  phone: z.string().refine(isValidPhone),
  code: z.string().transform(oneTimeCodeDigits).pipe(z.string().regex(/^\d{6}$/)),
  displayName: z.string().trim().min(1).max(200),
  // Optional: an empty box is no email, not an invalid one.
  email: z
    .string()
    .trim()
    .transform((value) => value || undefined)
    .pipe(z.email().max(256).optional()),
  // The backend allows an account with no password, but this site has no sign-in by code yet, so
  // such an account could not sign in here again. The page therefore always asks for one.
  password: z.string().min(8).max(256),
});

export type RegisterInput = z.input<typeof registration>;

/**
 * Opens the account and signs the new customer in. Only a refusal returns; success redirects to
 * the portal, or to the sign-in page with a notice if the backend made the account but did not
 * sign it in.
 */
export async function registerCustomer(input: RegisterInput): Promise<ActionResult> {
  const parsed = registration.safeParse(input);

  if (!parsed.success) {
    const field = String(parsed.error.issues[0]?.path[0] ?? '');
    const messages: Record<string, string> = {
      phone: registerPage.errors.badPhone,
      code: registerPage.errors.badCode,
      displayName: registerPage.errors.missingName,
      email: registerPage.errors.badEmail,
      password: registerPage.errors.passwordTooShort,
    };
    // Field names go back in the API's snake_case, so the form places them the same way either way.
    return invalid(field === 'displayName' ? 'display_name' : field, messages[field] ?? registerPage.errors.badCode);
  }

  const { phone, code, displayName, email, password } = parsed.data;
  const result = await runAction(() =>
    postRegister({ phone: phoneDigits(phone), code, displayName, email, password }),
  );

  if (!result.ok) {
    return result;
  }

  if (!result.data) {
    redirect(`${SIGN_IN_PATH}?notice=registered`);
  }

  const { tokens, isStaff, isCustomer } = result.data;
  await writeSession(tokens);
  redirect(destinationFor({ staff: isStaff, customer: isCustomer }, CUSTOMER_HOME_PATH));
}
