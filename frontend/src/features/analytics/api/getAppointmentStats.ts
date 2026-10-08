import { staffApi } from '@/lib/api/server-client';
import type { AppointmentStats, ReportQuery } from '../types/analytics.types';

/** `GET /staff/stats/appointments` (stats:read). Days are clinic days, both ends included. */
export function getAppointmentStats({ from, to, branchId, granularity }: ReportQuery): Promise<AppointmentStats> {
  return staffApi().get<AppointmentStats>('/staff/stats/appointments', {
    query: { from, to, branch: branchId, granularity },
  });
}
