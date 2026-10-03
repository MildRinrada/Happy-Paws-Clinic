import { staffApi } from '@/lib/api/server-client';
import type { NewPet, PetSummary } from '../types/pet.types';

/**
 * `POST /staff/pets` — "ออก HN อัตโนมัติ · ผูกเจ้าของหลัก". Needs `patient:write`. A microchip
 * already on another patient answers `422` on `microchip_number`.
 */
export function createPet(pet: NewPet): Promise<PetSummary> {
  return staffApi().post<PetSummary>('/staff/pets', { body: pet });
}
