import { emit } from '../../core/bus';
import { nowIso, singleton, table, uuid, type Row } from '../../core/db';
import { send } from '../../core/outbox';
import {
  customer,
  deliveries,
  hasMarketingConsent,
  inbox,
  lineFollowers,
  maskEmail,
  maskLine,
  maskPhone,
  serviceMessagesOff,
} from './store';
import { fill, templateOf, TEMPLATES } from './templates';

/**
 * The notification dispatcher, as the backend's: one message to one customer, worded from the
 * catalogue, tried on LINE, then SMS, then email (or on the one channel asked for), with every
 * attempt in the delivery log, the words in the customer's web inbox, and what "went out" in the
 * demo outbox. Consent is checked first: marketing needs it given, service messages need it not
 * withdrawn.
 */

export type Channel = 'line' | 'sms' | 'email';
export type Source = 'system' | 'manual' | 'broadcast' | 'automation';

// ---- LINE Official Account --------------------------------------------------------------------------

export interface LineSettingsRow {
  version: number;
  channel_id?: string;
  channel_secret?: string;
  channel_access_token?: string;
  enabled: boolean;
  notification_types: Record<string, boolean>;
  broadcast_monthly_limit: number;
  updated_at?: string;
}

export const lineSettings = singleton<LineSettingsRow>('line_settings', () => ({
  version: 0,
  enabled: false,
  notification_types: Object.fromEntries(TEMPLATES.map((t) => [t.key, true])),
  broadcast_monthly_limit: 300,
}));

/** `PUT /staff/integrations/line`'s answer: never a secret, only whether one is stored. */
export function lineSettingsView() {
  const s = lineSettings.get();
  const types = Object.fromEntries(TEMPLATES.map((t) => [t.key, s.notification_types?.[t.key] ?? true]));
  return {
    version: s.version,
    ...(s.channel_id ? { channel_id: s.channel_id } : {}),
    enabled: s.enabled,
    has_channel_secret: Boolean(s.channel_secret),
    has_channel_access_token: Boolean(s.channel_access_token),
    secret_source: s.channel_secret ? 'database' : 'none',
    token_source: s.channel_access_token ? 'database' : 'none',
    can_push: s.enabled && Boolean(s.channel_access_token),
    can_verify: Boolean(s.channel_secret),
    notification_types: types,
    broadcast_monthly_limit: s.broadcast_monthly_limit,
    webhook_path: '/webhooks/line',
    ...(s.updated_at ? { updated_at: s.updated_at } : {}),
  };
}

/**
 * The LINE address a customer is linked to: the customers module keeps it on the customer row
 * (`line`, docs/contract-extensions.md § 18), set when the counter picks a follower.
 */
export function lineFollowerOf(customerId: string): Row | undefined {
  const line = customer(customerId)?.line;
  if (!line?.line_user_id) return undefined;
  const follower = lineFollowers().find((f) => f.line_user_id === line.line_user_id);
  return { id: line.line_user_id, ...follower, ...line };
}

/** Linked and not blocked: a LINE message would reach them now. */
export function canReceiveLine(customerId: string): boolean {
  const follower = lineFollowerOf(customerId);
  return Boolean(follower && !follower.unfollowed_at);
}

// ---- Dispatch ---------------------------------------------------------------------------------------

export interface DispatchOptions {
  channel?: 'auto' | Channel;
  /** Tried first under `auto`: the channel the customer wrote on last. */
  preferred?: Channel;
  source?: Source;
  batchId?: string;
  staffId?: string;
  branchId?: string;
  /** Written to the web inbox too. True unless said otherwise. */
  inbox?: boolean;
  /** Words given as they are, for a caller that has no catalogue template. */
  title?: string;
  body?: string;
  /** What the message is about, so a reminder rule does not send twice for it. */
  subjectKey?: string;
  automationId?: string;
}

export interface DispatchResult {
  customer_id: string;
  status: 'sent' | 'failed' | 'skipped' | 'unknown';
  channel?: Channel;
  error_code?: string;
  fallback_from?: Channel;
  inbox_id?: string;
}

function clinicName(): string {
  return table('clinic_profile').all()[0]?.name ?? 'Happy Paws Clinic';
}

/** The words of a template for a customer, as the inbox and each channel get them. */
export function wordsOf(templateKey: string, values: Record<string, string | undefined>) {
  const template = templateOf(templateKey);
  const all = { clinic: clinicName(), portal_url: 'https://happypaws.demo/portal', ...values };
  if (!template) return { title: values.title ?? 'ข้อความจากคลินิก', line: values.message ?? '', sms: values.message ?? '' };
  return { title: fill(template.title, all), line: fill(template.line, all), sms: fill(template.sms, all) };
}

