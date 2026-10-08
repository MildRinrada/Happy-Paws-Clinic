import { staffApi } from '@/lib/api/server-client';
import type { RetentionAnalytics, ReportQuery } from '../types/analytics.types';

/** `GET /staff/analytics/retention` (analytics:read). Days are clinic days, both ends included. */
export function getRetentionAnalytics({ from, to, branchId }: ReportQuery): Promise<RetentionAnalytics> {
  return staffApi().get<RetentionAnalytics>('/staff/analytics/retention', {
    query: { from, to, branch: branchId },
  });
}
