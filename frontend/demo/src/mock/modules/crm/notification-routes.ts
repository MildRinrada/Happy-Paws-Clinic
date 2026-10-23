import { emit } from '../../core/bus';
import { nowIso } from '../../core/db';
import { checkVersion, created, fail, invalid, noContent, notFound, page, requireCustomer, requireStaff, route } from '../../core/http';
import { membersOf } from './insights';
import { dispatch, lineSettings, newBatchId, summarise, type Channel } from './notify';
import { automationView, publicReminder, remindersOf, windowOf } from './reminders';
import { automations, deliveries, inbox, segments } from './store';
import { templateOf, TEMPLATES } from './templates';

/**
 * Notifications: the template list, a staff send, the delivery log, the LINE broadcast, the owner's
 * web inbox, and the reminder rules with what they will send.
 */

// ---- Templates, send, log ---------------------------------------------------------------------------

route('GET', '/staff/notifications/templates', (ctx) => {
  requireStaff(ctx, 'notify:manage');
  const line = lineSettings.get();
  const data = [...TEMPLATES]
    .sort((a, b) => Number(b.staff_sendable) - Number(a.staff_sendable))
    .map((t) => ({
      key: t.key,
      name: t.name,
      purpose: t.purpose,
      staff_sendable: t.staff_sendable,
      variables: t.variables,
      line_enabled: line.notification_types?.[t.key] !== false,
      bodies: { title: t.title, line: t.line, sms: t.sms },
    }));
  return { data };
});

const CHANNELS = ['auto', 'line', 'sms', 'email'];

route('POST', '/staff/notifications/send', (ctx) => {
  const auth = requireStaff(ctx, 'notify:send');
  const b = ctx.body ?? {};
  const template = templateOf(String(b.template ?? ''));
  if (!template) invalid('template', 'ไม่พบเทมเพลตนี้');
  if (!template.staff_sendable) invalid('template', 'เทมเพลตนี้ระบบส่งเองเท่านั้น');
  const ids: string[] = Array.isArray(b.customer_ids) ? [...new Set<string>(b.customer_ids.map(String))] : [];
  if (ids.length === 0) invalid('customer_ids', 'เลือกผู้รับอย่างน้อยหนึ่งคน');
  if (ids.length > 100) invalid('customer_ids', 'ส่งได้ครั้งละไม่เกิน 100 คน');
  const channel = CHANNELS.includes(b.channel) ? b.channel : 'auto';
  const values: Record<string, string> = b.variables ?? {};
  for (const variable of template.variables) {
    const value = String(values[variable.name] ?? '').trim();
    if (variable.required && !value) invalid(`variables.${variable.name}`, `กรุณากรอก ${variable.name}`);
    if (value.length > variable.max_length) invalid(`variables.${variable.name}`, `${variable.name} ยาวเกิน ${variable.max_length} ตัวอักษร`);
  }
  const batchId = newBatchId();
  const results = ids.map((id) =>
    dispatch(id, template.key, values, { channel, source: 'manual', batchId, staffId: auth.staffId, branchId: auth.branches[0] }),
  );
  return summarise(batchId, results);
});

const LOG_FILTERS = ['status', 'channel', 'source', 'customer_id', 'template', 'batch_id'] as const;

route('GET', '/staff/notifications/log', (ctx) => {
  const auth = requireStaff(ctx, 'notify:read');
  const rows = deliveries()
    .where((d) => !auth.branches.length || !d.branch_id || auth.branches.includes(d.branch_id))
    .filter((d) => LOG_FILTERS.every((key) => !ctx.query.get(key) || d[key] === ctx.query.get(key)))
    .sort((a, b) => b.attempted_at.localeCompare(a.attempted_at))
    .map(({ subject_key: _s, automation_id: _a, ...d }) => d);
  return page(rows, ctx.query, 30);
});

// ---- LINE broadcast ---------------------------------------------------------------------------------

/** LINE pushes of broadcasts sent this calendar month: what the monthly quota counts. */
function broadcastsThisMonth(): number {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
  return deliveries().where((d) => d.source === 'broadcast' && d.status === 'sent' && d.channel === 'line' && d.attempted_at >= start).length;
}

