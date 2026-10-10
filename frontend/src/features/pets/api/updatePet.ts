import { staffApi } from '@/lib/api/server-client';
import type { PetChanges, PetSummary } from '../types/pet.types';

/**
 * `PATCH /staff/pets/{id}` — needs `patient:write` and `If-Match`. Not in the API reference; see
 * docs/contract-extensions.md § 28. A chip already on another patient answers `422` on
 * `microchip_number`; a new neuter status on a closed record answers `409 state_conflict`.
 */
export function updatePet(petId: string, changes: PetChanges, version: number): Promise<PetSummary> {
  return staffApi().patch<PetSummary>(`/staff/pets/${encodeURIComponent(petId)}`, { body: changes, ifMatch: version });
}
