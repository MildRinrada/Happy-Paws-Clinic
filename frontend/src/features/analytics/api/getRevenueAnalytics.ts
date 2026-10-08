import { staffApi } from '@/lib/api/server-client';
import type { RevenueAnalytics, ReportQuery } from '../types/analytics.types';

/** `GET /staff/analytics/revenue` (analytics:read). Days are clinic days, both ends included. */
export function getRevenueAnalytics({ from, to, branchId, granularity }: ReportQuery): Promise<RevenueAnalytics> {
  return staffApi().get<RevenueAnalytics>('/staff/analytics/revenue', {
    query: { from, to, branch: branchId, granularity },
  });
}
