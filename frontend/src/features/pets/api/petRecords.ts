import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type {
  AllergySeverity,
  NewVaccination,
  PetAllergy,
  PetTimelineEntry,
  PetVaccinations,
  VaccinationDose,
  WeightSeries,
} from '../types/pet.types';

const base = (petId: string) => `/staff/pets/${encodeURIComponent(petId)}`;

/** `GET /staff/pets/{id}/history` — needs `record:read`. Audited: the read itself is logged. */
export function getPetHistory(petId: string, cursor?: string): Promise<CursorPage<PetTimelineEntry>> {
  return staffApi().get<CursorPage<PetTimelineEntry>>(`${base(petId)}/history`, { query: { cursor, limit: 30 } });
}

/** `GET /staff/pets/{id}/vaccinations` — needs `patient:read`. */
export function getPetVaccinations(petId: string): Promise<PetVaccinations> {
  return staffApi().get<PetVaccinations>(`${base(petId)}/vaccinations`);
}

/** `GET /staff/pets/{id}/weights` — needs `patient:read`. */
export function getPetWeights(petId: string): Promise<WeightSeries> {
  return staffApi().get<WeightSeries>(`${base(petId)}/weights`);
}

/** `POST /staff/pets/{id}/allergies` — needs `record:write`. A severe allergy blocks that drug. */
export function recordAllergy(
  petId: string,
  allergy: { substance: string; severity: AllergySeverity; reaction?: string },
): Promise<PetAllergy> {
  return staffApi().post<PetAllergy>(`${base(petId)}/allergies`, { body: allergy });
}

/**
 * `POST /staff/pets/{id}/vaccinations` — needs `record:write`. The lot and the vial's expiry are
 * required: they are what a recall searches by. A deceased patient answers `409`.
 */
export function recordVaccination(petId: string, dose: NewVaccination): Promise<VaccinationDose> {
  return staffApi().post<VaccinationDose>(`${base(petId)}/vaccinations`, { body: dose });
}
