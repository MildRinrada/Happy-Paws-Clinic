import { staffApi } from '@/lib/api/server-client';
import type {
  AssignmentRule,
  AssignmentRules,
  CopyWeekInput,
  CopyWeekResult,
  NewShiftsInput,
  ScheduleCalendar,
  Shift,
  ShiftChangeResult,
  ShiftRemoved,
  ShiftTimesInput,
  TimeOffInput,
  TimeOffResult,
} from '../types/schedule.types';

/**
 * The roster: read on `schedule:read`, written on `schedule:write`. What is written here is what
 * booking can offer. Changing, removing and copying shifts, and reading the rules back, are not in
 * the contract (docs/contract-extensions.md § 47 to § 49).
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

/** `PATCH /staff/schedules/shifts/{id}` (§ 47): one block at new times, guarded by its version. */
export function changeShift(id: string, times: ShiftTimesInput, version: number): Promise<ShiftChangeResult> {
  return staffApi().patch<ShiftChangeResult>(`/staff/schedules/shifts/${encodeURIComponent(id)}`, { body: times, ifMatch: version });
}

/** `DELETE /staff/schedules/shifts/{id}` (§ 47): one block off the roster. Answers the appointments that were in it. */
export function removeShift(id: string, version: number): Promise<ShiftRemoved> {
  return staffApi().delete<ShiftRemoved>(`/staff/schedules/shifts/${encodeURIComponent(id)}`, { ifMatch: version });
}

/** `POST /staff/schedules/shifts/copy-week` (§ 49): a week of the branch onto another, all or nothing. */
export function copyWeek(input: CopyWeekInput): Promise<CopyWeekResult> {
  return staffApi().post<CopyWeekResult>('/staff/schedules/shifts/copy-week', { body: input });
}

/** `GET /staff/schedules/assignment-rules` (§ 48): the stored set and its version, on `schedule:read`. */
export function getAssignmentRules(): Promise<AssignmentRules> {
  return staffApi().get<AssignmentRules>('/staff/schedules/assignment-rules');
}

/**
 * `PUT /staff/schedules/assignment-rules` — "กติกาจ่ายเคสอัตโนมัติ". Replaces the whole set; an
 * empty list removes every rule. `version` is the one read with the rules, so a save over someone
 * else's answers `409 version_conflict` instead of quietly replacing it.
 */
export function replaceAssignmentRules(rules: AssignmentRule[], version?: number): Promise<AssignmentRules> {
  return staffApi().put<AssignmentRules>('/staff/schedules/assignment-rules', { body: { rules }, ifMatch: version });
}
