import { serverEnv } from '@/config/env';

/**
 * Opens a server-sent-event stream on the backend and hands back the raw response, so a route
 * handler can pipe its body to the browser untouched.
 *
 * The browser cannot do this itself: `EventSource` sends no headers, and the access token is in a
 * cookie script cannot read. It connects to this app's own origin instead, and the bearer token
 * is attached here — the same path every other authenticated call takes (docs/assumptions.md
 * § 11). The token never appears in a URL.
 */
export function openEventStream(
  path: string,
  query: Record<string, string>,
  accessToken: string,
  signal: AbortSignal,
): Promise<Response> {
  const url = new URL(serverEnv.apiBaseUrl().replace(/\/$/, '') + path);

  for (const [key, value] of Object.entries(query)) {
    url.searchParams.set(key, value);
  }

  return fetch(url, {
    headers: { Accept: 'text/event-stream', Authorization: `Bearer ${accessToken}` },
    cache: 'no-store',

    // The browser closing its side aborts this, which closes the backend's.
    signal,
  });
}
