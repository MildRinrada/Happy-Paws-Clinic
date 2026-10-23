import { singleton, table, type Row } from '../../core/db';

/**
 * This module's tables. Rows are kept in the shapes the product reads
 * (`features/appointments/types`, `features/booking/types`, `features/queue/types`), plus the few
 * internal fields named here, so other modules reading `appointments` or `queue_tickets` get
 * exactly what the API answers.
 */

export type AppointmentStatus =
  | 'draft'
  | 'booked'
  | 'confirmed'
  | 'checked_in'
  | 'in_consultation'
  | 'completed'
  | 'cancelled'
  | 'no_show';

export interface AppointmentRow extends Row {
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
  /** Internal: when it was booked, for "days booked ahead" in the attendance risk. */
  created_at: string;
  /** Internal: who booked it, `staff` at the counter or `online` by the owner. */
  source?: 'staff' | 'online';
  /** Internal: when a no-show was recorded. */
  no_show_at?: string | null;
}

export interface HoldRow extends Row {
  branch_id: string;
  service_id: string;
  staff_id: string;
  starts_at: string;
  ends_at: string;
  expires_at: string;
  /** Internal: the signed-in customer, or `guest:<phone>` for a guest booking not yet proved. */
  holder: string;
  consumed: boolean;
}

export type TicketStatus = 'waiting' | 'called' | 'in_room' | 'served' | 'skipped' | 'left';
export type TicketPriority = 'normal' | 'urgent' | 'emergency';

export interface TicketRow extends Row {
  version: number;
  number: string;
  branch_id: string;
  service_date: string;
  status: TicketStatus;
  priority: TicketPriority;
  pet_id: string;
  customer_id: string;
  service_id: string;
  appointment_id?: string;
  staff_id?: string;
  room_id?: string;
  skip_count: number;
  issued_at: string;
  called_at?: string;
  priority_reason?: string;
  entered_room_at?: string;
  served_at?: string;
  left_at?: string;
  left_reason?: string;
  /** Internal: the running number of the day, for the call order. */
  sequence: number;
  /** Internal: when it was last sent to the back of the line. */
  skipped_at?: string;
}

export interface WaitingRow extends Row {
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

export interface ScreenRow extends Row {
  name: string;
  branch_id: string;
  created_by_staff_id: string;
  created_at: string;
  setup_code?: string | null;
  setup_expires_at?: string;
  claimed_at?: string;
  expires_at?: string;
  last_seen_at?: string;
  revoked_at?: string;
}

export const appointments = () => table<AppointmentRow>('appointments');
export const holds = () => table<HoldRow>('booking_holds');
export const tickets = () => table<TicketRow>('queue_tickets');
export const waitingList = () => table<WaitingRow>('waiting_list');
export const screens = () => table<ScreenRow>('display_screens');
/** Idempotency keys already answered: the same key on a retry gets the first answer back. */
export const replays = () => table<Row & { key: string; scope: string; result_id: string }>('appointment_replays');

// ---- Rule sets --------------------------------------------------------------------------------------

export interface AppointmentType {
  code: string;
  name: string;
  duration_minutes?: number | null;
  online_booking: boolean;
  lead_time_minutes?: number | null;
  queue_priority: TicketPriority;
}

export const appointmentTypes = singleton<{ version: number; types: AppointmentType[] }>('appointment_types', () => ({
  version: 1,
  types: [
    { code: 'regular', name: 'นัดปกติ', duration_minutes: null, online_booking: true, lead_time_minutes: null, queue_priority: 'normal' },
    { code: 'follow_up', name: 'ติดตามผล', duration_minutes: null, online_booking: true, lead_time_minutes: null, queue_priority: 'normal' },
    { code: 'emergency', name: 'ฉุกเฉิน', duration_minutes: null, online_booking: false, lead_time_minutes: null, queue_priority: 'emergency' },
    { code: 'surgery', name: 'ผ่าตัด', duration_minutes: 90, online_booking: false, lead_time_minutes: null, queue_priority: 'normal' },
  ],
}));

export const cancellationPolicy = singleton<{ version: number; cutoff_hours: number; fee_satang: number; free_allowance: number; window_days: number }>(
  'cancellation_policy',
  () => ({ version: 1, cutoff_hours: 24, fee_satang: 20000, free_allowance: 1, window_days: 365 }),
);

export function typeOf(code: string | null | undefined): AppointmentType | undefined {
  return appointmentTypes.get().types.find((type) => type.code === (code || 'regular'));
}

/** Strips the internal fields: what the API answers for an appointment. */
export function appointmentDto(row: AppointmentRow) {
  const { created_at: _created, source: _source, no_show_at: _noShow, ...dto } = row;
  return dto;
}

export function ticketDto(row: TicketRow) {
  const { sequence: _sequence, skipped_at: _skipped, ...dto } = row;
  return dto;
}

export function holdDto(row: HoldRow) {
  return {
    id: row.id,
    branch_id: row.branch_id,
    service_id: row.service_id,
    staff_id: row.staff_id,
    starts_at: row.starts_at,
    ends_at: row.ends_at,
    expires_at: row.expires_at,
  };
}
