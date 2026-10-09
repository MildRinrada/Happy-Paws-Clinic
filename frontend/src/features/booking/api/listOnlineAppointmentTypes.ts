import { staffApi } from '@/lib/api/server-client';
import type { OnlineAppointmentType } from '../types/booking.types';

/**
 * `GET /portal/appointment-types` (docs/contract-extensions.md § 23) — only the types the clinic
 * lets customers book themselves, in the clinic's order. Sent with the signed-in owner's session.
 */
export async function listOnlineAppointmentTypes(): Promise<OnlineAppointmentType[]> {
  const result = await staffApi().get<{ data: OnlineAppointmentType[] }>('/portal/appointment-types');
  return result.data;
}
