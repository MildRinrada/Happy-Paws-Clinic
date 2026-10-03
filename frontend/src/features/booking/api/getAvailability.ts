import { publicApi } from '@/lib/api/server-client';
import type { Availability } from '../types/booking.types';

/**
 * `GET /booking/availability` — public. `date` is a clinic day (`YYYY-MM-DD`). The backend already
 * leaves out times inside the clinic's notice period, beyond how far ahead it books, outside the
 * service's online hours, and on holidays (where `slots` is empty).
 */
export function getAvailability(serviceId: string, date: string, branchId: string): Promise<Availability> {
  return publicApi().get<Availability>('/booking/availability', {
    query: { service: serviceId, date, branch: branchId },
  });
}
