import { clearedCookieWrites, sessionCookieWrites } from '@/lib/auth/session-cookies';
import { clearCookies, setCookie } from '../runtime/cookies';
import { sessionOfToken, sessions, startSession } from './core/auth';
import { DEMO_ACCOUNTS, type DemoAccount } from './core/demo-accounts';
import { getCookie } from '../runtime/cookies';
import { plantSeed } from '.';
import { clearOutbox } from './core/outbox';

/**
 * What the demo bar does in one click that a visitor would otherwise do through the sign-in form:
 * become one of the demo accounts, sign out, or start the demo again from fresh data.
 */

export type DemoRole = DemoAccount['key'] | 'none';

function writeSession(userId: string) {
  const session = startSession(userId);
  for (const { name, value, options } of sessionCookieWrites({
    accessToken: session.access_token,
    accessTokenExpiresAt: session.expires_at,
    refreshToken: session.refresh_token,
  })) {
    setCookie(name, value, options);
  }
}

export function signOutDemo() {
  const token = getCookie('hp_at');
  const session = token ? sessionOfToken(token) : undefined;
  if (session) sessions().update(session.id, { revoked: true });
  for (const { name, value, options } of clearedCookieWrites()) setCookie(name, value, options);
}

export function signInAs(role: DemoRole) {
  signOutDemo();
  if (role !== 'none') writeSession(DEMO_ACCOUNTS[role].user_id);
}

/** Which demo account this browser is signed in as, if any. */
export function currentRole(): DemoRole {
  const token = getCookie('hp_at');
  const session = token ? sessionOfToken(token) : undefined;
  if (!session || session.revoked) return 'none';
  const account = Object.values(DEMO_ACCOUNTS).find((entry) => entry.user_id === session.user_id);
  return account?.key ?? 'none';
}

export function resetDemo() {
  clearCookies();
  plantSeed();
  clearOutbox();
}

declare global {
  interface Window {
    __demo?: { signInAs: typeof signInAs; resetDemo: typeof resetDemo; currentRole: typeof currentRole };
  }
}

window.__demo = { signInAs, resetDemo, currentRole };
