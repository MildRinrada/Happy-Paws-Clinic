/** One working block. A shift with a break comes back as two, before and after it. */
export interface Shift {
  id: string;
  staff_id: string;
  branch_id: string;
  starts_at: string;
  ends_at: string;
  /** Sent back as `If-Match` when changing or removing the block (docs/contract-extensions.md § 47). */
  version: number;
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

/** The whole stored set, as `GET` (§ 48) and `PUT /staff/schedules/assignment-rules` answer it. */
export interface AssignmentRules {
  version: number;
  rules: AssignmentRule[];
}

/** `PATCH /staff/schedules/shifts/{id}` (§ 47): the block at its new times, both ends. */
export interface ShiftTimesInput {
  starts_at: string;
  ends_at: string;
}

/** What changing a block did: the block now, and the appointments in the time it no longer covers. */
export interface ShiftChangeResult {
  shift: Shift;
  conflicts: TimeOffConflict[];
}

/** What removing a block did: which block, and the appointments that were in it. */
export interface ShiftRemoved {
  shift_id: string;
  conflicts: TimeOffConflict[];
}

/** `POST /staff/schedules/shifts/copy-week` (§ 49). Weeks are named by their Monday. */
export interface CopyWeekInput {
  branch_id: string;
  from_week: string;
  to_week: string;
  staff_ids?: string[];
}

/** The copies made, and how many were left out because their day had already begun. */
export interface CopyWeekResult {
  data: Shift[];
  skipped_started: number;
}

/**
 * One clash in a refusal (`details.conflicts` of `shift_overlap`, `details.time_off` of
 * `staff_on_time_off`): whose, and when.
 */
export interface ShiftClash {
  staff_id?: string;
  shift_id?: string;
  time_off_id?: string;
  starts_at: string;
  ends_at: string;
}

/** What the roster page knows about the week before, to say what a copy would do before anyone presses it. */
export interface CopyWeekPlan {
  /** Mondays, `YYYY-MM-DD`. */
  fromWeek: string;
  toWeek: string;
  /** "6 ต.ค. - 12 ต.ค.", for the sentence. */
  fromLabel: string;
  toLabel: string;
  /** Shifts on the week before, a shift with a break counted once. */
  shifts: number;
  people: number;
}
