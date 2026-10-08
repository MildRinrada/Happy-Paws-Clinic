export type AppointmentStatus =
  | 'draft'
  | 'booked'
  | 'confirmed'
  | 'checked_in'
  | 'in_consultation'
  | 'completed'
  | 'cancelled'
  | 'no_show';

/** One appointment as the API returns it, to staff and to the owner alike. */
export interface Appointment {
  id: string;
  version: number;
  status: AppointmentStatus;
  branch_id: string;
  customer_id: string;
  pet_id: string;
  service_id: string;
  staff_id?: string | null;
  starts_at: string;
  ends_at: string;
  reason?: string | null;
  walk_in: boolean;
  confirmed_at?: string | null;
  checked_in_at?: string | null;
  cancelled_at?: string | null;
  cancellation_reason?: string | null;
  type: string;
  series_id?: string | null;
  late_cancellation?: boolean;
}

/** `POST /staff/appointments`. The customer is the pet's primary owner, set by the server. */
export interface NewStaffAppointment {
  pet_id: string;
  service_id: string;
  starts_at: string;
  staff_id?: string;
  branch_id?: string;
  reason?: string;
}

/**
 * A customer's missed and late-cancelled visits, counted from the calendar. `fee_due_satang` is
 * what the latest strike carries under the cancellation policy: reported, never charged on its own.
 */
export interface CustomerAttendance {
  customer_id: string;
  no_shows: number;
  late_cancellations: number;
  strikes_in_window: number;
  window_days: number;
  free_allowance: number;
  fee_due_satang: number;
}

/** `POST /staff/appointments/{id}/no-show` — the changed appointment and where the customer now stands. */
export interface NoShowResult {
  appointment: Appointment;
  attendance: CustomerAttendance;
}