route('POST', '/staff/integrations/line/broadcast', (ctx) => {
  const auth = requireStaff(ctx, 'notify:broadcast');
  const b = ctx.body ?? {};
  const message = String(b.message ?? '').trim();
  if (!message) invalid('message', 'กรุณาพิมพ์ข้อความประกาศ');
  if (message.length > 1000) invalid('message', 'ข้อความยาวเกิน 1,000 ตัวอักษร');
  const settings = lineSettings.get();
  if (!settings.enabled || !settings.channel_access_token) fail(409, 'line_not_configured', 'ยังไม่ได้เชื่อมต่อ LINE Official Account หรือปิดการส่งไว้');

  let ids: string[];
  if (b.segment_id) {
    const segment = segments().get(b.segment_id);
    if (!segment) notFound('กลุ่มลูกค้า');
    ids = membersOf(segment.criteria).map((c) => c.id);
  } else {
    ids = Array.isArray(b.customer_ids) ? [...new Set<string>(b.customer_ids.map(String))] : [];
    if (ids.length === 0) invalid('customer_ids', 'เลือกผู้รับอย่างน้อยหนึ่งคน');
  }

  const limit = settings.broadcast_monthly_limit;
  const used = broadcastsThisMonth();
  if (used >= limit) fail(409, 'quota_exceeded', `ใช้โควตาประกาศเดือนนี้ครบ ${limit.toLocaleString('th-TH')} ข้อความแล้ว`);

  const batchId = newBatchId();
  const results = ids.map((id) => {
    // Stop at the quota: the rest are not sent, and say so.
    if (broadcastsThisMonth() >= limit) return { customer_id: id, status: 'skipped' as const, channel: 'line' as Channel, error_code: 'provider_quota' };
    return dispatch(id, 'clinic.announcement', { message }, { channel: 'line', source: 'broadcast', batchId, staffId: auth.staffId, branchId: auth.branches[0] });
  });
  const after = broadcastsThisMonth();
  return { ...summarise(batchId, results), quota: { limit, used: after, remaining: Math.max(0, limit - after) } };
});

// ---- The owner's web inbox --------------------------------------------------------------------------

function inboxView(row: Record<string, any>) {
  return { id: row.id, template: row.template, title: row.title, body: row.body, sent_at: row.sent_at, read_at: row.read_at ?? null };
}

route('GET', '/portal/notifications', (ctx) => {
  const auth = requireCustomer(ctx);
  const mine = inbox()
    .where((n) => n.customer_id === auth.customerId)
    .sort((a, b) => b.sent_at.localeCompare(a.sent_at));
  const unreadOnly = ctx.query.get('unread') === 'true';
  const rows = unreadOnly ? mine.filter((n) => !n.read_at) : mine;
  const result = page(rows.map(inboxView), ctx.query, 20);
  return { data: result.data, page: { next_cursor: result.page.next_cursor ?? null }, unread_count: mine.filter((n) => !n.read_at).length };
});

route('POST', '/portal/notifications/read', (ctx) => {
  const auth = requireCustomer(ctx);
  const ids: string[] | undefined = Array.isArray(ctx.body?.ids) ? ctx.body.ids : undefined;
  let marked = 0;
  for (const row of inbox().where((n) => n.customer_id === auth.customerId && !n.read_at && (!ids || ids.includes(n.id)))) {
    inbox().update(row.id, { read_at: nowIso() });
    marked++;
  }
  emit('notifications', { customer_id: auth.customerId });
  return { marked, unread_count: inbox().where((n) => n.customer_id === auth.customerId && !n.read_at).length };
});

// ---- Automations ------------------------------------------------------------------------------------

const TRIGGERS = ['vaccine_due', 'appointment_upcoming', 'treatment_plan_session', 'customer_lapsed', 'visit_completed'];

function visibleRules(branches: string[]) {
  return automations().where((r) => !r.deleted_at && (!branches.length || !r.condition?.branch_id || branches.includes(r.condition.branch_id)));
}

function checkTrigger(trigger: Record<string, any>) {
  if (!TRIGGERS.includes(trigger.type)) invalid('trigger.type', 'ชนิดเหตุการณ์ไม่ถูกต้อง');
  if (trigger.offset_hours !== undefined) {
    if (trigger.type !== 'appointment_upcoming' && trigger.type !== 'treatment_plan_session') invalid('trigger.offset_hours', 'เหตุการณ์นี้นับเป็นวันเท่านั้น');
    if (!(trigger.offset_hours >= 1 && trigger.offset_hours <= 168)) invalid('trigger.offset_hours', 'ล่วงหน้าได้ 1 ถึง 168 ชั่วโมง');
  } else {
    const d = Number(trigger.offset_days);
    const afterOnly = trigger.type === 'customer_lapsed' ? [-90, -1] : trigger.type === 'visit_completed' ? [-30, -1] : [0, 90];
    if (!Number.isInteger(d) || d < afterOnly[0]! || d > afterOnly[1]!) invalid('trigger.offset_days', `จำนวนวันต้องอยู่ระหว่าง ${afterOnly[0]} ถึง ${afterOnly[1]}`);
    if (trigger.send_time && !/^\d{2}:\d{2}$/.test(trigger.send_time)) invalid('trigger.send_time', 'เวลาส่งไม่ถูกต้อง');
  }
}

route('GET', '/staff/automations', (ctx) => {
  const auth = requireStaff(ctx, 'automation:read');
  return { data: visibleRules(auth.branches).sort((a, b) => a.created_at.localeCompare(b.created_at)).map(automationView) };
});

