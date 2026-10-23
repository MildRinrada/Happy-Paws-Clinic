import { emit } from '../../core/bus';
import { nowIso, type Row } from '../../core/db';
import { dispatch, newBatchId, type Channel, type DispatchResult } from './notify';
import { inbox } from './store';
import { templateOf } from './templates';

/**
 * What other modules (and the portal) may call on the CRM module. Everything here writes through
 * this module's own tables, so the staff pages, the portal and the demo outbox stay in step.
 *
 * Messages
 * - `notifyCustomer(customerId, { title, body, channel?, template? })`: free-text message. Web inbox
 *   row + delivery log + outbox (LINE, then SMS, then email under `auto`). Answers the per-recipient result.
 * - `notifyTemplate(customerId, templateKey, values, { channel?, branchId?, staffId? })`: a catalogue
 *   message (`queue.called`, `receipt.issued`, `consultation.summary`, `appointment.reminder`, ...),
 *   worded per channel as the backend words it. Consent is checked.
 * - `unreadCountOf(customerId)`, `latestNotificationsOf(customerId, n)`: for `GET /portal/home`.
 * - `addInboxItem(customerId, { template, title, body })`: a web-inbox row only, nothing sent.
 *
 * Threads
 * - `threadOfCustomer(customerId)`: the customer's thread row or undefined.
 * - `customerWrites(customerId, text, channel?)`, `staffReplies(threadId, text, staffId?)`.
 *
 * Loyalty and packages
 * - `loyaltyView(customerId)`: `GET /staff/loyalty/{id}`'s answer (balance, tier, history, packages).
 * - `balanceOf(customerId)`, `tierOfCustomer(customerId)`.
 * - `addPoints(customerId, kind, signedPoints, { invoice_id?, invoice_number?, note?, staff_id? })`.
 * - `earnPointsForInvoice(invoice)` / `reversePointsForInvoice(invoiceId)`: also run on their own
 *   from billing's invoice events (see billing-bridge.ts), so billing need not call them.
 * - `packagesOfCustomer(customerId)`, `portalPackageView(row)`, `usablePackageFor(customerId, serviceId, petId?)`,
 *   `usePackage(packageId)`.
 * - `redeemPoints(customerId, points, invoice)`: billing's points discount (409 insufficient_points).
 * - `couponDiscountFor(code, invoice)`, `recordCouponRedemption(...)`, `releaseCouponRedemption(invoiceId)`,
 *   `findCoupon(code)`: billing's coupon discount.
 *
 * Integrations
 * - `fireWebhookEvent(eventType, data)`: queue a delivery to every webhook subscribed to the event.
 * - `lineFollowerOf(customerId)`, `canReceiveLine(customerId)`: the customer's LINE address (from the
 *   customer row's `line`); `followerOf(lineUserId)`: a follower of the clinic's account.
 */

export function notifyCustomer(
  customerId: string,
  message: { title: string; body: string; channel?: 'auto' | Channel; template?: string; branchId?: string },
): DispatchResult {
  return dispatch(customerId, message.template ?? 'clinic.message', { message: message.body }, {
    channel: message.channel ?? 'auto',
    title: message.title,
    body: message.body,
    branchId: message.branchId,
  });
}

export function notifyTemplate(
  customerId: string,
  templateKey: string,
  values: Record<string, string | undefined>,
  options: { channel?: 'auto' | Channel; branchId?: string; staffId?: string; source?: 'system' | 'manual' } = {},
): DispatchResult {
  if (!templateOf(templateKey)) console.warn(`[mock] crm: unknown template ${templateKey}`);
  return dispatch(customerId, templateKey, values, { channel: options.channel ?? 'auto', branchId: options.branchId, staffId: options.staffId, source: options.source ?? 'system', batchId: newBatchId() });
}

export function unreadCountOf(customerId: string): number {
  return inbox().where((n) => n.customer_id === customerId && !n.read_at).length;
}

/** The newest inbox items, as `InboxItem`. */
export function latestNotificationsOf(customerId: string, count = 3) {
  return inbox()
    .where((n) => n.customer_id === customerId)
    .sort((a, b) => b.sent_at.localeCompare(a.sent_at))
    .slice(0, count)
    .map((n) => ({ id: n.id, template: n.template, title: n.title, body: n.body, sent_at: n.sent_at, read_at: n.read_at ?? null }));
}

/** Writes an inbox row only, for something the owner reads in the portal and nowhere else. */
export function addInboxItem(customerId: string, item: { template: string; title: string; body: string }): Row {
  const row = inbox().insert({ customer_id: customerId, ...item, sent_at: nowIso(), read_at: null });
  emit('notifications', { customer_id: customerId });
  return row;
}

export { threadOfCustomer, customerWrites, staffReplies } from './threads';
export {
  addPoints,
  balanceOf,
  earnPointsForInvoice,
  loyaltyView,
  packagesOfCustomer,
  portalPackageView,
  redeemPoints,
  reversePointsForInvoice,
  tierOfCustomer,
  usablePackageFor,
  usePackage,
} from './loyalty';
export { couponDiscountFor, findCoupon, recordCouponRedemption, releaseCouponRedemption } from './coupons';
export { fireWebhookEvent, followerOf } from './integration-routes';
export { canReceiveLine, lineFollowerOf } from './notify';
