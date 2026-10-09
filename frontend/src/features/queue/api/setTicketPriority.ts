import { staffApi } from '@/lib/api/server-client';
import type { QueueTicket, TicketPriority } from '../types/queue.types';

export interface SetTicketPriorityInput {
  ticketId: string;
  version: number;
  priority: TicketPriority;
  /** Required by the backend for anything above `normal` (`409 invalid_priority` without it). */
  reason?: string;
}

/**
 * `POST /staff/queue/{id}/priority` — raise or lower a ticket's urgency after it was issued.
 * Needs `queue:write`. A contract extension: see docs/contract-extensions.md § 1. Audited.
 */
export function setTicketPriority(input: SetTicketPriorityInput): Promise<QueueTicket> {
  return staffApi().post<QueueTicket>(`/staff/queue/${encodeURIComponent(input.ticketId)}/priority`, {
    ifMatch: input.version,
    body: { priority: input.priority, reason: input.reason },
  });
}
