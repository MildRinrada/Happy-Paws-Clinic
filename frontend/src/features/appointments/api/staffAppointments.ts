import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { Appointment, NewStaffAppointment, NoShowResult } from '../types/appointment.types';

/**
 * The clinic's own appointment calls. Reading the board needs `appointment:read`; booking,
 * moving, cancelling and no-show need `appointment:write`. The shared `/appointments/{id}/*`
 * routes decide who may act on which appointment themselves.
 */

const path = (id: string, action?: string) =>
  `/appointments/${encodeURIComponent(id)}${action ? `/${action}` : ''}`;

/** `GET /staff/appointments?date=&branch=` — one page of one clinic day, oldest first. */
export function listAppointments(query: {
  date: string;
  branchId: string;
  cursor?: string;
}): Promise<CursorPage<Appointment>> {
  return staffApi().get<CursorPage<Appointment>>('/staff/appointments', {
    query: { date: query.date, branch: query.branchId, cursor: query.cursor, limit: 100 },
  });
}

/** `POST /staff/appointments` — books for the pet's owner. Idempotent: one key per form. */
export function createAppointment(appointment: NewStaffAppointment, idempotencyKey: string): Promise<Appointment> {
  return staffApi().post<Appointment>('/staff/appointments', { body: appointment, idempotencyKey });
}

/** `POST /appointments/{id}/reschedule` — a new start, and optionally another vet. */
export function rescheduleAppointment(
  id: string,
  version: number,
  change: { starts_at: string; staff_id?: string },
): Promise<Appointment> {
  return staffApi().post<Appointment>(path(id, 'reschedule'), { body: change, ifMatch: version });
}

/** `POST /appointments/{id}/cancel` — the reason is kept on the appointment. */
export function cancelAppointment(id: string, reason?: string): Promise<Appointment> {
  return staffApi().post<Appointment>(path(id, 'cancel'), { body: { reason } });
}

/** `POST /appointments/{id}/confirm` — the owner said yes on the phone. */
export function confirmAppointment(id: string): Promise<Appointment> {
  return staffApi().post<Appointment>(path(id, 'confirm'));
}

/** `POST /staff/appointments/{id}/no-show` — only after the start time. */
export function markNoShow(id: string): Promise<NoShowResult> {
  return staffApi().post<NoShowResult>(`/staff/appointments/${encodeURIComponent(id)}/no-show`);
}
