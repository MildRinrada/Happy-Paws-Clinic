import type { Appointment } from '@/features/appointments/types/appointment.types';
import { staffApi } from '@/lib/api/server-client';

/**
 * `POST /appointments/{id}/cancel` — never refused for being late, only counted: an owner who
 * cancels inside the clinic's policy cut-off gets `late_cancellation: true` on the answer.
 */
export function cancelAppointment(appointmentId: string, reason?: string): Promise<Appointment> {
  return staffApi().post<Appointment>(`/appointments/${encodeURIComponent(appointmentId)}/cancel`, {
    body: { reason: reason || undefined },
  });
}
