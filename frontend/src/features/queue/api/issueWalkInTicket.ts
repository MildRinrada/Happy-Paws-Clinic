import { staffApi } from '@/lib/api/server-client';
import type { QueueTicket, TicketPriority } from '../types/queue.types';

export interface IssueWalkInTicketInput {
  petId: string;
  serviceId: string;
  branchId: string;
  priority: TicketPriority;
  priorityReason?: string;
  /** The same key on a retry returns the ticket already issued instead of printing a second. */
  idempotencyKey: string;
}

/** `POST /staff/queue/tickets` — "ออกบัตรคิวให้ walk-in ที่ยังไม่มีนัด". Needs `queue:write`. */
export function issueWalkInTicket(input: IssueWalkInTicketInput): Promise<QueueTicket> {
  return staffApi().post<QueueTicket>('/staff/queue/tickets', {
    idempotencyKey: input.idempotencyKey,
    body: {
      pet_id: input.petId,
      service_id: input.serviceId,
      branch_id: input.branchId,
      priority: input.priority,
      priority_reason: input.priorityReason,
    },
  });
}
