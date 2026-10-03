import { staffApi } from '@/lib/api/server-client';
import type { QueueTicket } from '../types/queue.types';

/**
 * `POST /staff/queue/{id}/leave` — the owner gave up waiting. Needs `queue:write`.
 * A contract extension: see docs/contract-extensions.md § 2.
 */
export function leaveQueue(ticketId: string, version: number, reason?: string): Promise<QueueTicket> {
  return staffApi().post<QueueTicket>(`/staff/queue/${encodeURIComponent(ticketId)}/leave`, {
    ifMatch: version,
    body: { reason },
  });
}
