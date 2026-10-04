import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { LabListQuery, LabRequest, LabResultInput, NewLabRequest } from '../types/lab.types';

/**
 * The lab's calls. Scopes differ per step, as the contract draws them: `lab:request` to order,
 * `lab:write` to enter results, `lab:read` to see the worklist and to mark results as read.
 * None of them take an idempotency key on the backend.
 */

const path = (id: string, part: string) => `/staff/lab-requests/${encodeURIComponent(id)}/${part}`;

/** `GET /staff/lab-requests`: newest first, always within the caller's branches. */
export function listLabRequests(query: LabListQuery): Promise<CursorPage<LabRequest>> {
  return staffApi().get<CursorPage<LabRequest>>('/staff/lab-requests', {
    query: {
      status: query.status,
      pet_id: query.petId,
      requested_by: query.requestedBy,
      branch: query.branchId,
      cursor: query.cursor,
      limit: query.limit ?? 25,
    },
  });
}

/** `POST /staff/lab-requests`: answers `201` with the new request, status `pending`. */
export function createLabRequest(request: NewLabRequest): Promise<LabRequest> {
  return staffApi().post<LabRequest>('/staff/lab-requests', { body: request });
}

/** `POST /staff/lab-requests/{id}/results`: adds to what is there and puts the request back in front of a vet. */
export function recordLabResults(id: string, results: LabResultInput[]): Promise<LabRequest> {
  return staffApi().post<LabRequest>(path(id, 'results'), { body: { results } });
}

/** `POST /staff/lab-requests/{id}/review`: a contract extension (docs/contract-extensions.md § 11). */
export function reviewLabResults(id: string): Promise<LabRequest> {
  return staffApi().post<LabRequest>(path(id, 'review'));
}
