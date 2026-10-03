import { serverEnv } from '@/config/env';
import { readAccessToken } from '@/lib/auth/session';
import { createHttpClient, type HttpClient } from './http-client';

/**
 * The backend client for server code. Every feature's `api/` module builds on one of these two.
 *
 * Created per call rather than once at module load, so a missing `API_BASE_URL` fails the request
 * that needed it instead of the build that merely imported this file.
 */

/** Calls the backend as the signed-in user, with the access token the middleware kept fresh. */
export function staffApi(): HttpClient {
  return createHttpClient({
    baseUrl: serverEnv.apiBaseUrl(),
    authHeaders: async () => {
      const token = await readAccessToken();
      const headers: Record<string, string> = {};

      // No token means no header. The backend then answers 401, which is the truthful outcome and
      // the one `authed()` turns into a trip to the sign-in page.
      if (token) {
        headers.Authorization = `Bearer ${token}`;
      }

      return headers;
    },
  });
}

/** Calls the contract's public reads — `/clinic`, `/services` — with no credentials at all. */
export function publicApi(): HttpClient {
  return createHttpClient({ baseUrl: serverEnv.apiBaseUrl() });
}
