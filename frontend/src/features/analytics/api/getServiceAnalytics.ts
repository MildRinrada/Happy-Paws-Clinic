import { staffApi } from '@/lib/api/server-client';
import type { ServiceAnalytics, ReportQuery } from '../types/analytics.types';

/** `GET /staff/analytics/services` (analytics:read). Days are clinic days, both ends included. */
export function getServiceAnalytics({ from, to, branchId }: ReportQuery): Promise<ServiceAnalytics> {
  return staffApi().get<ServiceAnalytics>('/staff/analytics/services', {
    query: { from, to, branch: branchId },
  });
}
