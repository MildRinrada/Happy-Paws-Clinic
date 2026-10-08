import { staffApi } from '@/lib/api/server-client';
import type { UpcomingReminders } from '../types/automation.types';

/** `GET /staff/reminders/upcoming` (automation:read). What the switched-on rules will send, earliest first. */
export function listUpcomingReminders(days: number, branchId?: string): Promise<UpcomingReminders> {
  return staffApi().get<UpcomingReminders>('/staff/reminders/upcoming', { query: { days, branch: branchId } });
}
