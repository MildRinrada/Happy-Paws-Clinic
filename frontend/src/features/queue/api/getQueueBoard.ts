import { staffApi } from '@/lib/api/server-client';
import type { QueueBoard } from '../types/queue.types';

export interface QueueBoardQuery {
  /** Clinic-local day, `YYYY-MM-DD`. Left out, the backend uses today in clinic time. */
  date?: string;
  /** Adds `served` and `left` tickets, for looking back over a day. */
  includeFinished?: boolean;
}

/**
 * `GET /staff/queue?branch=&date=&include_finished=` — "คิวทั้งหมดวันนี้พร้อมสถานะแต่ละใบ".
 * Needs `queue:read`. `date` and `include_finished` are what the history view and its date
 * picker are built on; the backend has no status filter, so narrowing by status is the caller's.
 */
export function getQueueBoard(branchId: string, query: QueueBoardQuery = {}): Promise<QueueBoard> {
  return staffApi().get<QueueBoard>('/staff/queue', {
    query: {
      branch: branchId,
      date: query.date,
      include_finished: query.includeFinished ? true : undefined,
    },
  });
}
