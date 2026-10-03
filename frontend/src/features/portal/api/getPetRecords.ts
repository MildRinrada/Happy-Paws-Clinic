import { staffApi } from '@/lib/api/server-client';
import type { OwnerRecords } from '../types/records.types';

/**
 * `GET /portal/pets/{id}/records`. A pet that is not the caller's, or whose link does not allow
 * history, is `404`, the same as one that does not exist.
 */
export function getPetRecords(petId: string): Promise<OwnerRecords> {
  return staffApi().get<OwnerRecords>(`/portal/pets/${encodeURIComponent(petId)}/records`);
}
