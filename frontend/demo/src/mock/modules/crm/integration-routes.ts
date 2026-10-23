import { nowIso, singleton, table, type Row } from '../../core/db';
import { allows, checkVersion, created, fail, file, invalid, matches, noContent, notFound, page, requireStaff, route } from '../../core/http';
import { lineSettings, lineSettingsView } from './notify';
import { apiKeys, calendarLinks, lineFollowers, webhookDeliveries, webhooks } from './store';
import { TEMPLATES } from './templates';

/**
 * Integrations: the LINE Official Account and its followers, the payment and outside-lab
 * providers, the calendar feed link, webhooks with their delivery log, and API keys. Secrets go in
 * once and never come back out: answers say only whether one is stored.
 */

const random = (bytes: number) =>
  btoa(String.fromCharCode(...crypto.getRandomValues(new Uint8Array(bytes))))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');

const hex = (bytes: number) => [...crypto.getRandomValues(new Uint8Array(bytes))].map((b) => b.toString(16).padStart(2, '0')).join('');

/** A credential field of a `PUT`: absent keeps it, an empty string clears it, anything else sets it. */
function secret(current: string | undefined, sent: unknown): string | undefined {
  if (sent === undefined) return current;
  const text = String(sent).trim();
  return text ? text : undefined;
}

const isEmpty = (body: unknown) => !body || (typeof body === 'object' && Object.keys(body as object).length === 0);

// ---- LINE -------------------------------------------------------------------------------------------

route('PUT', '/staff/integrations/line', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  if (isEmpty(ctx.body)) return lineSettingsView();
  const current = lineSettings.get();
  checkVersion(ctx, { version: current.version });
  const b = ctx.body;
  if (b.channel_id !== undefined && b.channel_id !== '' && !/^\d{10}$/.test(String(b.channel_id))) invalid('channel_id', 'Channel ID เป็นตัวเลข 10 หลัก');
  if (b.broadcast_monthly_limit !== undefined && !(Number.isInteger(b.broadcast_monthly_limit) && b.broadcast_monthly_limit >= 0 && b.broadcast_monthly_limit <= 100_000)) {
    invalid('broadcast_monthly_limit', 'โควตาต้องเป็นจำนวนเต็ม 0 ถึง 100,000');
  }
  const types = { ...current.notification_types };
  for (const [key, value] of Object.entries(b.notification_types ?? {})) {
    if (!TEMPLATES.some((t) => t.key === key)) invalid('notification_types', `ไม่รู้จักชนิดข้อความ ${key}`);
    types[key] = Boolean(value);
  }
  lineSettings.set({
    channel_id: b.channel_id === undefined ? current.channel_id : String(b.channel_id) || undefined,
    channel_secret: secret(current.channel_secret, b.channel_secret),
    channel_access_token: secret(current.channel_access_token, b.channel_access_token),
    enabled: typeof b.enabled === 'boolean' ? b.enabled : current.enabled,
    notification_types: types,
    broadcast_monthly_limit: b.broadcast_monthly_limit ?? current.broadcast_monthly_limit,
    version: current.version + 1,
    updated_at: nowIso(),
  });
  return lineSettingsView();
});

route('GET', '/staff/integrations/line/followers', (ctx) => {
  requireStaff(ctx, 'customer:write');
  const unlinkedOnly = ctx.query.get('unlinked') !== 'false';
  const since = new Date(Date.now() - 14 * 86_400_000).toISOString();
  const linkedTo = new Map<string, string>();
  for (const c of table('customers').all()) if (c.line?.line_user_id && !c.merged_into_customer_id) linkedTo.set(c.line.line_user_id, c.id);
  const rows = lineFollowers()
    .where((f) => !f.unfollowed_at && f.last_seen_at >= since)
    .filter((f) => !unlinkedOnly || !linkedTo.has(f.line_user_id))
    .filter((f) => matches(ctx.query.get('q'), f.display_name))
    .sort((a, b) => b.last_seen_at.localeCompare(a.last_seen_at))
    .map((f) => ({
      line_user_id: f.line_user_id,
      ...(f.display_name ? { display_name: f.display_name } : {}),
      ...(f.picture_url ? { picture_url: f.picture_url } : {}),
      followed_at: f.followed_at,
      last_seen_at: f.last_seen_at,
      ...(!unlinkedOnly && linkedTo.has(f.line_user_id) ? { linked_customer_id: linkedTo.get(f.line_user_id) } : {}),
    }));
  return page(rows, ctx.query, 20);
});

