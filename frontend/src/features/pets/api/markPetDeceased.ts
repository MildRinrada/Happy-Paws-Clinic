import { staffApi } from '@/lib/api/server-client';
import type { MarkDeceasedInput, PetDeceasedResult } from '../types/pet.types';

/**
 * `POST /staff/pets/{id}/deceased` — needs `patient:write`. One transaction: the record closes,
 * every future appointment is cancelled, and the owner gets one message if any were. History stays.
 */
export function markPetDeceased(petId: string, input: MarkDeceasedInput): Promise<PetDeceasedResult> {
  return staffApi().post<PetDeceasedResult>(`/staff/pets/${encodeURIComponent(petId)}/deceased`, { body: input });
}
