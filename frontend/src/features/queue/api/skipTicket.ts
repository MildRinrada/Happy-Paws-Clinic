import { staffApi } from '@/lib/api/server-client';
import type { QueueTicket } from '../types/queue.types';

/** `POST /staff/queue/{id}/skip` — "ข้ามคิวที่ยังไม่มา · เลื่อนไปท้ายแถว". Needs `queue:call`. */
export function skipTicket(ticketId: string, version: number): Promise<QueueTicket> {
  return staffApi().post<QueueTicket>(`/staff/queue/${encodeURIComponent(ticketId)}/skip`, {
    ifMatch: version,
  });
}