// ---- Payment and lab providers ----------------------------------------------------------------------

const payment = singleton<Row & Record<string, any>>('payment_settings', () => ({ id: 'singleton', version: 0, mode: 'test', enabled: false }));
const lab = singleton<Row & Record<string, any>>('lab_settings', () => ({ id: 'singleton', version: 0, enabled: false }));

const PROVIDER = /^[a-z0-9_]{2,40}$/;

function paymentView() {
  const s = payment.get();
  return {
    version: s.version,
    ...(s.provider ? { provider: s.provider } : {}),
    mode: s.mode,
    enabled: s.enabled,
    ...(s.public_key ? { public_key: s.public_key } : {}),
    has_secret_key: Boolean(s.secret_key),
    has_webhook_secret: Boolean(s.webhook_secret),
    connected: Boolean(s.enabled && s.provider && s.public_key && s.secret_key),
    ...(s.updated_at ? { updated_at: s.updated_at } : {}),
  };
}

function labView() {
  const s = lab.get();
  return {
    version: s.version,
    ...(s.provider ? { provider: s.provider } : {}),
    enabled: s.enabled,
    ...(s.account_id ? { account_id: s.account_id } : {}),
    ...(s.base_url ? { base_url: s.base_url } : {}),
    has_api_key: Boolean(s.api_key),
    connected: Boolean(s.enabled && s.provider && s.api_key),
    ...(s.updated_at ? { updated_at: s.updated_at } : {}),
  };
}

route('PUT', '/staff/integrations/payment', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  if (isEmpty(ctx.body)) return paymentView();
  const current = payment.get();
  checkVersion(ctx, { version: current.version });
  const b = ctx.body;
  if (b.provider !== undefined && b.provider !== '' && !PROVIDER.test(b.provider)) invalid('provider', 'ชื่อผู้ให้บริการใช้ตัวอักษรอังกฤษพิมพ์เล็ก ตัวเลข หรือ _');
  if (b.mode !== undefined && b.mode !== 'test' && b.mode !== 'live') invalid('mode', 'โหมดต้องเป็น test หรือ live');
  payment.set({
    provider: b.provider === undefined ? current.provider : b.provider || undefined,
    mode: b.mode ?? current.mode,
    enabled: typeof b.enabled === 'boolean' ? b.enabled : current.enabled,
    public_key: b.public_key === undefined ? current.public_key : b.public_key || undefined,
    secret_key: secret(current.secret_key, b.secret_key),
    webhook_secret: secret(current.webhook_secret, b.webhook_secret),
    version: current.version + 1,
    updated_at: nowIso(),
  });
  return paymentView();
});

route('PUT', '/staff/integrations/lab', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  if (isEmpty(ctx.body)) return labView();
  const current = lab.get();
  checkVersion(ctx, { version: current.version });
  const b = ctx.body;
  if (b.provider !== undefined && b.provider !== '' && !PROVIDER.test(b.provider)) invalid('provider', 'ชื่อแล็บใช้ตัวอักษรอังกฤษพิมพ์เล็ก ตัวเลข หรือ _');
  if (b.base_url && !/^https:\/\//.test(b.base_url)) invalid('base_url', 'ที่อยู่ต้องขึ้นต้นด้วย https://');
  lab.set({
    provider: b.provider === undefined ? current.provider : b.provider || undefined,
    enabled: typeof b.enabled === 'boolean' ? b.enabled : current.enabled,
    account_id: b.account_id === undefined ? current.account_id : b.account_id || undefined,
    base_url: b.base_url === undefined ? current.base_url : b.base_url || undefined,
    api_key: secret(current.api_key, b.api_key),
    version: current.version + 1,
    updated_at: nowIso(),
  });
  return labView();
});

// ---- Calendar feed ----------------------------------------------------------------------------------

function calendarLinkOf(staffId: string, rotate = false): Row {
  const existing = calendarLinks().find((l) => l.staff_id === staffId && !l.revoked_at);
  if (existing && !rotate) return existing;
  if (existing) calendarLinks().update(existing.id, { revoked_at: nowIso() });
  return calendarLinks().insert({ staff_id: staffId, token: `cal_${random(24)}`, created_at: nowIso() });
}

function calendarView(link: Row, canViewBranch: boolean) {
  return {
    path: `/staff/integrations/calendar/ics?token=${link.token}`,
    token: link.token,
    can_view_branch: canViewBranch,
    created_at: link.created_at,
    ...(link.last_used_at ? { last_used_at: link.last_used_at } : {}),
  };
}

