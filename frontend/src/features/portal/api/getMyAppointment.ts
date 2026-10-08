import type { Appointment } from '@/features/appointments/types/appointment.types';
import { staffApi } from '@/lib/api/server-client';

/**
 * `GET /appointments/{id}` — one appointment. Shared with staff; for an owner the backend answers
 * only their own pets' appointments and `404` for anyone else's.
 */
export function getMyAppointment(appointmentId: string): Promise<Appointment> {
  return staffApi().get<Appointment>(`/appointments/${encodeURIComponent(appointmentId)}`);
}
