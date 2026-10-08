import type { Appointment } from './appointment.types';

/** The five problems `GET /staff/appointments/conflicts` reports. Rooms are not checked. */
export type ConflictKind =
  | 'clinic_closed'
  | 'staff_off_shift'
  | 'staff_on_time_off'
  | 'staff_double_booked'
  | 'pet_double_booked';

export interface AppointmentConflict {
  kind: ConflictKind | string;
  /** The backend's own Thai sentence, e.g. "หมอลาในช่วงนี้: ป่วย". */
  message: string;
  starts_at: string;
  ends_at: string;
  appointment_id?: string | null;
  staff_id?: string | null;
  conflicting_appointment_id?: string | null;
}

/** `GET /staff/appointments/conflicts`. `clear` is true when nothing was found. */
export interface ConflictReport {
  clear: boolean;
  conflicts: AppointmentConflict[];
}

/** `POST /staff/appointments/recurring`. `rrule` repeats the first visit. */
export interface NewRecurringSeries {
  pet_id: string;
  service_id: string;
  first_starts_at: string;
  rrule: string;
  staff_id?: string;
  branch_id?: string;
  reason?: string;
}

/** Every visit in the course, in order, under one `series_id`. */
export interface RecurringSeries {
  series_id: string;
  appointments: Appointment[];
}

/** One date in a refused course: `details.conflicts[]` on `422 slot_unavailable`. */
export interface SeriesConflict {
  starts_at: string;
  reason: string;
  /** Free `HH:mm` times on that day. */
  suggested: string[];
}

/** `POST /staff/waiting-list`. Without dates the backend waits from today for 14 days. */
export interface NewWaitingListEntry {
  pet_id: string;
  service_id: string;
  earliest_on?: string;
  latest_on?: string;
  branch_id?: string;
  staff_id?: string;
  note?: string;
}

/** One entry on the waiting list, as the backend stored it. */
export interface WaitingListEntry {
  id: string;
  customer_id: string;
  pet_id: string;
  service_id: string;
  branch_id?: string | null;
  staff_id?: string | null;
  earliest_on: string;
  latest_on: string;
  note?: string | null;
  added_at: string;
  last_notified_at?: string | null;
  notified_count: number;
}
