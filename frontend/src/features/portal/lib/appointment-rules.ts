import type { AppointmentStatus } from '@/features/appointments/types/appointment.types';

/**
 * The states in which an owner may still move or cancel an appointment from the portal: booked or
 * confirmed, before the pet has arrived. Once checked in, a change is a conversation at the counter.
 */
export const CHANGEABLE: ReadonlySet<AppointmentStatus> = new Set(['booked', 'confirmed']);

/** Booked or confirmed, and not yet started. The change cut-off itself is the backend's to apply. */
export function canChangeOnline(status: AppointmentStatus, startsAt: string, now: Date = new Date()): boolean {
  return CHANGEABLE.has(status) && Date.parse(startsAt) > now.getTime();
}
