import { cookies } from 'next/headers';
import { DISPLAY_PATH } from './display-paths';

/**
 * The waiting-room screen's credential on this side: its display token, in a cookie of its own
 * (docs/contract-extensions.md § 30).
 *
 * - httpOnly, so script on the screen never sees it, exactly like the staff session's tokens.
 * - `Path=/display`, so the browser sends it to the screen's page, its setup page and its stream
 *   and to nothing else on this site. The staff console never receives it, and a staff session on
 *   the same browser is a separate pair of cookies the screen never reads.
 * - Kept for 400 days, the longest a browser keeps any cookie, or until the display's own end
 *   date when it has one. The backend decides whether the token still works on every request.
 */

export const DISPLAY_COOKIE = 'hp_display';

const MAX_AGE_SECONDS = 60 * 60 * 24 * 400;

/** The screen's token for this request, or `undefined` when this browser was never set up. */
export async function readDisplayToken(): Promise<string | undefined> {
  return (await cookies()).get(DISPLAY_COOKIE)?.value;
}

/** Stores a freshly claimed token. Only callable from a server action or a route handler. */
export async function writeDisplayToken(token: string, expiresAt?: string): Promise<void> {
  const untilEnd = expiresAt ? Math.floor((Date.parse(expiresAt) - Date.now()) / 1000) : MAX_AGE_SECONDS;

  (await cookies()).set(DISPLAY_COOKIE, token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax',
    path: DISPLAY_PATH,
    maxAge: Math.max(60, Math.min(untilEnd, MAX_AGE_SECONDS)),
  });
}
