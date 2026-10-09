import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type {
  ConflictReport,
  NewRecurringSeries,
  NewWaitingListEntry,
  RecurringSeries,
  WaitingListEntry,
  WaitingListItem,
  WaitingListStatus,
} from '../types/advanced-booking.types';

/**
 * The booking calls beyond one appointment at a time. Checking a time needs `appointment:read`;
 * booking a course and managing the waiting list need `appointment:write`.
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
 * `GET /staff/appointments/conflicts?date=` — every visit already booked on one clinic day at one
 * branch, checked against holidays, shifts, leave and each other.
 */
export function detectDayConflicts(date: string, branchId: string): Promise<ConflictReport> {
  return staffApi().get<ConflictReport>('/staff/appointments/conflicts', { query: { date, branch: branchId } });
}

/** `POST /staff/waiting-list`. Entries lapse by themselves after `latest_on`. */
export function addToWaitingList(entry: NewWaitingListEntry): Promise<WaitingListEntry> {
  return staffApi().post<WaitingListEntry>('/staff/waiting-list', { body: entry });
}

/**
 * `GET /staff/waiting-list` (docs/contract-extensions.md § 23), oldest first. `branch` gives the
 * entries an opening there would reach: that branch's and those for any branch.
 */
export function listWaitingList(query: {
  branchId?: string;
  serviceId?: string;
  status?: WaitingListStatus;
  cursor?: string;
  limit?: number;
}): Promise<CursorPage<WaitingListItem>> {
  return staffApi().get<CursorPage<WaitingListItem>>('/staff/waiting-list', {
    query: {
      branch: query.branchId,
      service_id: query.serviceId,
      status: query.status,
      cursor: query.cursor,
      limit: query.limit,
    },
  });
}

/** `DELETE /staff/waiting-list/{id}` — takes the pet off the list. A second remove is `404`. */
export function removeFromWaitingList(id: string): Promise<void> {
  return staffApi().delete<void>(`/staff/waiting-list/${encodeURIComponent(id)}`);
}
