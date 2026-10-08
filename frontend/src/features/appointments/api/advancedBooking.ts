import { staffApi } from '@/lib/api/server-client';
import type {
  ConflictReport,
  NewRecurringSeries,
  NewWaitingListEntry,
  RecurringSeries,
  WaitingListEntry,
} from '../types/advanced-booking.types';

/**
 * The booking calls beyond one appointment at a time. Checking a time needs `appointment:read`;
 * booking a course and adding to the waiting list need `appointment:write`.
 */

/**
 * `GET /staff/appointments/conflicts` for a proposed visit. Without `ends_at` the backend uses the
 * service's length. It only reports: nothing is refused or held.
 */
export function detectConflicts(query: {
  startsAt: string;
  serviceId: string;
  branchId: string;
  vetId?: string;
  petId?: string;
}): Promise<ConflictReport> {
  return staffApi().get<ConflictReport>('/staff/appointments/conflicts', {
    query: {
      starts_at: query.startsAt,
      service_id: query.serviceId,
      branch: query.branchId,
      vet_id: query.vetId,
      pet_id: query.petId,
    },
  });
}

/**
 * `POST /staff/appointments/recurring` — every visit or none. A course that cannot be booked whole
 * is refused with `422 slot_unavailable` and one entry per failed date in `details.conflicts`.
 */
export function createRecurringSeries(series: NewRecurringSeries, idempotencyKey: string): Promise<RecurringSeries> {
  return staffApi().post<RecurringSeries>('/staff/appointments/recurring', { body: series, idempotencyKey });
}

/**
 * `POST /staff/waiting-list`. The contract has no way to list or withdraw entries; they lapse by
 * themselves after `latest_on`.
 */
export function addToWaitingList(entry: NewWaitingListEntry): Promise<WaitingListEntry> {
  return staffApi().post<WaitingListEntry>('/staff/waiting-list', { body: entry });
}
