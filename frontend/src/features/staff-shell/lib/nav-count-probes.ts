import { awaitingReplies, stockProblems } from '@/features/analytics/lib/attention-probes';
import type { AttentionItem } from '@/features/analytics/types/today.types';
import { listLabRequests } from '@/features/laboratory/api/lab-requests';
import { getQueueBoard } from '@/features/queue/api/getQueueBoard';
import type { NavCount } from '../types/staff-shell.types';

/**
 * One read per menu badge. Each answers the count, or `null` when nothing is waiting, and throws
 * when it cannot tell. Messages and stock are the overview's own "ต้องจัดการตอนนี้" reads, so the
 * menu and the overview never disagree; the queue counts every ticket still to be called, not only
 * the long waits the overview flags.
 */

function asCount(item: AttentionItem | null): NavCount | null {
  return item ? { count: item.count, more: item.more } : null;
}

/** Tickets in the board's "รอเรียก" column: `waiting` and `skipped`. */
export async function waitingTickets(branchId: string): Promise<NavCount | null> {
  const board = await getQueueBoard(branchId);
  const count = board.tickets.filter((ticket) => ticket.status === 'waiting' || ticket.status === 'skipped').length;

  return count > 0 ? { count, more: false } : null;
}

/** Conversations whose last word is the customer's. */
export async function unansweredThreads(): Promise<NavCount | null> {
  return asCount(await awaitingReplies());
}

/** Stock that can stop a prescription: out of stock, expired or near expiry. */
export async function stockAlerts(branchId: string): Promise<NavCount | null> {
  return asCount(await stockProblems(branchId));
}

/**
 * Lab requests waiting on whoever is signed in: with `lab:write`, the samples still waiting on the
 * lab at this branch; otherwise the signed-in vet's own results nobody has read yet. One page of
 * 100, so a busier list shows "100+".
 */
export async function labWork(branchId: string, entersResults: boolean): Promise<NavCount | null> {
  const page = await listLabRequests({
    status: entersResults ? 'pending' : 'resulted',
    requestedBy: entersResults ? undefined : 'me',
    branchId,
    limit: 100,
  });

  return page.data.length > 0 ? { count: page.data.length, more: Boolean(page.page.next_cursor) } : null;
}
