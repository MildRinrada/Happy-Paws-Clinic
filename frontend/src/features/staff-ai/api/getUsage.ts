import { staffApi } from '@/lib/api/server-client';
import type { AiUsage } from '../types/staff-ai.types';

/** `GET /staff/ai/usage`: whether the assistant is on, and this month's allowance. Needs `ai:use`. */
export function getUsage(): Promise<AiUsage> {
  return staffApi().get<AiUsage>('/staff/ai/usage');
}
