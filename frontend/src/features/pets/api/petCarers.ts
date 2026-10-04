import { staffApi } from '@/lib/api/server-client';
import type { PetCarer } from '../types/pet.types';

const owners = (petId: string) => `/staff/pets/${encodeURIComponent(petId)}/owners`;

/**
 * `POST /staff/pets/{id}/owners` — needs `patient:write`. Adding someone already linked updates
 * whether they may read the history instead of failing.
 */
export function addCarer(petId: string, carer: { customer_id: string; may_view_history: boolean }): Promise<PetCarer> {
  return staffApi().post<PetCarer>(owners(petId), { body: carer });
}

/**
 * `DELETE /staff/pets/{id}/owners/{cid}` — needs `patient:write`. Closes the link and keeps the
 * row, so who could see what stays on record. The primary owner answers `409`.
 */
export function removeCarer(petId: string, customerId: string): Promise<void> {
  return staffApi().delete<void>(`${owners(petId)}/${encodeURIComponent(customerId)}`);
}
