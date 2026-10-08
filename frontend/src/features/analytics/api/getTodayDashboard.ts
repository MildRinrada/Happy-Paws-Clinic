import { staffApi } from '@/lib/api/server-client';
import type { TodayDashboard } from '../types/analytics.types';

/** `GET /staff/dashboard/today` (dashboard:read). Today's cards at one branch. */
export function getTodayDashboard(branchId?: string): Promise<TodayDashboard> {
  return staffApi().get<TodayDashboard>('/staff/dashboard/today', { query: { branch: branchId } });
}
