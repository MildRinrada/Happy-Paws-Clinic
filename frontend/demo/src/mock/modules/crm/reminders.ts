import { day, nowIso, table, type Row } from '../../core/db';
import { lapseOf, vaccineDues } from './insights';
import { canReceiveLine, lineSettings, wordsOf } from './notify';
import { automations, customer, deliveries, hasMarketingConsent, homeBranchOf, inbox, maskLine, maskPhone, pet, petsOf, serviceMessagesOff, staffName } from './store';
import { templateOf } from './templates';

/**
 * What the reminder rules would send (`GET /staff/reminders/upcoming`, `POST .../simulate`), worked
 * out by one function for both, as the backend does. A rule's subject is a vaccine due date, an
 * appointment, a lapsed customer or a finished visit; its send time is that day moved by the rule's
 * offset.
 */

const TEMPLATE_OF: Record<string, string> = {
  vaccine_due: 'vaccine.due',
  appointment_upcoming: 'appointment.reminder',
  treatment_plan_session: 'treatment_plan.session',
  customer_lapsed: 'customer.lapsed',
  visit_completed: 'review.invitation',
};

export function templateOfTrigger(type: string): string {
  return TEMPLATE_OF[type] ?? 'clinic.message';
}

const thaiDate = (iso: string) =>
  new Date(iso.length === 10 ? `${iso}T00:00:00` : iso).toLocaleDateString('th-TH', { day: 'numeric', month: 'short', year: 'numeric' });
const thaiTime = (iso: string) => new Date(iso).toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit', hour12: false });

/** A day plus `offset` days, at `HH:mm` local time. */
function atDay(dayText: string, offsetDays: number, time = '09:00'): string {
  const [h, m] = time.split(':').map(Number);
  const date = new Date(`${dayText}T00:00:00`);
  date.setDate(date.getDate() + offsetDays);
  date.setHours(h ?? 9, m ?? 0, 0, 0);
  return date.toISOString();
}

interface Subject {
  customer_id: string;
  pet_id: string;
  branch_id?: string;
  send_at: string;
  subject: Record<string, unknown> & { type: string; id: string; due_on: string };
  values: Record<string, string | undefined>;
  species?: string;
  vaccine_name?: string;
  unconfirmed?: boolean;
}

