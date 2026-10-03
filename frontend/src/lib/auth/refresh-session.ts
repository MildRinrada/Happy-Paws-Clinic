import { postRefresh } from '@/lib/api/auth-transport';
import type { SessionTokens } from './session-cookies';

/**
 * Refreshes a session exactly once, however many requests ask at the same moment.
 *
 * The backend rotates the refresh token on every use and treats a second presentation of a spent
 * one as theft: it revokes the whole session (docs/authentication.md § 3). That is the right rule
 * and it has a sharp edge for a browser, which fires a page request, a stream and a server action
 * side by side. When the access token lapses, all three arrive here holding the same refresh
 * token. If each refreshed on its own, the second would replay a token the first had just spent
 * and the user would be signed out for opening two tabs.
 *
 * So refreshes are keyed by the token being spent. Callers that arrive while one is in flight wait
 * for it; callers that arrive just after — their cookies not yet updated by the response still on
 * its way to the browser — are handed the same result for a short while.
 *
 * This is process memory, which is right for this deployment: one clinic is one instance, and
 * there is one Next.js server in it. Two servers behind a balancer would need this shared.
 */

interface Settled {
  tokens: SessionTokens | null;
  at: number;
}

/** Long enough to cover responses still in flight; short enough that a copied cookie gains nothing. */
const REUSE_WINDOW_MS = 20_000;

const inFlight = new Map<string, Promise<SessionTokens | null>>();
const settled = new Map<string, Settled>();

function forget(now: number) {
  for (const [token, entry] of settled) {
    if (now - entry.at > REUSE_WINDOW_MS) {
      settled.delete(token);
    }
  }
}

/** New tokens for the session this refresh token belongs to, or `null` when the session is over. */
export function refreshSession(refreshToken: string): Promise<SessionTokens | null> {
  const now = Date.now();
  forget(now);

  const recent = settled.get(refreshToken);
  if (recent) {
    return Promise.resolve(recent.tokens);
  }

  const running = inFlight.get(refreshToken);
  if (running) {
    return running;
  }

  const started = postRefresh(refreshToken)
    .then((tokens) => {
      settled.set(refreshToken, { tokens, at: Date.now() });
      return tokens;
    })
    .finally(() => {
      inFlight.delete(refreshToken);
    });

  inFlight.set(refreshToken, started);

  return started;
}
