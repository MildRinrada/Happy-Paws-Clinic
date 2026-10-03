import { staffApi } from '@/lib/api/server-client';
import type { Appointment } from '../types/appointment.types';

/**
 * `GET /portal/appointments?upcoming=` — the signed-in owner's own appointments. `upcoming=true`
 * is everything that starts from now on, soonest first; `false` is the history, latest first.
 *
 * `staffApi()` is the authenticated client for any signed-in session; the name predates the portal.
 */
export async function listMyAppointments(upcoming: boolean, limit = 10): Promise<Appointment[]> {
  const { data } = await staffApi().get<{ data: Appointment[] }>('/portal/appointments', {
    query: { upcoming, limit },
  });

  return data;
}