/** Every subject a rule could message about, with its send time. Not yet filtered by window. */
function subjectsOf(rule: Row): Subject[] {
  const t = rule.trigger ?? {};
  const out: Subject[] = [];
  if (t.type === 'vaccine_due') {
    for (const due of vaccineDues()) {
      const p = pet(due.pet_id);
      out.push({
        customer_id: due.customer_id,
        pet_id: due.pet_id,
        branch_id: homeBranchOf(due.customer_id),
        send_at: atDay(due.due_on, -(t.offset_days ?? 0), t.send_time),
        subject: { type: 'vaccination', id: `${due.pet_id}:${due.vaccine_name}`, due_on: due.due_on, label: due.vaccine_name },
        values: { pet_name: p?.name, date: thaiDate(due.due_on), vaccine_name: due.vaccine_name },
        species: p?.species,
        vaccine_name: due.vaccine_name,
      });
    }
  } else if (t.type === 'appointment_upcoming') {
    for (const a of table('appointments').where((x) => ['booked', 'confirmed'].includes(x.status) && x.starts_at > nowIso())) {
      const p = pet(a.pet_id);
      const sendAt =
        t.offset_hours !== undefined
          ? new Date(Date.parse(a.starts_at) - t.offset_hours * 3_600_000).toISOString()
          : atDay(String(new Date(a.starts_at).toLocaleDateString('sv-SE')), -(t.offset_days ?? 1), t.send_time);
      const service = table('services').get(a.service_id);
      out.push({
        customer_id: a.customer_id,
        pet_id: a.pet_id,
        branch_id: a.branch_id,
        send_at: sendAt,
        subject: {
          type: 'appointment',
          id: a.id,
          due_on: new Date(a.starts_at).toLocaleDateString('sv-SE'),
          starts_at: a.starts_at,
          appointment_id: a.id,
          ...(service?.name ? { label: service.name } : {}),
        },
        values: { pet_name: p?.name, date: thaiDate(a.starts_at), time: thaiTime(a.starts_at), vet_name: staffName(a.staff_id) },
        species: p?.species,
        unconfirmed: !a.confirmed_at && a.status !== 'confirmed',
      });
    }
  } else if (t.type === 'customer_lapsed') {
    for (const c of table('customers').where((x) => x.is_active !== false && !x.merged_into_customer_id)) {
      const lapse = lapseOf(c.id);
      const pets = petsOf(c.id);
      if (!lapse || pets.length === 0) continue;
      out.push({
        customer_id: c.id,
        pet_id: pets[0]!.id,
        branch_id: homeBranchOf(c.id),
        send_at: atDay(lapse.due_on, -(t.offset_days ?? -7), t.send_time),
        subject: { type: 'customer', id: c.id, due_on: lapse.due_on },
        values: { pet_name: pets.map((p) => p.name).join(' และ '), date: thaiDate(lapse.last_visit_on) },
        species: pets[0]!.species,
      });
    }
  } else if (t.type === 'visit_completed') {
    for (const a of table('appointments').where((x) => x.status === 'completed')) {
      const p = pet(a.pet_id);
      const visitDay = new Date(a.starts_at).toLocaleDateString('sv-SE');
      out.push({
        customer_id: a.customer_id,
        pet_id: a.pet_id,
        branch_id: a.branch_id,
        send_at: atDay(visitDay, -(t.offset_days ?? -1), t.send_time),
        subject: { type: 'appointment', id: a.id, due_on: visitDay, appointment_id: a.id },
        values: { pet_name: p?.name, date: thaiDate(a.starts_at) },
        species: p?.species,
      });
    }
  }
  return out;
}

function conditionHolds(rule: Row, s: Subject): boolean {
  const c = rule.condition ?? {};
  if (c.branch_id && s.branch_id !== c.branch_id) return false;
  if (c.species && !String(s.species ?? '').includes(c.species)) return false;
  if (c.vaccine_name && !String(s.vaccine_name ?? '').includes(c.vaccine_name)) return false;
  if (c.unconfirmed_only && !s.unconfirmed) return false;
  return true;
}

function subjectKey(rule: Row, s: Subject): string {
  return `${rule.id}|${s.subject.type}|${s.subject.id}|${s.subject.due_on}`;
}

/** Why a reminder would not go: no consent, or no channel the rule may use. */
function skipReason(rule: Row, customerId: string, template: string): string | undefined {
  const row = customer(customerId);
  if (!row) return 'no_recipient';
  const marketing = templateOf(template)?.purpose === 'marketing';
  if (marketing ? !hasMarketingConsent(customerId) : serviceMessagesOff(customerId)) return 'no_consent';
  const channel = rule.action?.channel ?? 'auto';
  const line = canReceiveLine(customerId) && lineSettings.get().enabled;
  if (channel === 'line' && !line) return 'no_address';
  if (channel === 'sms' && !row.phone) return 'no_address';
  if (channel === 'email' && !row.email) return 'no_address';
  if (channel === 'auto' && !line && !row.phone && !row.email) return 'no_channel';
  return undefined;
}

