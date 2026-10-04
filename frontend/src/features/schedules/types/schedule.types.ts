/** One working block. A shift with a break comes back as two, before and after it. */
export interface Shift {
  id: string;
  staff_id: string;
  branch_id: string;
  starts_at: string;
  ends_at: string;
}

/** One stretch of leave. */
export interface TimeOff {
  id: string;
  staff_id: string;
  starts_at: string;
  ends_at: string;
  reason?: string;
}

/** One person's part of the roster. */
export interface StaffSchedule {
  staff_id: string;
  display_name: string;
  department: string;
  shifts: Shift[];
  time_off: TimeOff[];
}

/** `GET /staff/schedules?from=&to=` — only people with a shift or leave in the span. */
export interface ScheduleCalendar {
  from: string;
  to: string;
  staff: StaffSchedule[];
}

/** `POST /staff/schedules/shifts`. Times are instants with the clinic's offset. */
export interface NewShiftsInput {
  staff_id: string;
  branch_id: string;
  starts_at: string;
  ends_at: string;
  break?: { starts_at: string; ends_at: string };
  rrule?: string;
}

/** `POST /staff/schedules/time-off`. */
export interface TimeOffInput {
  staff_id: string;
  starts_at: string;
  ends_at: string;
  reason?: string;
}

/** An appointment already booked inside the leave, and who could take it instead. */
export interface TimeOffConflict {
  appointment_id: string;
  branch_id: string;
  service_id: string;
  starts_at: string;
  ends_at: string;
  suggested_staff_ids: string[];
}

/** What recording leave did. Nothing is moved; the conflicts are for a person to sort out. */
export interface TimeOffResult {
  time_off: TimeOff;
  shifts_cancelled: number;
  shifts_remaining: Shift[];
  conflicts: TimeOffConflict[];
}

/**
 * One case-assignment rule: cases for this species and/or service go to these people first, in
 * this order. At least one of `species` and `service_id` is set.
 */
export interface AssignmentRule {
  species?: string | null;
  service_id?: string | null;
  staff_ids: string[];
}

/** `PUT /staff/schedules/assignment-rules` answers the whole stored set. There is no `GET`. */
export interface AssignmentRules {
  version: number;
  rules: AssignmentRule[];
}
