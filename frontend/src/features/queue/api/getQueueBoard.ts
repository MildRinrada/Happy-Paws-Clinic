import { staffApi } from '@/lib/api/server-client';
import type { QueueBoard } from '../types/queue.types';

/** `GET /staff/queue?branch=` — "คิวทั้งหมดวันนี้พร้อมสถานะแต่ละใบ". Needs `queue:read`. */
export function getQueueBoard(branchId: string): Promise<QueueBoard> {
  return staffApi().get<QueueBoard>('/staff/queue', { query: { branch: branchId } });
}
