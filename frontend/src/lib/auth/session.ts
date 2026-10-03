import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';
import { ApiError } from '@/lib/api/api-error';
import {
  ACCESS_COOKIE,
  REFRESH_COOKIE,
  clearedCookieWrites,
  sessionCookieWrites,
  type SessionTokens,
} from './session-cookies';

/**
 * Reading and writing the session from server code — server components, server actions and route
 * handlers. Refreshing is not here: that happens once per request, in the middleware, before any
 * of this runs.
 */

export const SIGN_IN_PATH = '/login';

/** The access token for this request, or `undefined` when nobody is signed in. */
export async function readAccessToken(): Promise<string | undefined> {
  return (await cookies()).get(ACCESS_COOKIE)?.value;
}

export async function readRefreshToken(): Promise<string | undefined> {
  return (await cookies()).get(REFRESH_COOKIE)?.value;
}

/** Starts a session. Only callable where cookies can be written: an action or a route handler. */
export async function writeSession(tokens: SessionTokens): Promise<void> {
  const jar = await cookies();

  for (const { name, value, options } of sessionCookieWrites(tokens)) {
    jar.set(name, value, options);
  }
}

export async function clearSession(): Promise<void> {
  const jar = await cookies();

  for (const { name, value, options } of clearedCookieWrites()) {
    jar.set(name, value, options);
  }
}

/**
 * Runs a backend read for a page, sending the visitor to sign in if the session turns out to be
 * over. The middleware keeps a live session fresh, so a 401 here means there is no session left
 * to refresh — revoked from another device, or deactivated.
 */
export async function authed<T>(read: () => Promise<T>): Promise<T> {
  try {
    return await read();
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      redirect(SIGN_IN_PATH);
    }
    throw error;
  }
}
