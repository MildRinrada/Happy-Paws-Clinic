import { staffApi } from '@/lib/api/server-client';
import type { QueueBoard, TicketStatus } from '../types/queue.types';

export interface QueueBoardQuery {
  /** Clinic-local day, `YYYY-MM-DD`. Left out, the backend uses today in clinic time. */
  date?: string;
  /** Adds `served` and `left` tickets, for looking back over a day. */
  includeFinished?: boolean;
  /** Exactly these statuses, e.g. `['served', 'left']` (docs/contract-extensions.md § 31). */
  statuses?: readonly TicketStatus[];
}

/**
 * `GET /staff/queue?branch=&date=&include_finished=&status=` — "คิวทั้งหมดวันนี้พร้อมสถานะแต่ละใบ".
 * Needs `queue:read`. `date`, `include_finished` and `status` are what the history view and its
 * filters are built on.
 */
export function getQueueBoard(branchId: string, query: QueueBoardQuery = {}): Promise<QueueBoard> {
  return staffApi().get<QueueBoard>('/staff/queue', {
    query: {
      branch: branchId,
      date: query.date,
      include_finished: query.includeFinished ? true : undefined,
      status: query.statuses && query.statuses.length > 0 ? query.statuses.join(',') : undefined,
    },
  });
}