/** What one rule would send between now and `days` ahead (and anything already due). */
export function remindersOf(rule: Row, days: number, branchId?: string) {
  const from = nowIso();
  const to = new Date(Date.now() + days * 86_400_000).toISOString();
  // A send time up to a day past still counts as due now: the job runs every few minutes.
  const dueFloor = new Date(Date.now() - 86_400_000).toISOString();
  const template = templateOfTrigger(rule.trigger?.type);
  return subjectsOf(rule)
    .filter((s) => s.send_at >= dueFloor && s.send_at <= to && conditionHolds(rule, s) && (!branchId || !s.branch_id || s.branch_id === branchId))
    .map((s) => {
      const key = subjectKey(rule, s);
      const sent = deliveries().find((d) => d.subject_key === key && d.status === 'sent');
      const reason = sent ? undefined : skipReason(rule, s.customer_id, template);
      const words = wordsOf(template, s.values);
      const owner = customer(s.customer_id);
      return {
        automation_id: rule.id,
        automation_name: rule.name,
        send_at: s.send_at,
        due_now: s.send_at <= from,
        status: sent ? 'already_sent' : reason ? 'would_skip' : 'would_send',
        ...(reason ? { skip_reason: reason } : {}),
        customer_id: s.customer_id,
        customer_name: owner?.display_name ?? '',
        pet_id: s.pet_id,
        pet_name: pet(s.pet_id)?.name ?? s.values.pet_name ?? '',
        ...(s.branch_id ? { branch_id: s.branch_id } : {}),
        subject: s.subject,
        template,
        channel: rule.action?.channel ?? 'auto',
        message: { title: words.title, body: words.line },
        _key: key,
        _values: s.values,
      };
    })
    .sort((a, b) => a.send_at.localeCompare(b.send_at));
}

export type ReminderRow = ReturnType<typeof remindersOf>[number];

export function publicReminder(r: ReminderRow) {
  const { _key, _values, ...rest } = r;
  return rest;
}

/** `days` from today as the window's ends, the way the answer states them. */
export function windowOf(days: number) {
  return { from: nowIso(), to: new Date(Date.now() + days * 86_400_000).toISOString() };
}

/**
 * Records what is due by now on every switched-on rule as sent this morning, as the backend's job
 * would have done before the demo opened. The log and the inbox get it; the outbox does not, so the
 * demo bar shows only what the visitor sends.
 */
export function markDueAsSent(): number {
  let sent = 0;
  for (const rule of automations().where((r) => r.enabled && !r.deleted_at)) {
    for (const r of remindersOf(rule, 0)) {
      if (!r.due_now || r.status !== 'would_send') continue;
      const channel = canReceiveLine(r.customer_id) ? 'line' : 'sms';
      const owner = customer(r.customer_id);
      const attempted = new Date(Math.min(Date.now() - 60_000, Date.parse(r.send_at))).toISOString();
      deliveries().insert({
        customer_id: r.customer_id,
        customer_name: owner?.display_name,
        template: r.template,
        channel,
        recipient: channel === 'line' ? maskLine(owner?.line?.line_user_id) : maskPhone(owner?.phone),
        status: 'sent',
        source: 'automation',
        branch_id: r.branch_id,
        attempted_at: attempted,
        subject_key: r._key,
        automation_id: rule.id,
      });
      inbox().insert({ customer_id: r.customer_id, template: r.template, title: r.message.title, body: r.message.body, sent_at: attempted, read_at: null });
      sent++;
    }
  }
  return sent;
}

/** A rule's history from the delivery log, on top of what the seed says it sent before. */
export function statsOf(rule: Row) {
  const base = rule.seed_stats ?? { total: 0, sent: 0, skipped: 0, failed: 0, lost: 0, last_30_days: 0 };
  const rows = deliveries().where((d) => d.automation_id === rule.id);
  const monthAgo = new Date(Date.now() - 30 * 86_400_000).toISOString();
  const last = rows.map((d) => d.attempted_at).sort().pop() ?? base.last_fired_at;
  return {
    total: base.total + rows.length,
    sent: base.sent + rows.filter((d) => d.status === 'sent').length,
    skipped: base.skipped + rows.filter((d) => d.status === 'skipped').length,
    failed: base.failed + rows.filter((d) => d.status === 'failed').length,
    lost: base.lost,
    last_30_days: base.last_30_days + rows.filter((d) => d.attempted_at >= monthAgo).length,
    ...(last ? { last_fired_at: last } : {}),
  };
}

export function automationView(rule: Row) {
  const { seed_stats: _s, deleted_at: _d, ...rest } = rule;
  return { ...rest, action: { type: 'send_notification', channel: rule.action?.channel ?? 'auto', template: templateOfTrigger(rule.trigger?.type) }, stats: statsOf(rule) };
}

export const today = () => day(0);
