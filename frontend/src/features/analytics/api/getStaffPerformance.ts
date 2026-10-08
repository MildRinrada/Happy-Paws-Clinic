import { staffApi } from '@/lib/api/server-client';
import type { StaffPerformance, ReportQuery } from '../types/analytics.types';

/** `GET /staff/analytics/staff-performance` (analytics:hr). Days are clinic days, both ends included. */
export function getStaffPerformance({ from, to, branchId }: ReportQuery): Promise<StaffPerformance> {
  return staffApi().get<StaffPerformance>('/staff/analytics/staff-performance', {
    query: { from, to, branch: branchId },
  });
}
