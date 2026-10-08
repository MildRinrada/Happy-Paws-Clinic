import { staffApi } from '@/lib/api/server-client';
import type { WorkloadStats, ReportQuery } from '../types/analytics.types';

/** `GET /staff/stats/workload` (stats:read). Days are clinic days, both ends included. */
export function getWorkloadStats({ from, to, branchId }: ReportQuery): Promise<WorkloadStats> {
  return staffApi().get<WorkloadStats>('/staff/stats/workload', {
    query: { from, to, branch: branchId },
  });
}
