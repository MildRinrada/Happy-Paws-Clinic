import { day, table, type Row } from '../../core/db';
import { hasConsent } from '../customers/store';

/**
 * The CRM module's tables, and the reads it makes of other modules' tables. Other modules' rows are
 * read in the shape their own API answers with (README rule 3); every read here tolerates a table
 * that is still empty, since the seeds of the other modules run in parallel during development.
 */

// ---- Own tables ----------------------------------------------------------------------------------

export const segments = () => table('crm_segments');
export const coupons = () => table('crm_coupons');
export const couponRedemptions = () => table('crm_coupon_redemptions');
export const packages = () => table('crm_packages');
export const pointsEntries = () => table('crm_points_entries');
export const threads = () => table('msg_threads');
export const threadMessages = () => table('msg_thread_messages');
/** The portal inbox: what `GET /portal/notifications` lists. */
export const inbox = () => table('notifications');
/** One row per delivery attempt, what `GET /staff/notifications/log` lists. */
export const deliveries = () => table('notification_deliveries');
export const automations = () => table('automations');
export const webhooks = () => table('webhooks');
export const webhookDeliveries = () => table('webhook_deliveries');
export const apiKeys = () => table('api_keys');
export const lineFollowers = () => table('line_followers');
export const calendarLinks = () => table('calendar_links');
export const idempotency = () => table('crm_idempotency');

// ---- Other modules' tables (read only) -----------------------------------------------------------

export function activeCustomers(): Row[] {
  return table('customers').where((c) => c.is_active !== false && !c.merged_into_customer_id && !c.deleted_at);
}

export function customer(id: string | null | undefined): Row | undefined {
  return table('customers').get(id);
}

export function customerName(id: string | null | undefined): string | undefined {
  return customer(id)?.display_name;
}

/** The pets a customer is the primary owner of, living ones only. */
export function petsOf(customerId: string): Row[] {
  return table('pets').where((p) => p.primary_customer_id === customerId && !p.is_deceased && !p.deleted_at);
}

export function pet(id: string | null | undefined): Row | undefined {
  return table('pets').get(id);
}

export function branches(): Row[] {
  return table('branches').all();
}

export function branchName(id: string | null | undefined): string | undefined {
  return table('branches').get(id)?.name;
}

export function services(): Row[] {
  return table('services').where((s) => s.is_active !== false);
}

export function staffMember(id: string | null | undefined): Row | undefined {
  return table('staff_members').get(id);
}

export function staffName(id: string | null | undefined): string | undefined {
  return staffMember(id)?.display_name;
}

export function staffByUser(userId: string): Row | undefined {
  return table('staff_members').find((s) => s.user_id === userId);
}

export function appointmentsOf(customerId: string): Row[] {
  return table('appointments').where((a) => a.customer_id === customerId);
}

export function invoicesOf(customerId: string): Row[] {
  return table('invoices').where((i) => i.customer_id === customerId && i.status !== 'void' && i.status !== 'voided');
}

/** Visit days of a customer: finished appointments and bills, one per day, oldest first. */
export function visitDaysOf(customerId: string): string[] {
  const days = new Set<string>();
  for (const a of appointmentsOf(customerId)) {
    if (a.status === 'completed' || a.status === 'checked_in' || a.status === 'in_progress') days.add(String(a.starts_at).slice(0, 10));
  }
  for (const i of invoicesOf(customerId)) {
    if (i.issued_at && (i.paid_satang ?? 0) > 0) days.add(String(i.issued_at).slice(0, 10));
  }
  return [...days].filter((d) => d <= day(0)).sort();
}

export function spendOf(customerId: string): number {
  return invoicesOf(customerId).reduce((sum, i) => sum + (Number(i.paid_satang) || 0) - (Number(i.refunded_satang) || 0), 0);
}

/**
 * The branch a customer mostly comes to: of their appointments, then bills; the first branch when
 * neither says.
 */
export function homeBranchOf(customerId: string): string | undefined {
  const counts = new Map<string, number>();
  for (const row of [...appointmentsOf(customerId), ...invoicesOf(customerId)]) {
    if (row.branch_id) counts.set(row.branch_id, (counts.get(row.branch_id) ?? 0) + 1);
  }
  const best = [...counts.entries()].sort((a, b) => b[1] - a[1])[0]?.[0];
  return best ?? branches()[0]?.id;
}

/** Whether the customer agreed to marketing, by the customers module's consent record (default: no). */
export function hasMarketingConsent(customerId: string): boolean {
  return hasConsent(customerId, 'marketing');
}

/** Whether the customer switched off service messages (default: on). */
export function serviceMessagesOff(customerId: string): boolean {
  return !hasConsent(customerId, 'service_messages');
}

// ---- Small helpers -------------------------------------------------------------------------------

/** A stable number from a string, for choices a seed or a fallback makes the same way every time. */
export function hash(text: string): number {
  let h = 2166136261;
  for (let i = 0; i < text.length; i++) {
    h ^= text.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  return h >>> 0;
}

/** A seeded random number generator, so a seed makes the same choices each time it runs. */
export function rng(seedText: string): () => number {
  let a = hash(seedText) || 1;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export function maskPhone(phone: string | null | undefined): string | undefined {
  const digits = (phone ?? '').replace(/\D/g, '');
  return digits ? `***${digits.slice(-4)}` : undefined;
}

export function maskEmail(email: string | null | undefined): string | undefined {
  if (!email || !email.includes('@')) return undefined;
  const [name, domain] = email.split('@');
  return `${name!.slice(0, 1)}***@${domain}`;
}

export function maskLine(lineUserId: string | null | undefined): string | undefined {
  return lineUserId ? `U***${lineUserId.slice(-4)}` : undefined;
}

/** Days between two `YYYY-MM-DD` days (b - a). */
export function daysBetween(a: string, b: string): number {
  return Math.round((Date.parse(`${b}T00:00:00Z`) - Date.parse(`${a}T00:00:00Z`)) / 86_400_000);
}

/** Drops `undefined` and `null` keys, as the API leaves absent values out. */
export function compact<T extends Record<string, unknown>>(row: T): T {
  const out: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(row)) if (value !== undefined && value !== null) out[key] = value;
  return out as T;
}

/** Strips the internal fields this module keeps on a row before it goes on the wire. */
export function strip<T extends Row>(row: T, ...keys: string[]): T {
  const copy = { ...row } as Record<string, unknown>;
  for (const key of keys) delete copy[key];
  return copy as T;
}

/**
 * A UUID that is the same every time for the same key, so the seed's segments, webhooks and
 * threads keep their addresses across a reset and a link to one can be shared or bookmarked.
 */
export function stableId(key: string): string {
  const next = rng(`id-${key}`);
  const hex = Array.from({ length: 32 }, () => Math.floor(next() * 16).toString(16)).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-4${hex.slice(13, 16)}-8${hex.slice(17, 20)}-${hex.slice(20, 32)}`;
}
