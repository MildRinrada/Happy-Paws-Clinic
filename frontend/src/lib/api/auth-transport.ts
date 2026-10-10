import { serverEnv } from '@/config/env';
import { BACKEND_REFRESH_COOKIE, type SessionTokens } from '@/lib/auth/session-cookies';
import { ApiError } from './api-error';
import { extractError } from './http-client';

/**
 * The backend calls that deal in the refresh cookie: sign in, second factor, refresh, sign
 * out. They are apart from the ordinary client because they need the one thing it does not
 * expose — the response's `Set-Cookie` — and because the refresh token must travel as a cookie,
 * not a body field (the backend reads it from nowhere else).
 *
 * Server-side only. Nothing here may be imported by a client component: it handles the refresh
 * token in the clear.
 */

interface TokenPayload {
  access_token: string;
  expires_at: string;
  two_factor_satisfied: boolean;
  /** "คืน capabilities ของบัญชีในคลินิกนี้": which memberships the new token carries. */
  capabilities?: { staff_id?: string | null; customer_id?: string | null };
}

interface ChallengePayload {
  two_factor_required: true;
  challenge: string;
  methods: string[];
}

/** A finished sign-in: the tokens, and which sides of the site the account may use (it decides where to go next). */
export interface SignedIn {
  tokens: SessionTokens;
  isStaff: boolean;
  isCustomer: boolean;
}

export type LoginOutcome =
  | ({ kind: 'signed_in' } & SignedIn)
  | { kind: 'second_factor'; challenge: string };

async function post(path: string, body: unknown, refreshToken?: string): Promise<Response> {
  const headers = new Headers({ Accept: 'application/json', 'Content-Type': 'application/json' });

  if (refreshToken) {
    headers.set('Cookie', `${BACKEND_REFRESH_COOKIE}=${refreshToken}`);
  }

  return fetch(serverEnv.apiBaseUrl().replace(/\/$/, '') + path, {
    method: 'POST',
    headers,
    body: JSON.stringify(body),
    cache: 'no-store',
  });
}

async function failure(response: Response): Promise<ApiError> {
  return new ApiError(response.status, extractError(await response.json().catch(() => null), response.status));
}

/** Pulls the rotated refresh token out of the backend's `Set-Cookie`. */
function refreshTokenFrom(response: Response): string | null {
  for (const cookie of response.headers.getSetCookie()) {
    const [pair] = cookie.split(';');
    const separator = pair?.indexOf('=') ?? -1;

    if (pair && separator > 0 && pair.slice(0, separator).trim() === BACKEND_REFRESH_COOKIE) {
      return decodeURIComponent(pair.slice(separator + 1).trim());
    }
  }
  return null;
}

async function signedInFrom(response: Response): Promise<SignedIn> {
  const payload = (await response.json()) as TokenPayload;
  const refreshToken = refreshTokenFrom(response);

  if (!refreshToken) {
    // The backend always sets it on a completed sign-in. Without it there is no session to keep.
    throw new Error('The API completed a sign-in without issuing a refresh cookie.');
  }

  return {
    tokens: {
      accessToken: payload.access_token,
      accessTokenExpiresAt: payload.expires_at,
      refreshToken,
    },
    isStaff: Boolean(payload.capabilities?.staff_id),
    isCustomer: Boolean(payload.capabilities?.customer_id),
  };
}

async function tokensFrom(response: Response): Promise<SessionTokens> {
  return (await signedInFrom(response)).tokens;
}

/**
 * `POST /auth/login`, "ทั้งสองฝั่ง": the one sign-in for customers and staff alike. The identifier
 * is an email or a phone number. An account with a second factor (every member of staff who
 * touches medical records) gets a challenge to answer with a code, exactly as on the staff route.
 */
export async function postLogin(identifier: string, password: string): Promise<LoginOutcome> {
  const response = await post('/auth/login', { identifier, password });

  if (!response.ok) {
    throw await failure(response);
  }

  // Cloned because the body can only be read once and which shape it is decides who reads it.
  const peek = (await response.clone().json()) as Partial<ChallengePayload>;

  return peek.two_factor_required && peek.challenge
    ? { kind: 'second_factor', challenge: peek.challenge }
    : { kind: 'signed_in', ...(await signedInFrom(response)) };
}

/** What `POST /auth/register` takes, on the wire. */
export interface RegisterBody {
  phone: string;
  code: string;
  display_name: string;
  password: string;
  email?: string;
}

/**
 * `POST /auth/register` — opens a customer's account with a proven phone number and signs them
 * straight in, which is why it lives here: the refresh token comes back only in `Set-Cookie`.
 */
export async function postRegister(body: RegisterBody): Promise<SignedIn> {
  const response = await post('/auth/register', body);

  if (!response.ok) {
    throw await failure(response);
  }

  return signedInFrom(response);
}

/** What `POST /booking/guest/verify` takes, on the wire (docs/contract-extensions.md § 27). */
export interface GuestVerifyBody {
  hold_id: string;
  phone: string;
  code: string;
  display_name: string;
  accept_privacy_notice: boolean;
  /** Still accepted and ignored by the backend, which stamps its published notice (§ 35). Not sent. */
  policy_version?: string;
  marketing_consent: boolean;
  /** A friend's referral code (docs/contract-extensions.md § 39). Optional; never changes the answer. */
  referral_code?: string;
}

/**
 * `POST /booking/guest/verify` — proves the number with its code and signs the visitor in as a
 * customer, refresh token in `Set-Cookie` as at sign-in, which is why it lives here. The rest of
 * the answer (their pets, the hold, whether the account is new) goes back to the caller as it came.
 */
export async function postGuestBookingVerify<T>(body: GuestVerifyBody): Promise<{ signedIn: SignedIn; payload: T }> {
  const response = await post('/booking/guest/verify', body);

  if (!response.ok) {
    throw await failure(response);
  }

  const payload = (await response.clone().json()) as T;

  return { signedIn: await signedInFrom(response), payload };
}

/** `POST /auth/2fa/verify` — "คืน access token จริง". */
export async function postVerifyTwoFactor(challenge: string, code: string): Promise<SignedIn> {
  const response = await post('/auth/2fa/verify', { challenge, code });

  if (!response.ok) {
    throw await failure(response);
  }

  return signedInFrom(response);
}

/**
 * `POST /auth/refresh` — rotates the refresh token. Answers `null` when the session is over
 * (expired, revoked, or the token was already spent), which is a normal ending and not an error.
 */
export async function postRefresh(refreshToken: string): Promise<SessionTokens | null> {
  const response = await post('/auth/refresh', {}, refreshToken);

  if (response.status === 401) {
    return null;
  }
  if (!response.ok) {
    throw await failure(response);
  }

  return tokensFrom(response);
}

/** `POST /auth/logout` — revokes this session on the backend. Best effort: the cookies go either way. */
export async function postLogout(refreshToken: string): Promise<void> {
  await post('/auth/logout', { all_devices: false }, refreshToken).catch(() => undefined);
}
