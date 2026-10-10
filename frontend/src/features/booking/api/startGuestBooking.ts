import { publicApi } from '@/lib/api/server-client';
import type { GuestBookingStarted } from '../types/booking.types';

export interface StartGuestBookingInput {
  phone: string;
  serviceId: string;
  branchId: string;
  startsAt: string;
  staffId?: string;
}

/**
 * `POST /booking/guest/start` (docs/contract-extensions.md § 27) — anonymous. Holds the time for ten
 * minutes and texts a code to the phone. The answer is the same whoever the number belongs to.
 * Starting again lets go of the time this number held before.
 */
export function startGuestBooking({ phone, serviceId, branchId, startsAt, staffId }: StartGuestBookingInput): Promise<GuestBookingStarted> {
  return publicApi().post<GuestBookingStarted>('/booking/guest/start', {
    body: { phone, service_id: serviceId, branch_id: branchId, starts_at: startsAt, staff_id: staffId },
  });
}
