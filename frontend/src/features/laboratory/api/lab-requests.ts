import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type {
  LabCorrection,
  LabFileLink,
  LabFileType,
  LabFileUpload,
  LabListQuery,
  LabRequest,
  LabResultInput,
  NewLabRequest,
} from '../types/lab.types';

/**
 * The lab's calls. Scopes differ per step, as the contract draws them: `lab:request` to order,
 * `lab:write` to enter, correct and attach, `lab:read` to see the worklist, open a file and mark
 * results as read. None of them take an idempotency key on the backend.
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

/** `POST /staff/lab-requests/{id}/review` (§ 11), with the vet's optional note (§ 53). */
export function reviewLabResults(id: string, note?: string): Promise<LabRequest> {
  return staffApi().post<LabRequest>(path(id, 'review'), { body: note ? { note } : {} });
}

/** `POST .../results/{resultId}/amend` (§ 52): the value replaced stays on file. */
export function amendLabResult(id: string, resultId: string, correction: LabCorrection, version: number): Promise<LabRequest> {
  return staffApi().post<LabRequest>(path(id, `results/${encodeURIComponent(resultId)}/amend`), { body: correction, ifMatch: version });
}

/** `POST .../results/{resultId}/void` (§ 52): the value stays listed, struck out. */
export function voidLabResult(id: string, resultId: string, reason: string, version: number): Promise<LabRequest> {
  return staffApi().post<LabRequest>(path(id, `results/${encodeURIComponent(resultId)}/void`), { body: { reason }, ifMatch: version });
}

/** `POST .../attachments/uploads` (§ 54): a signed URL for one file, and the value that names it. */
export function requestLabFileUpload(id: string, contentType: LabFileType, sizeBytes: number): Promise<LabFileUpload> {
  return staffApi().post<LabFileUpload>(path(id, 'attachments/uploads'), { body: { content_type: contentType, size_bytes: sizeBytes } });
}

/** `POST .../attachments` (§ 54): puts an uploaded file on the request. */
export function attachLabFile(id: string, file: string, fileName: string, sizeBytes: number): Promise<LabRequest> {
  return staffApi().post<LabRequest>(path(id, 'attachments'), { body: { file, file_name: fileName, size_bytes: sizeBytes } });
}

/** `DELETE .../attachments/{attachmentId}` (§ 54): the file stays in storage, the request no longer lists it. */
export function removeLabFile(id: string, attachmentId: string, version: number): Promise<LabRequest> {
  return staffApi().delete<LabRequest>(path(id, `attachments/${encodeURIComponent(attachmentId)}`), { ifMatch: version });
}

/** `GET .../attachments/{attachmentId}/link` (§ 54): a five-minute link, written to the access log. */
export function getLabFileLink(id: string, attachmentId: string): Promise<LabFileLink> {
  return staffApi().get<LabFileLink>(path(id, `attachments/${encodeURIComponent(attachmentId)}/link`));
}
