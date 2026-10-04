'use server';

import { redirect } from 'next/navigation';
import { z } from 'zod';
import { ApiError } from '@/lib/api/api-error';
import { postLogin, postLogout, postVerifyTwoFactor, type LoginOutcome, type SignedIn } from '@/lib/api/auth-transport';
import { oneTimeCodeDigits } from '@/lib/auth/one-time-code';
import { SIGN_IN_PATH, clearSession, readRefreshToken, writeSession } from '@/lib/auth/session';
import { signInPage } from '../content/sign-in-page';
import { destinationFor } from '../lib/home-path';

/**
 * Sign-in for everyone: a phone number or an email with a password, then a second factor for an
 * account that has one.
 *
 * One page for customers and staff, because the backend has one endpoint for both
 * (`POST /auth/login`, "ทั้งสองฝั่ง"). Where the person lands afterwards comes from the
 * capabilities the backend returns with the tokens, not from which page they used.
 *
 * The two steps are two backend calls and the first does not produce a session when a second
 * factor is enrolled: it gives a short-lived challenge and no token, and only
 * `POST /auth/2fa/verify` mints one. So the challenge goes back to the form as plain state, and
 * the session cookies are written only once there are real tokens to put in them.
 *
 * Neither token is ever returned to the browser as data. They go into httpOnly cookies here, on
 * the server, and the page gets a redirect.
 */

/** `next` is the page the visitor was heading to; it rides along in the state between the steps. */
export type SignInState =
  | { step: 'credentials'; identifier?: string; next?: string; error?: string }
  | { step: 'second_factor'; challenge: string; identifier: string; next?: string; error?: string };

const credentials = z.object({
  identifier: z.string().trim().min(1).max(256),
  password: z.string().min(1).max(256),
});

const secondFactor = z.object({
  challenge: z.string().min(1),
  code: z.string().regex(/^\d{6,8}$/),
});

/** The backend's own message, which deliberately does not say which half of the pair was wrong. */
function refusal(error: unknown): string {
  if (error instanceof ApiError) {
    return error.status === 429 ? signInPage.errors.tooMany : error.message;
  }
  // The API is down or unreachable. Say so, rather than blaming the password.
  return signInPage.errors.unreachable;
}

async function finish({ tokens, isStaff, isCustomer }: SignedIn, next?: string): Promise<never> {
  await writeSession(tokens);
  redirect(destinationFor({ staff: isStaff, customer: isCustomer }, next));
}

export async function signIn(previous: SignInState, form: FormData): Promise<SignInState> {
  const identifier = String(form.get('identifier') ?? '');
  const next = previous.next;
  const parsed = credentials.safeParse({ identifier, password: form.get('password') });

  if (!parsed.success) {
    return { step: 'credentials', identifier, next, error: signInPage.errors.missing };
  }

  let outcome: LoginOutcome;

  try {
    outcome = await postLogin(parsed.data.identifier, parsed.data.password);
  } catch (error) {
    // The identifier goes back so it stays in the box; the password never does.
    return { step: 'credentials', identifier, next, error: refusal(error) };
  }

  if (outcome.kind === 'second_factor') {
    return { step: 'second_factor', challenge: outcome.challenge, identifier: parsed.data.identifier, next };
  }

  return finish(outcome, next);
}

export async function verifySecondFactor(previous: SignInState, form: FormData): Promise<SignInState> {
  if (previous.step !== 'second_factor') {
    return { step: 'credentials' };
  }

  const parsed = secondFactor.safeParse({
    challenge: previous.challenge,
    code: oneTimeCodeDigits(String(form.get('code') ?? '')),
  });

  if (!parsed.success) {
    return { ...previous, error: signInPage.errors.badCode };
  }

  let signedIn: SignedIn;

  try {
    signedIn = await postVerifyTwoFactor(parsed.data.challenge, parsed.data.code);
  } catch (error) {
    // The backend answers a wrong code with the same generic 401 it uses for a wrong password,
    // and is right to. But at this step the password has already been accepted, so repeating
    // "อีเมลหรือรหัสผ่านไม่ถูกต้อง" would send the user to retype the wrong thing. The challenge
    // also lapses after five minutes, which looks the same from here.
    const message =
      error instanceof ApiError && error.status === 401 ? signInPage.errors.wrongCode : refusal(error);

    return { ...previous, error: message };
  }

  return finish(signedIn, previous.next);
}

/** Revokes the session on the backend and drops the cookies. The cookies go whatever the backend says. */
export async function signOut(): Promise<void> {
  const refreshToken = await readRefreshToken();

  if (refreshToken) {
    await postLogout(refreshToken);
  }

  await clearSession();
  redirect(SIGN_IN_PATH);
}
