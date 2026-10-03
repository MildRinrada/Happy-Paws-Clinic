import { staffApi } from '@/lib/api/server-client';
import type { QueueTicket } from '../types/queue.types';

export interface AssignTicketInput {
  ticketId: string;
  version: number;
  /** Either may be sent alone: a room change does not require re-stating the vet. */
  staffId?: string;
  roomId?: string;
}

/** `POST /staff/queue/{id}/assign` — "จ่ายหมอและห้องตรวจ · ย้ายคิวข้ามห้องได้". Needs `queue:write`. */
export function assignTicket(input: AssignTicketInput): Promise<QueueTicket> {
  return staffApi().post<QueueTicket>(`/staff/queue/${encodeURIComponent(input.ticketId)}/assign`, {
    ifMatch: input.version,
    body: { staff_id: input.staffId, room_id: input.roomId },
  });
}