const icsDate = (iso: string) => iso.replace(/[-:]/g, '').replace(/\.\d{3}/, '');

route('GET', '/staff/integrations/calendar/ics', (ctx) => {
  const token = ctx.query.get('token');
  if (token) {
    // A calendar app fetching the feed with the token in the link: the appointments as iCalendar.
    const link = calendarLinks().find((l) => l.token === token && !l.revoked_at);
    if (!link) fail(401, 'unauthenticated', 'ลิงก์ปฏิทินนี้ใช้ไม่ได้แล้ว');
    calendarLinks().update(link.id, { last_used_at: nowIso() });
    const branchView = ctx.query.get('view') === 'branch';
    const events = table('appointments')
      .where((a) => (branchView || a.staff_id === link.staff_id) && !['cancelled', 'draft'].includes(a.status))
      .map((a) => {
        const service = table('services').get(a.service_id)?.name ?? 'นัดหมาย';
        const petName = table('pets').get(a.pet_id)?.name ?? '';
        return ['BEGIN:VEVENT', `UID:${a.id}@happypaws`, `DTSTART:${icsDate(a.starts_at)}`, `DTEND:${icsDate(a.ends_at)}`, `SUMMARY:${service} · ${petName}`, 'END:VEVENT'].join('\r\n');
      });
    const body = ['BEGIN:VCALENDAR', 'VERSION:2.0', 'PRODID:-//Happy Paws Clinic//TH', 'X-WR-CALNAME:Happy Paws Clinic', 'X-PUBLISHED-TTL:PT30M', ...events, 'END:VCALENDAR'].join('\r\n');
    return file(body, 'text/calendar', 'happypaws.ics');
  }
  const auth = requireStaff(ctx);
  return calendarView(calendarLinkOf(auth.staffId), allows(auth.scopes, 'appointment:read'));
});

route('POST', '/staff/integrations/calendar/token', (ctx) => {
  const auth = requireStaff(ctx);
  return calendarView(calendarLinkOf(auth.staffId, true), allows(auth.scopes, 'appointment:read'));
});

// ---- Webhooks ---------------------------------------------------------------------------------------

export const WEBHOOK_EVENTS = [
  'appointment.created',
  'appointment.rescheduled',
  'appointment.cancelled',
  'appointment.no_show',
  'appointment.checked_in',
  'queue.ticket.called',
  'consultation.closed',
  'record.signed',
  'lab.result.ready',
  'prescription.dispensed',
  'invoice.issued',
  'payment.succeeded',
  'payment.refunded',
  'inventory.low_stock',
  'customer.created',
  'pet.created',
];

const SIGNING = { algorithm: 'HMAC-SHA256', header: 'X-Signature', timestamp_header: 'X-Webhook-Timestamp', signed_payload: '<timestamp>.<body>' };

export function webhookView(row: Row) {
  return {
    id: row.id,
    url: row.url,
    events: row.events,
    ...(row.description ? { description: row.description } : {}),
    active: row.active,
    version: row.version,
    created_by_staff_id: row.created_by_staff_id,
    created_at: row.created_at,
    ...(row.updated_at ? { updated_at: row.updated_at } : {}),
  };
}

function checkWebhookInput(b: Record<string, any>, partial: boolean) {
  if (!partial || b.url !== undefined) {
    let url: URL | undefined;
    try {
      url = new URL(String(b.url ?? ''));
    } catch {
      url = undefined;
    }
    if (!url || url.protocol !== 'https:') invalid('url', 'ที่อยู่ต้องเป็น https:// ที่ถูกต้อง');
    if (/^(localhost|127\.|10\.|192\.168\.)/.test(url.hostname)) invalid('url', 'ส่งไปที่อยู่ภายในเครือข่ายไม่ได้');
  }
  if (!partial || b.events !== undefined) {
    if (!Array.isArray(b.events) || b.events.length === 0) invalid('events', 'เลือกเหตุการณ์อย่างน้อยหนึ่งอย่าง');
    const unknown = b.events.find((e: string) => !WEBHOOK_EVENTS.includes(e));
    if (unknown) invalid('events', `ไม่รู้จักเหตุการณ์ ${unknown}`);
  }
  if (b.description !== undefined && String(b.description).length > 200) invalid('description', 'คำอธิบายยาวเกิน 200 ตัวอักษร');
}

