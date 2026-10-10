import { staffApi } from '@/lib/api/server-client';
import type { DailyBrief } from '../types/staff-ai.types';

/** `GET /staff/ai/daily-brief?branch_id=`. Needs `dashboard:read`. Reading never counts a use. */
export function getDailyBrief(branchId: string): Promise<DailyBrief> {
  return staffApi().get<DailyBrief>('/staff/ai/daily-brief', { query: { branch_id: branchId } });
}
