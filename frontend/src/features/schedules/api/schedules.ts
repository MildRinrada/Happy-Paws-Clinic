import { staffApi } from '@/lib/api/server-client';
import type { NewShiftsInput, ScheduleCalendar, Shift, TimeOffInput, TimeOffResult } from '../types/schedule.types';

/**
 * The roster: read on `schedule:read`, written on `schedule:write`. What is written here is what
 * booking can offer. The contract has no way to remove a shift; leave is how time comes off.
 */

/** `GET /staff/schedules?from=&to=&branch=` — clinic days, both ends included. */
export function getScheduleCalendar(from: string, to: string, branchId?: string): Promise<ScheduleCalendar> {
  return staffApi().get<ScheduleCalendar>('/staff/schedules', { query: { from, to, branch: branchId } });
}

/** `POST /staff/schedules/shifts` — one shift, or a run of them with an RRULE. All or nothing. */
export async function createShifts(input: NewShiftsInput): Promise<Shift[]> {
  const { data } = await staffApi().post<{ data: Shift[] }>('/staff/schedules/shifts', { body: input });
  return data;
}

/** `POST /staff/schedules/time-off` — takes effect at once and lists the appointments in the way. */
export function recordTimeOff(input: TimeOffInput): Promise<TimeOffResult> {
  return staffApi().post<TimeOffResult>('/staff/schedules/time-off', { body: input });
}