route('POST', '/staff/automations', (ctx) => {
  const auth = requireStaff(ctx, 'automation:manage');
  const b = ctx.body ?? {};
  const name = String(b.name ?? '').trim();
  if (!name) invalid('name', 'กรุณาตั้งชื่อกฎ');
  const trigger = b.trigger ?? {};
  checkTrigger(trigger);
  const c = b.condition ?? {};
  if (auth.branches.length && !c.branch_id) invalid('condition.branch_id', 'บัญชีที่ดูแลบางสาขาต้องเลือกสาขาของกฎ');
  const channel = CHANNELS.includes(b.action?.channel) ? b.action.channel : 'auto';
  const row = automations().insert({
    version: 1,
    name,
    enabled: b.enabled !== false,
    trigger: { type: trigger.type, ...(trigger.offset_hours !== undefined ? { offset_hours: trigger.offset_hours } : { offset_days: trigger.offset_days, ...(trigger.send_time ? { send_time: trigger.send_time } : {}) }) },
    condition: {
      ...(c.branch_id ? { branch_id: c.branch_id } : {}),
      ...(c.species ? { species: c.species } : {}),
      ...(c.vaccine_name ? { vaccine_name: c.vaccine_name } : {}),
      unconfirmed_only: Boolean(c.unconfirmed_only),
    },
    action: { type: 'send_notification', channel },
    created_by_staff_id: auth.staffId,
    created_at: nowIso(),
  });
  return created(automationView(row));
});

route('PATCH', '/staff/automations/:id', (ctx) => {
  const auth = requireStaff(ctx, 'automation:manage');
  const row = visibleRules(auth.branches).find((r) => r.id === ctx.params.id!);
  if (!row) notFound('กฎการแจ้งเตือน');
  checkVersion(ctx, { version: row.version });
  const b = ctx.body ?? {};
  const patch: Record<string, unknown> = {};
  if (b.name !== undefined) {
    if (!String(b.name).trim()) invalid('name', 'กรุณาตั้งชื่อกฎ');
    patch.name = String(b.name).trim();
  }
  if (b.trigger) {
    if (b.trigger.type && b.trigger.type !== row.trigger.type) invalid('trigger.type', 'เปลี่ยนชนิดเหตุการณ์ของกฎไม่ได้ สร้างกฎใหม่แทน');
    const trigger = { ...b.trigger, type: row.trigger.type };
    checkTrigger(trigger);
    patch.trigger = trigger;
  }
  if (b.condition) {
    const next = { ...row.condition };
    for (const key of ['species', 'vaccine_name'] as const) {
      if (b.condition[key] === '') delete next[key];
      else if (b.condition[key] !== undefined) next[key] = b.condition[key];
    }
    if (b.condition.unconfirmed_only !== undefined) next.unconfirmed_only = Boolean(b.condition.unconfirmed_only);
    patch.condition = next;
  }
  if (b.action?.channel && CHANNELS.includes(b.action.channel)) patch.action = { ...row.action, channel: b.action.channel };
  if (typeof b.enabled === 'boolean') patch.enabled = b.enabled;
  return automationView(automations().update(row.id, patch)!);
});

route('DELETE', '/staff/automations/:id', (ctx) => {
  const auth = requireStaff(ctx, 'automation:manage');
  const row = visibleRules(auth.branches).find((r) => r.id === ctx.params.id!);
  if (!row) notFound('กฎการแจ้งเตือน');
  checkVersion(ctx, { version: row.version });
  automations().update(row.id, { deleted_at: nowIso(), enabled: false });
  return noContent();
});

route('POST', '/staff/automations/:id/simulate', (ctx) => {
  const auth = requireStaff(ctx, 'automation:manage');
  const row = visibleRules(auth.branches).find((r) => r.id === ctx.params.id!);
  if (!row) notFound('กฎการแจ้งเตือน');
  const days = Math.max(1, Math.min(90, Number(ctx.body?.days) || 7));
  const all = remindersOf(row, days);
  const limit = 200;
  return {
    automation_id: row.id,
    enabled: row.enabled,
    evaluated_at: nowIso(),
    ...windowOf(days),
    due_now: all.filter((r) => r.due_now).length,
    would_send: all.filter((r) => r.status === 'would_send').length,
    would_skip: all.filter((r) => r.status === 'would_skip').length,
    already_sent: all.filter((r) => r.status === 'already_sent').length,
    truncated: all.length > limit,
    recipients: all.slice(0, limit).map(publicReminder),
  };
});

route('GET', '/staff/reminders/upcoming', (ctx) => {
  const auth = requireStaff(ctx, 'automation:read');
  const days = Math.max(1, Math.min(90, Number(ctx.query.get('days')) || 7));
  const branch = ctx.query.get('branch') ?? undefined;
  const all = visibleRules(auth.branches)
    .filter((r) => r.enabled)
    .flatMap((r) => remindersOf(r, days, branch))
    .sort((a, b) => a.send_at.localeCompare(b.send_at));
  const limit = 500;
  return { ...windowOf(days), total: all.length, truncated: all.length > limit, data: all.slice(0, limit).map(publicReminder) };
});