function liveWebhook(id: string): Row {
  const row = webhooks().get(id);
  if (!row || row.deleted_at) notFound('webhook');
  return row;
}

route('GET', '/staff/webhooks', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  return { data: webhooks().where((w) => !w.deleted_at).sort((a, b) => b.created_at.localeCompare(a.created_at)).map(webhookView) };
});

route('POST', '/staff/webhooks', (ctx) => {
  const auth = requireStaff(ctx, 'integration:manage');
  const b = ctx.body ?? {};
  checkWebhookInput(b, false);
  const secretValue = `whsec_${random(32)}`;
  const row = webhooks().insert({
    url: String(b.url),
    events: [...new Set(b.events as string[])],
    ...(b.description ? { description: String(b.description) } : {}),
    active: true,
    version: 1,
    secret: secretValue,
    created_by_staff_id: auth.staffId,
    created_at: nowIso(),
  });
  return created({ id: row.id, url: row.url, events: row.events, ...(row.description ? { description: row.description } : {}), active: true, secret: secretValue, signing: SIGNING, created_at: row.created_at });
});

route('PATCH', '/staff/webhooks/:id', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  const row = liveWebhook(ctx.params.id!);
  checkVersion(ctx, { version: row.version });
  const b = ctx.body ?? {};
  checkWebhookInput(b, true);
  const patch: Record<string, unknown> = { updated_at: nowIso() };
  if (b.url !== undefined) patch.url = b.url;
  if (b.events !== undefined) patch.events = [...new Set(b.events as string[])];
  if (b.description !== undefined) patch.description = String(b.description).trim() || undefined;
  if (typeof b.active === 'boolean') patch.active = b.active;
  return webhookView(webhooks().update(row.id, patch)!);
});

route('DELETE', '/staff/webhooks/:id', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  const row = liveWebhook(ctx.params.id!);
  checkVersion(ctx, { version: row.version });
  webhooks().update(row.id, { deleted_at: nowIso(), active: false });
  return noContent();
});

route('POST', '/staff/webhooks/:id/rotate-secret', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  const row = liveWebhook(ctx.params.id!);
  checkVersion(ctx, { version: row.version });
  const secretValue = `whsec_${random(32)}`;
  const next = webhooks().update(row.id, { secret: secretValue })!;
  return { id: row.id, secret: secretValue, signing: SIGNING, version: next.version, rotated_at: nowIso() };
});

export function deliveryView(row: Row) {
  const { webhook_id: _w, payload: _p, ...rest } = row;
  return rest;
}

route('GET', '/staff/webhooks/:id/deliveries', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  const hook = webhooks().get(ctx.params.id!);
  if (!hook) notFound('webhook');
  const rows = webhookDeliveries()
    .where((d) => d.webhook_id === hook.id)
    .sort((a, b) => b.created_at.localeCompare(a.created_at))
    .map(deliveryView);
  return page(rows, ctx.query);
});

/**
 * Finishes an attempt a moment later, the way the delivery worker would: an endpoint that answers
 * 2xx succeeds, one that failed before fails again with the same answer.
 */
function attempt(deliveryId: string, delayMs: number) {
  setTimeout(() => {
    const row = webhookDeliveries().get(deliveryId);
    if (!row || row.status !== 'pending') return;
    const hook = webhooks().get(row.webhook_id);
    const failing = Boolean(hook?.failing);
    const at = nowIso();
    webhookDeliveries().update(deliveryId, {
      attempts: row.attempts + 1,
      last_attempt_at: at,
      last_duration_ms: failing ? 10_000 : 120 + Math.floor(Math.random() * 300),
      ...(failing
        ? { status: row.attempts + 1 >= row.max_attempts ? 'failed' : 'pending', last_status_code: 503, last_error: 'ปลายทางตอบ 503 Service Unavailable', last_response_body: '{"error":"maintenance"}', next_attempt_at: new Date(Date.now() + 60_000).toISOString(), ...(row.attempts + 1 >= row.max_attempts ? { completed_at: at, next_attempt_at: undefined } : {}) }
        : { status: 'succeeded', last_status_code: 200, last_error: undefined, last_response_body: '{"ok":true}', next_attempt_at: undefined, completed_at: at }),
    });
  }, delayMs);
}

