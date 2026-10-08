import { staffApi } from '@/lib/api/server-client';
import type { QueueStats, ReportQuery } from '../types/analytics.types';

/** `GET /staff/stats/queue` (stats:read). Days are clinic days, both ends included. */
export function getQueueStats({ from, to, branchId }: ReportQuery): Promise<QueueStats> {
  return staffApi().get<QueueStats>('/staff/stats/queue', {
    query: { from, to, branch: branchId },
  });
}
