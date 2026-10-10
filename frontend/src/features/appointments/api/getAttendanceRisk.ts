import { staffApi } from '@/lib/api/server-client';
import type { AttendanceRisk } from '../types/attendance-risk.types';

/**
 * `GET /staff/appointments/attendance-risk?branch=&date=`: each open booking of the day with its
 * risk of not being attended and the reasons. Needs `appointment:read`. Today or later only.
 * docs/contract-extensions.md § 33.
 */
export function getAttendanceRisk(date: string, branch: string): Promise<AttendanceRisk> {
  return staffApi().get<AttendanceRisk>('/staff/appointments/attendance-risk', { query: { date, branch } });
}
