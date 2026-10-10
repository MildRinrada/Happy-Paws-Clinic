import { staffApi } from '@/lib/api/server-client';
import type { PetSummary } from '../types/staff-ai.types';

/**
 * `POST /staff/ai/pets/{id}/summary`. Needs `record:read` and `ai:use`. Unchanged data is served
 * from the cache and not counted.
 */
export function summarizePet(petId: string): Promise<PetSummary> {
  return staffApi().post<PetSummary>(`/staff/ai/pets/${encodeURIComponent(petId)}/summary`);
}
