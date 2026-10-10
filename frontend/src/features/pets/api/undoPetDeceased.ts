import { staffApi } from '@/lib/api/server-client';
import type { PetDeceasedUndone } from '../types/pet.types';

/**
 * `POST /staff/pets/{id}/deceased/undo` — needs `patient:write` and a reason. Not in the API
 * reference; see docs/contract-extensions.md § 29. Cancelled visits stay cancelled and come back
 * in the answer; the owner is not messaged.
 */
export function undoPetDeceased(petId: string, reason: string): Promise<PetDeceasedUndone> {
  return staffApi().post<PetDeceasedUndone>(`/staff/pets/${encodeURIComponent(petId)}/deceased/undo`, { body: { reason } });
}
