import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { PetSummary } from '../types/pet.types';

/**
 * `GET /staff/pets?q=` — by name, hospital number or microchip. The first few matches only: this
 * feeds a pick-list at the counter, where a tenth result means "type one more letter".
 */
export async function searchPets(query: string, limit = 8): Promise<PetSummary[]> {
  const page = await staffApi().get<CursorPage<PetSummary>>('/staff/pets', { query: { q: query, limit } });

  return page.data;
}