route('POST', '/staff/webhooks/:id/deliveries/:deliveryId/redeliver', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  liveWebhook(ctx.params.id!);
  const row = webhookDeliveries().get(ctx.params.deliveryId!);
  if (!row || row.webhook_id !== ctx.params.id!) notFound('การส่ง');
  const next = webhookDeliveries().update(row.id, {
    status: 'pending',
    max_attempts: Math.max(row.max_attempts, row.attempts + 1),
    next_attempt_at: nowIso(),
    completed_at: undefined,
  })!;
  attempt(row.id, 2_500);
  return deliveryView(next);
});

/**
 * An event for the webhooks that subscribe to it. Other modules may call this when something the
 * contract's webhook events name happens; the delivery shows in the log and completes a moment later.
 */
export function fireWebhookEvent(eventType: string, data: Record<string, unknown> = {}): number {
  const hooks = webhooks().where((w) => w.active && !w.deleted_at && w.events.includes(eventType));
  const eventId = `evt_${hex(12)}`;
  for (const hook of hooks) {
    const row = webhookDeliveries().insert({
      webhook_id: hook.id,
      event_id: eventId,
      event_type: eventType,
      status: 'pending',
      attempts: 0,
      max_attempts: 6,
      next_attempt_at: nowIso(),
      created_at: nowIso(),
      payload: data,
    });
    attempt(row.id, 1_500);
  }
  return hooks.length;
}

// ---- API keys ---------------------------------------------------------------------------------------

function keyStatus(row: Row): 'active' | 'expired' | 'revoked' {
  if (row.revoked_at) return 'revoked';
  return row.expires_at <= nowIso() ? 'expired' : 'active';
}

function keyView(row: Row) {
  return {
    id: row.id,
    name: row.name,
    prefix: row.prefix,
    scopes: row.scopes,
    branch_ids: row.branch_ids,
    status: keyStatus(row),
    created_by_staff_id: row.created_by_staff_id,
    created_at: row.created_at,
    expires_at: row.expires_at,
    ...(row.last_used_at ? { last_used_at: row.last_used_at } : {}),
    ...(row.revoked_at ? { revoked_at: row.revoked_at } : {}),
  };
}

route('GET', '/staff/api-keys', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  return { data: apiKeys().all().sort((a, b) => b.created_at.localeCompare(a.created_at)).map(keyView) };
});

route('POST', '/staff/api-keys', (ctx) => {
  const auth = requireStaff(ctx, 'integration:manage');
  const b = ctx.body ?? {};
  const name = String(b.name ?? '').trim();
  if (!name) invalid('name', 'กรุณาตั้งชื่อ key');
  const scopes: string[] = Array.isArray(b.scopes) ? [...new Set<string>(b.scopes)] : [];
  if (scopes.length === 0) invalid('scopes', 'เลือกสิทธิ์อย่างน้อยหนึ่งข้อ');
  const beyond = scopes.find((s) => !allows(auth.scopes, s));
  if (beyond) invalid('scopes', `บัญชีนี้ไม่มีสิทธิ์ ${beyond} จึงให้ key ไม่ได้`);
  const expires = String(b.expires_at ?? '');
  const expiresMs = Date.parse(expires);
  if (!expiresMs || expiresMs <= Date.now()) invalid('expires_at', 'วันหมดอายุต้องอยู่ในอนาคต');
  if (expiresMs > Date.now() + 366 * 86_400_000) invalid('expires_at', 'key มีอายุได้ไม่เกินหนึ่งปี');
  const prefix = hex(4);
  const key = `hp_${prefix}_${random(32)}`;
  const row = apiKeys().insert({
    name,
    prefix: `hp_${prefix}`,
    scopes,
    branch_ids: Array.isArray(b.branch_ids) ? b.branch_ids : [],
    created_by_staff_id: auth.staffId,
    created_at: nowIso(),
    expires_at: new Date(expiresMs).toISOString(),
  });
  return created({ id: row.id, name, key, prefix: row.prefix, scopes, branch_ids: row.branch_ids, expires_at: row.expires_at, created_at: row.created_at });
});

route('DELETE', '/staff/api-keys/:id', (ctx) => {
  requireStaff(ctx, 'integration:manage');
  const row = apiKeys().get(ctx.params.id!);
  if (!row) notFound('API key');
  if (!row.revoked_at) apiKeys().update(row.id, { revoked_at: nowIso() });
  return noContent();
});

/** A follower's public details, for the customers module when the counter links one to a customer. */
export function followerOf(lineUserId: string): Row | undefined {
  return lineFollowers().find((f) => f.line_user_id === lineUserId);
}
