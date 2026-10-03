import { staffApi } from '@/lib/api/server-client';
import type { ConsentPurpose, ConsentSummary, DataRequestType } from '../types/privacy.types';

/** `GET /portal/consents` — what the customer has agreed to, with the defaults filled in. */
export function getConsents(): Promise<ConsentSummary> {
  return staffApi().get<ConsentSummary>('/portal/consents');
}

/** `PUT /portal/consents` — records decisions. Purposes left out are untouched. */
export function saveConsents(consents: Partial<Record<ConsentPurpose, boolean>>): Promise<ConsentSummary> {
  return staffApi().put<ConsentSummary>('/portal/consents', { body: { consents } });
}

/** `POST /portal/data-requests` — a data-subject request under PDPA, handled by the clinic's staff. */
export function createDataRequest(type: DataRequestType, detail?: string): Promise<{ id: string; status: string }> {
  return staffApi().post('/portal/data-requests', { body: { type, detail } });
}
