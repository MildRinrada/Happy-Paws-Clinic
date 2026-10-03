import { staffApi } from '@/lib/api/server-client';
import type { Appointment } from '../types/appointment.types';

/** What a check-in answers: the appointment, now `checked_in`, and the queue number it was given. */
export interface CheckInResult {
  appointment: Appointment;
  ticket: { id: string; number: string };
}

/**
 * `POST /appointments/{id}/check-in` — "เช็กอินที่เคาน์เตอร์หรือลูกค้าเช็กอินเอง". Issues a queue
 * ticket, so the contract requires an idempotency key: a double tap must not queue the pet twice.
 * Whether it is too early, or the appointment is not today, is the backend's to refuse.
 */
export function checkInAppointment(appointmentId: string, idempotencyKey: string): Promise<CheckInResult> {
  return staffApi().post<CheckInResult>(`/appointments/${encodeURIComponent(appointmentId)}/check-in`, { idempotencyKey });
}
