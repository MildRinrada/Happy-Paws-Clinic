import { staffApi } from '@/lib/api/server-client';
import type { SoapDraft } from '../types/staff-ai.types';

/** `POST /staff/ai/consultations/{id}/soap-draft`. Needs `consult:write` and `ai:use`. Saves nothing. */
export function draftSoap(consultationId: string, notes: string): Promise<SoapDraft> {
  return staffApi().post<SoapDraft>(`/staff/ai/consultations/${encodeURIComponent(consultationId)}/soap-draft`, {
    body: { notes },
  });
}
