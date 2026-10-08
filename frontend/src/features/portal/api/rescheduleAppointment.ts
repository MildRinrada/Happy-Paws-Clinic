import type { Appointment } from '@/features/appointments/types/appointment.types';
import { staffApi } from '@/lib/api/server-client';

export interface RescheduleInput {
  appointmentId: string;
  /** The `version` the page read. A change made since (say, by the counter) answers `409`. */
  version: number;
  startsAt: string;
  /** Left out to keep the same vet. */
  staffId?: string;
}

/**
 * `POST /appointments/{id}/reschedule` — moves the appointment, checking the new time as a booking
 * would. For an owner it is refused with `409 cutoff_passed` once the clinic's change cut-off has
 * passed. A moved appointment goes back to `booked`: the old confirmation does not carry over.
 */
export function rescheduleAppointment({ appointmentId, version, startsAt, staffId }: RescheduleInput): Promise<Appointment> {
  return staffApi().post<Appointment>(`/appointments/${encodeURIComponent(appointmentId)}/reschedule`, {
    ifMatch: version,
    body: { starts_at: startsAt, staff_id: staffId },
  });
}
