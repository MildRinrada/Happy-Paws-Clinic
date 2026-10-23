import {
  customerOf,
  findUserByIdentifier,
  membershipsOf,
  sessionOfToken,
  sessions,
  startSession,
  staffOf,
  tokenPayload,
  users,
  type User,
} from '../../core/auth';
import { DEMO_TOTP } from '../../core/demo-accounts';
import { minutesFromNow, nowIso, table, uuid } from '../../core/db';
import { accepted, checkVersion, fail, invalid, noContent, Reply, requireAuth, route } from '../../core/http';
import { checkOtp, digitsOf, issueOtp } from '../../core/otp';
import { send } from '../../core/outbox';

/** Sign-in, second factor, sessions, registration, password reset and the account's own `/me`. */

const challenges = () => table<{ id: string; user_id: string; expires_at: string }>('demo_2fa_challenges');
const resetTokens = () => table<{ id: string; user_id: string; expires_at: string; used: boolean }>('demo_reset_tokens');

function signedIn(userId: string): Reply {
  const { body, setCookies } = tokenPayload(startSession(userId));
  return new Reply(200, body, {}, setCookies);
}

function wrongCredentials(): never {
  fail(401, 'invalid_credentials', 'อีเมล/เบอร์โทร หรือรหัสผ่านไม่ถูกต้อง', { message_en: 'Invalid credentials.' });
}

function login(identifier: unknown, password: unknown): Reply {
  const user = typeof identifier === 'string' ? findUserByIdentifier(identifier) : undefined;
  if (!user || user.password !== password) wrongCredentials();
  if (user.status !== 'active') fail(403, 'account_disabled', 'บัญชีนี้ถูกปิดการใช้งาน');

  if (user.two_factor.enabled) {
    const challenge = challenges().insert({ id: `chl_${uuid()}`, user_id: user.id, expires_at: minutesFromNow(5) });
    return new Reply(200, { two_factor_required: true, challenge: challenge.id, methods: ['totp'] });
  }
  return signedIn(user.id);
}

route('POST', '/auth/login', ({ body }) => login(body?.identifier, body?.password));
route('POST', '/auth/staff/login', ({ body }) => login(body?.email ?? body?.identifier, body?.password));

route('POST', '/auth/2fa/verify', ({ body }) => {
  const challenge = challenges().get(body?.challenge);
  if (!challenge || challenge.expires_at <= nowIso()) wrongCredentials();
  if (body?.code !== DEMO_TOTP) fail(401, 'invalid_code', 'รหัสไม่ถูกต้อง');
  challenges().remove(challenge.id);
  return signedIn(challenge.user_id);
});

route('POST', '/auth/refresh', ({ cookies }) => {
  const session = sessions().find((row) => row.refresh_token === cookies.hp_refresh && !row.revoked);
  if (!session) fail(401, 'unauthenticated', 'เซสชันหมดอายุ');
  sessions().update(session.id, { revoked: true });
  return signedIn(session.user_id);
});

route('POST', '/auth/logout', ({ cookies, auth }) => {
  const session =
    sessions().find((row) => row.refresh_token === cookies.hp_refresh) ?? (auth ? sessionOfToken(auth.token) : undefined);
  if (session) sessions().update(session.id, { revoked: true });
  return noContent();
});

route('POST', '/auth/otp/request', ({ body }) => {
  const phone = digitsOf(String(body?.phone ?? ''));
  if (!/^0\d{8,9}$/.test(phone)) invalid('phone', 'เบอร์โทรศัพท์ไม่ถูกต้อง');
  return issueOtp(phone, String(body?.purpose ?? 'register'));
});

route('POST', '/auth/register', ({ body }) => {
  const phone = digitsOf(String(body?.phone ?? ''));
  if (!checkOtp(phone, String(body?.code ?? ''), 'register')) invalid('code', 'รหัสยืนยันไม่ถูกต้องหรือหมดอายุ');
  if (findUserByIdentifier(phone) || (body?.email && findUserByIdentifier(body.email))) {
    fail(409, 'contact_taken', 'เบอร์โทรหรืออีเมลนี้มีบัญชีอยู่แล้ว');
  }
  const user = users().insert({
    display_name: String(body?.display_name ?? '').trim() || 'ลูกค้าใหม่',
    email: body?.email || null,
    phone,
    password: String(body?.password ?? ''),
    status: 'active',
    two_factor: { enabled: false, method: 'none' },
    language: 'th',
    notifications: { email: true, sms: true, line: false },
    version: 1,
    created_at: nowIso(),
  });
  linkOrCreateCustomer(user);
  return signedIn(user.id);
});

/** A customer the counter already registered by phone gets the new account; anyone else gets a new record. */
export function linkOrCreateCustomer(user: User) {
  const customers = table('customers');
  const existing = customers.find((row) => !row.user_id && digitsOf(row.phone ?? '') === user.phone && row.is_active !== false);
  if (existing) return customers.update(existing.id, { user_id: user.id, has_portal_account: true });
  return customers.insert({
    version: 1,
    display_name: user.display_name,
    phone: user.phone ?? '',
    ...(user.email ? { email: user.email } : {}),
    has_portal_account: true,
    is_active: true,
    created_at: nowIso(),
    user_id: user.id,
  });
}

