import { staffApi } from '@/lib/api/server-client';
import type { PetDetail } from '../types/pet.types';

/** `GET /staff/pets/{id}` — needs `patient:read`. */
export function getPet(petId: string): Promise<PetDetail> {
  return staffApi().get<PetDetail>(`/staff/pets/${encodeURIComponent(petId)}`);
}