function addressOf(customerId: string, channel: Channel): { to?: string; masked?: string; error?: string } {
  const row = customer(customerId);
  if (channel === 'sms') return row?.phone ? { to: row.phone, masked: maskPhone(row.phone) } : { error: 'no_address' };
  if (channel === 'email') return row?.email ? { to: row.email, masked: maskEmail(row.email) } : { error: 'no_address' };
  const follower = lineFollowerOf(customerId);
  if (!follower) return { error: 'no_address' };
  const masked = maskLine(follower.line_user_id);
  if (follower.unfollowed_at) return { masked, error: 'unfollowed' };
  return { to: `LINE · ${follower.display_name ?? row?.display_name ?? ''}`.trim(), masked };
}

function lineRefusal(templateKey: string): string | undefined {
  const s = lineSettings.get();
  if (!s.channel_access_token) return 'channel_not_configured';
  if (!s.enabled) return 'channel_disabled';
  if (s.notification_types?.[templateKey] === false) return 'template_disabled';
  return undefined;
}

function log(row: Record<string, unknown>): void {
  deliveries().insert({ attempted_at: nowIso(), ...row });
}

/**
 * Sends one message to one customer. Answers what became of it, as a send's per-recipient result.
 * `values` fill the template; `options.title`/`body` replace its words for a free-text caller.
 */
export function dispatch(customerId: string, templateKey: string, values: Record<string, string | undefined>, options: DispatchOptions = {}): DispatchResult {
  const row = customer(customerId);
  const source = options.source ?? 'system';
  const base = {
    customer_id: customerId,
    customer_name: row?.display_name,
    template: templateKey,
    source,
    batch_id: options.batchId,
    sent_by_staff_id: options.staffId,
    branch_id: options.branchId,
    subject_key: options.subjectKey,
    automation_id: options.automationId,
  };
  if (!row) {
    log({ ...base, status: 'skipped', error_code: 'no_recipient' });
    return { customer_id: customerId, status: 'skipped', error_code: 'no_recipient' };
  }

  const template = templateOf(templateKey);
  const marketing = template?.purpose === 'marketing';
  if ((marketing && !hasMarketingConsent(customerId)) || (!marketing && serviceMessagesOff(customerId))) {
    log({ ...base, status: 'skipped', error_code: 'no_consent' });
    return { customer_id: customerId, status: 'skipped', error_code: 'no_consent' };
  }

  const words = wordsOf(templateKey, values);
  const title = options.title ?? words.title;
  const lineText = options.body ?? words.line;
  const smsText = options.body ? `${clinicName()}: ${options.body}` : words.sms;

  let inboxId: string | undefined;
  if (options.inbox !== false) {
    inboxId = inbox().insert({ customer_id: customerId, template: templateKey, title, body: lineText, sent_at: nowIso(), read_at: null }).id;
    emit('notifications', { customer_id: customerId });
  }

  const asked = options.channel ?? 'auto';
  const order: Channel[] =
    asked === 'auto' ? [...new Set<Channel>([...(options.preferred ? [options.preferred] : []), 'line', 'sms', 'email'])] : [asked];

  let failedFrom: Channel | undefined;
  let lastError: string | undefined;
  for (const channel of order) {
    const address = addressOf(customerId, channel);
    const refusal = channel === 'line' ? lineRefusal(templateKey) : undefined;
    const error = refusal ?? address.error;
    if (error) {
      lastError = error;
      // A channel that was actually tried and refused is an attempt in the log; a missing address is not.
      if (error === 'unfollowed' || asked !== 'auto') {
        log({ ...base, channel, recipient: address.masked, status: asked === 'auto' ? 'failed' : 'skipped', error_code: error, fallback_from: failedFrom });
        if (asked === 'auto') failedFrom = channel;
      }
      continue;
    }
    send({
      channel,
      to: address.to!,
      ...(channel === 'email' ? { subject: `${title} · ${clinicName()}` } : {}),
      body: channel === 'sms' ? smsText : channel === 'email' ? `เรียน ${row.display_name}\n\n${lineText}\n\n${clinicName()}` : lineText,
    });
    log({ ...base, channel, recipient: address.masked, status: 'sent', fallback_from: failedFrom });
    return { customer_id: customerId, status: 'sent', channel, ...(failedFrom ? { fallback_from: failedFrom } : {}), inbox_id: inboxId };
  }

  if (asked === 'auto') {
    log({ ...base, status: 'failed', error_code: 'no_channel', fallback_from: failedFrom });
    // The web inbox still has it: the owner reads it when they next sign in.
    return { customer_id: customerId, status: 'unknown', error_code: 'no_channel', inbox_id: inboxId };
  }
  return { customer_id: customerId, status: 'skipped', channel: asked, error_code: lastError, inbox_id: inboxId };
}

/** Wraps a run of results the way a send or broadcast answers. */
export function summarise(batchId: string, results: DispatchResult[]) {
  return {
    batch_id: batchId,
    sent: results.filter((r) => r.status === 'sent').length,
    failed: results.filter((r) => r.status === 'failed' || r.status === 'unknown').length,
    skipped: results.filter((r) => r.status === 'skipped').length,
    results: results.map(({ inbox_id: _inbox, ...r }) => r),
  };
}

export const newBatchId = () => uuid();