route('POST', '/auth/password/reset', ({ body }) => {
  if (body?.token) {
    const token = resetTokens().get(body.token);
    if (!token || token.used || token.expires_at <= nowIso()) invalid('token', 'ลิงก์นี้ใช้ไม่ได้แล้ว โปรดขอลิงก์ใหม่');
    resetTokens().update(token.id, { used: true });
    users().update(token.user_id, { password: String(body.new_password ?? '') });
    for (const session of sessions().where((row) => row.user_id === token.user_id)) sessions().update(session.id, { revoked: true });
    return { status: 'reset' };
  }
  const user = findUserByIdentifier(String(body?.identifier ?? ''));
  if (user) {
    const token = resetTokens().insert({ id: `rst_${uuid()}`, user_id: user.id, expires_at: minutesFromNow(30), used: false });
    const link = `${location.origin}/reset-password#token=${token.id}`;
    const byEmail = String(body?.identifier).includes('@');
    send({
      channel: byEmail ? 'email' : 'sms',
      to: byEmail ? (user.email ?? '') : (user.phone ?? ''),
      subject: 'ตั้งรหัสผ่านใหม่',
      body: `กดลิงก์นี้เพื่อตั้งรหัสผ่านใหม่ (ใช้ได้ 30 นาที): ${link}`,
    });
  }
  return accepted({ status: 'link_sent' });
});

// ---- The signed-in account --------------------------------------------------------------------------

function me(user: User) {
  return {
    user_id: user.id,
    display_name: user.display_name,
    status: user.status,
    memberships: membershipsOf(user.id),
    two_factor: user.two_factor,
    email: user.email ?? null,
    phone: user.phone ?? null,
    language: user.language ?? 'th',
    notifications: user.notifications ?? { email: true, sms: true, line: false },
    version: user.version,
  };
}

function currentUser(ctx: Parameters<typeof requireAuth>[0]): User {
  const auth = requireAuth(ctx);
  const user = users().get(auth.userId);
  if (!user) fail(401, 'unauthenticated', 'กรุณาเข้าสู่ระบบ');
  return user;
}

route('GET', '/me', (ctx) => me(currentUser(ctx)));

route('PATCH', '/me', (ctx) => {
  const user = currentUser(ctx);
  checkVersion(ctx, user);
  const body = ctx.body ?? {};
  const contactChanged =
    (body.email !== undefined && body.email !== (user.email ?? '')) || (body.phone !== undefined && body.phone !== (user.phone ?? ''));
  if (contactChanged && body.current_password !== user.password) {
    fail(401, 'invalid_credentials', 'รหัสผ่านปัจจุบันไม่ถูกต้อง', { field: 'current_password' });
  }
  const next = users().update(user.id, {
    ...(body.display_name !== undefined ? { display_name: body.display_name } : {}),
    ...(body.email !== undefined ? { email: body.email || null } : {}),
    ...(body.phone !== undefined ? { phone: body.phone ? digitsOf(body.phone) : null } : {}),
    ...(body.language !== undefined ? { language: body.language } : {}),
    ...(body.notifications !== undefined ? { notifications: body.notifications } : {}),
  })!;
  return me(next);
});

route('GET', '/me/capabilities', (ctx) => {
  const user = currentUser(ctx);
  return { staff_id: staffOf(user.id)?.id ?? null, customer_id: customerOf(user.id)?.id ?? null, memberships: membershipsOf(user.id) };
});

route('POST', '/me/password', (ctx) => {
  const user = currentUser(ctx);
  if (ctx.body?.current_password !== user.password) fail(401, 'invalid_credentials', 'รหัสผ่านปัจจุบันไม่ถูกต้อง');
  users().update(user.id, { password: String(ctx.body?.new_password ?? '') });
  for (const session of sessions().where((row) => row.user_id === user.id && row.access_token !== ctx.auth!.token)) {
    sessions().update(session.id, { revoked: true });
  }
  return noContent();
});

const DEMO_SECRET = 'JBSWY3DPEHPK3PXP';

route('POST', '/me/2fa/enroll', (ctx) => {
  const user = currentUser(ctx);
  if (ctx.body?.code !== undefined) {
    if (ctx.body.code !== DEMO_TOTP) invalid('code', `รหัสไม่ถูกต้อง (ในเดโมใช้ ${DEMO_TOTP})`);
    users().update(user.id, { two_factor: { enabled: true, method: 'totp', recovery_codes_left: 10 } });
    return {
      status: 'enabled',
      recovery_codes: Array.from({ length: 10 }, (_, index) => `DEMO-${String(index + 1).padStart(2, '0')}-${Math.random().toString(36).slice(2, 6).toUpperCase()}`),
    };
  }
  if (ctx.body?.current_password !== user.password) fail(401, 'invalid_credentials', 'รหัสผ่านปัจจุบันไม่ถูกต้อง', { field: 'current_password' });
  return {
    status: 'confirmation_required',
    secret: DEMO_SECRET,
    otp_auth_uri: `otpauth://totp/Happy%20Paws:${encodeURIComponent(user.email ?? user.phone ?? 'demo')}?secret=${DEMO_SECRET}&issuer=Happy%20Paws`,
  };
});

route('GET', '/me/sessions', (ctx) => {
  const user = currentUser(ctx);
  return sessions()
    .where((row) => row.user_id === user.id && !row.revoked && row.expires_at > nowIso())
    .sort((a, b) => b.last_used_at.localeCompare(a.last_used_at))
    .map((row) => ({
      id: row.id,
      user_agent: row.user_agent,
      ip_address: '127.0.0.1',
      created_at: row.created_at,
      last_used_at: row.last_used_at,
      expires_at: row.expires_at,
      current: row.access_token === ctx.auth!.token,
    }));
});

route('DELETE', '/me/sessions/:id', (ctx) => {
  const user = currentUser(ctx);
  const session = sessions().get(ctx.params.id);
  if (!session || session.user_id !== user.id) fail(404, 'not_found', 'ไม่พบอุปกรณ์นี้');
  sessions().update(session.id, { revoked: true });
  return noContent();
});
