import { appointmentPaths } from '@/features/appointments/lib/appointment-paths';
import type { AttendanceRisk } from '@/features/appointments/types/attendance-risk.types';
import { listInventoryAlerts } from '@/features/inventory/api/listInventoryAlerts';
import { listThreads } from '@/features/messaging/api/listThreads';
import { messagesHref } from '@/features/messaging/lib/messages-query';
import { getQueueBoard } from '@/features/queue/api/getQueueBoard';
import { listStaffReviews } from '@/features/reviews/api/staffReviews';
import { reviewPaths } from '@/features/reviews/lib/review-paths';
import { minutesSince } from '@/lib/format/datetime';
import { attentionBox } from '../content/attention-box';
import type { AttentionItem } from '../types/today.types';
import { formatCount } from './format-report';
import { LIST_CEILING, LONG_WAIT_MINUTES } from './today-settings';

/**
 * One read per line of "ต้องจัดการตอนนี้". Each answers the line, or `null` when nothing is waiting,
 * and throws when it cannot tell; the caller turns a throw into a line left out. Every count is the
 * one the linked page shows, so following the link never finds a different number.
 */

/** Tickets still waiting to be called (`waiting` and `skipped`, as the board counts them) past the threshold. */
export async function longWaits(branchId: string, now: Date): Promise<AttentionItem | null> {
  const board = await getQueueBoard(branchId);
  const waits = board.tickets
    .filter((ticket) => ticket.status === 'waiting' || ticket.status === 'skipped')
    .map((ticket) => minutesSince(ticket.issued_at, now))
    .filter((minutes) => minutes >= LONG_WAIT_MINUTES);

  if (waits.length === 0) return null;

  return {
    key: 'long_wait',
    count: waits.length,
    more: false,
    href: '/queue',
    detail: attentionBox.longest(Math.max(...waits)),
  };
}

/** Conversations whose last word is the customer's. Only the threads are read, no messages. */
export async function awaitingReplies(): Promise<AttentionItem | null> {
  const page = await listThreads({ status: 'awaiting_reply', limit: LIST_CEILING, messages: 0 });
  const count = page.data.filter((thread) => thread.status === 'awaiting_reply').length;

  if (count === 0) return null;

  return { key: 'messages', count, more: !!page.page.next_cursor, href: messagesHref({ status: 'awaiting_reply' }) };
}

/** Reviews written and not yet published or rejected. The moderation page opens on this tab. */
export async function pendingReviews(): Promise<AttentionItem | null> {
  const page = await listStaffReviews({ status: 'pending' });

  if (page.data.length === 0) return null;

  return { key: 'reviews', count: page.data.length, more: !!page.page.next_cursor, href: reviewPaths.staff };
}

/**
 * Stock that can stop a prescription: out of stock, expired, or near expiry within the alerts
 * page's own default window. Low but not out is left to the stock page: it is not today's problem.
 */
export async function stockProblems(branchId: string): Promise<AttentionItem | null> {
  const alerts = await listInventoryAlerts(branchId);
  const out = alerts.filter((alert) => alert.kind === 'out_of_stock').length;
  const expired = alerts.filter((alert) => alert.kind === 'expired').length;
  const near = alerts.filter((alert) => alert.kind === 'near_expiry').length;
  const count = out + expired + near;

  if (count === 0) return null;

  const text = attentionBox.stock;
  const detail = [
    out > 0 ? `${text.out} ${formatCount(out)}` : null,
    expired > 0 ? `${text.expired} ${formatCount(expired)}` : null,
    near > 0 ? `${text.near} ${formatCount(near)}` : null,
  ]
    .filter(Boolean)
    .join(' ');

  return { key: 'stock', count, more: false, href: '/inventory/alerts', detail };
}

/**
 * Today's bookings at medium or high risk of not being attended (docs/contract-extensions.md § 33)
 * that have not started yet, so there is still time to call. The calendar lists the same ones.
 */
export function riskyBookings(risk: AttendanceRisk, today: string, now: Date): AttentionItem | null {
  const count = risk.data.filter((row) => row.risk !== 'low' && Date.parse(row.starts_at) > now.getTime()).length;

  if (count === 0) return null;

  return {
    key: 'attendance',
    count,
    more: false,
    href: appointmentPaths.calendar(today),
    detail: attentionBox.attendance,
  };
}
