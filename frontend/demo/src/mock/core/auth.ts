import { nowIso, table, uuid, type Row } from './db';
import type { Auth } from './http';

/**
 * Accounts and sessions. An account (`users`) holds the sign-in details; what it may do comes from
 * its memberships, read fresh on every call from the `staff_members` and `customers` tables, so a
 * role changed in settings applies to the next request as it does in the product.
 */

export interface User extends Row {
  display_name: string;
  email?: string | null;
  /** Digits only, Thai format: 0812345678. */
  phone?: string | null;
  password: string;
  status: 'active' | 'deactivated';
  two_factor: { enabled: boolean; method: string; recovery_codes_left?: number };
  language?: 'th' | 'en';
  notifications?: { email: boolean; sms: boolean; line: boolean };
  version: number;
  created_at: string;
}

export interface Session extends Row {
  user_id: string;
  access_token: string;
  refresh_token: string;
  user_agent: string;
  created_at: string;
  last_used_at: string;
  expires_at: string;
  revoked: boolean;
}

/** A waiting-room screen paired with its own token (docs/contract-extensions.md § 30). */
export interface DisplayToken extends Row {
  token: string;
  branch_id: string;
  screen_id: string;
  revoked: boolean;
}

export const users = () => table<User>('users');
export const sessions = () => table<Session>('sessions');
export const displayTokens = () => table<DisplayToken>('display_tokens');

/** Sessions last the refresh token's thirty days; the access token is given the same, the demo has no clock to race. */
const SESSION_DAYS = 30;

export function findUserByIdentifier(identifier: string): User | undefined {
  const value = identifier.trim().toLowerCase();
  const digits = value.replace(/\D/g, '').replace(/^66/, '0');
  return users().find(
    (user) => user.email?.toLowerCase() === value || (digits.length >= 9 && user.phone?.replace(/\D/g, '') === digits),
  );
}

/** The staff membership of an account, if it holds an active one. */
export function staffOf(userId: string): Row | undefined {
  return table('staff_members').find((member) => member.user_id === userId && member.status === 'active');
}

export function customerOf(userId: string): Row | undefined {
  return table('customers').find((customer) => customer.user_id === userId && !customer.deleted_at);
}

export function scopesOfRoles(roleNames: string[]): string[] {
  const roles = table('roles').where((role) => roleNames.includes(role.name));
  return [...new Set(roles.flatMap((role) => role.scopes as string[]))];
}

/** `GET /me`'s memberships. */
export function membershipsOf(userId: string) {
  const memberships: Array<Record<string, unknown>> = [];
  const staff = staffOf(userId);
  if (staff) {
    memberships.push({
      type: 'staff',
      staff_id: staff.id,
      staff_type: staff.staff_type,
      ...(staff.license_no ? { license_no: staff.license_no } : {}),
      roles: staff.roles,
      branches: staff.branches,
      scopes: scopesOfRoles(staff.roles),
    });
  }
  const customer = customerOf(userId);
  if (customer) {
    memberships.push({ type: 'customer', customer_id: customer.id, roles: ['customer'], branches: [], scopes: ['portal'] });
  }
  return memberships;
}

export function startSession(userId: string): Session {
  const expires = new Date(Date.now() + SESSION_DAYS * 86_400_000).toISOString();
  return sessions().insert({
    user_id: userId,
    access_token: `demo_at_${uuid()}`,
    refresh_token: `demo_rt_${uuid()}`,
    user_agent: navigator.userAgent,
    created_at: nowIso(),
    last_used_at: nowIso(),
    expires_at: expires,
    revoked: false,
  });
}

/** The answer of a finished sign-in, with the refresh token in `Set-Cookie` as the backend sends it. */
export function tokenPayload(session: Session) {
  const staff = staffOf(session.user_id);
  const customer = customerOf(session.user_id);
  return {
    body: {
      access_token: session.access_token,
      token_type: 'Bearer',
      expires_at: session.expires_at,
      two_factor_satisfied: true,
      capabilities: { staff_id: staff?.id ?? null, customer_id: customer?.id ?? null },
    },
    setCookies: [`hp_refresh=${session.refresh_token}; Path=/; HttpOnly; Secure; SameSite=Lax; Max-Age=${SESSION_DAYS * 86_400}`],
  };
}

export function resolveToken(token: string): Auth | null {
  const display = displayTokens().find((row) => row.token === token && !row.revoked);
  if (display) {
    return { token, userId: '', scopes: ['display'], branches: [display.branch_id], display: { branchId: display.branch_id, screenId: display.screen_id } };
  }

  const session = sessions().find((row) => row.access_token === token);
  if (!session || session.revoked || session.expires_at <= nowIso()) return null;
  const user = users().get(session.user_id);
  if (!user || user.status !== 'active') return null;

  const staff = staffOf(user.id);
  const customer = customerOf(user.id);
  return {
    token,
    userId: user.id,
    staffId: staff?.id,
    customerId: customer?.id,
    scopes: staff ? scopesOfRoles(staff.roles) : customer ? ['portal'] : [],
    branches: staff ? (staff.branches as string[]) : [],
  };
}

export function sessionOfToken(token: string): Session | undefined {
  return sessions().find((row) => row.access_token === token);
}
