import { staffApi } from '@/lib/api/server-client';
import type { QueueTicket } from '../types/queue.types';

/** `POST /staff/queue/{id}/call` — "เรียกคิว · ยิงแจ้งเตือน LINE ให้เจ้าของ". Needs `queue:call`. */
export function callTicket(ticketId: string, version: number): Promise<QueueTicket> {
  return staffApi().post<QueueTicket>(`/staff/queue/${encodeURIComponent(ticketId)}/call`, {
    ifMatch: version,
  });
}
