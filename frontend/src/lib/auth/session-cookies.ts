/**
 * The two cookies this app keeps, and nothing that needs `next/headers` — so the middleware, which
 * runs before any of that exists, can import it too.
 *
 * Both tokens are httpOnly. The browser holds them and cannot read them: script on the page never sees a
 * token, which is the property the backend's own refresh cookie is designed around
 * (docs/authentication.md § 3). The Next.js server is the only thing that reads them, and it is
 * what attaches the bearer token to each backend call.
 */

/** The backend access token. Lives as long as the token does, less a margin. */
export const ACCESS_COOKIE = 'hp_at';

/** The backend refresh token — the value of its own `hp_refresh` cookie, held on this origin. */
export const REFRESH_COOKIE = 'hp_rt';

/** The backend's name for the refresh cookie, used when forwarding it. */
export const BACKEND_REFRESH_COOKIE = 'hp_refresh';

/**
 * "Someone is signed in on this browser", readable by script. Holds `1` and nothing else: not a
 * credential, and grants nothing. It exists so the public pages, which are static and never read
 * the session, can swap "เข้าสู่ระบบ" for "บัญชีของฉัน" in the browser. If it is stale, the link
 * leads to the sign-in page, which looks at the real session.
 */
export const SIGNED_IN_HINT_COOKIE = 'hp_signed_in';

/** The refresh token's lifetime per the contract: 30 days, sliding. */
const REFRESH_MAX_AGE_SECONDS = 60 * 60 * 24 * 30;

/**
 * The access cookie expires this long before the token inside it does, so a request never sets
 * off carrying a token that lapses in flight. When the cookie is gone the middleware refreshes.
 */
const ACCESS_EXPIRY_MARGIN_SECONDS = 30;

export interface SessionTokens {
  accessToken: string;
  /** ISO 8601, from the backend's `expires_at`. */
  accessTokenExpiresAt: string;
  refreshToken: string;
}

export interface CookieWrite {
  name: string;
  value: string;
  options: {
    /** True for both tokens. False only for the signed-in hint, which holds no secret. */
    httpOnly: boolean;
    secure: boolean;
    sameSite: 'lax';
    path: '/';
    maxAge: number;
  };
}

const baseOptions = () => ({
  httpOnly: true as boolean,
  secure: process.env.NODE_ENV === 'production',

  // Lax, not strict: a strict cookie is withheld on the first navigation from another site, so
  // following a link to the console from an email would land on the sign-in page while signed
  // in. Mutations here are server actions, which Next checks for same-origin on its own.
  sameSite: 'lax' as const,
  path: '/' as const,
});

/** The cookies that represent a signed-in session. */
export function sessionCookieWrites(tokens: SessionTokens): CookieWrite[] {
  const secondsLeft = Math.floor((Date.parse(tokens.accessTokenExpiresAt) - Date.now()) / 1000);

  return [
    {
      name: ACCESS_COOKIE,
      value: tokens.accessToken,
      options: { ...baseOptions(), maxAge: Math.max(1, secondsLeft - ACCESS_EXPIRY_MARGIN_SECONDS) },
    },
    {
      name: REFRESH_COOKIE,
      value: tokens.refreshToken,
      options: { ...baseOptions(), maxAge: REFRESH_MAX_AGE_SECONDS },
    },
    {
      name: SIGNED_IN_HINT_COOKIE,
      value: '1',
      options: { ...baseOptions(), httpOnly: false, maxAge: REFRESH_MAX_AGE_SECONDS },
    },
  ];
}

/** The cookies that represent having signed out. */
export function clearedCookieWrites(): CookieWrite[] {
  return [ACCESS_COOKIE, REFRESH_COOKIE, SIGNED_IN_HINT_COOKIE].map((name) => ({
    name,
    value: '',
    options: { ...baseOptions(), httpOnly: name !== SIGNED_IN_HINT_COOKIE, maxAge: 0 },
  }));
}
