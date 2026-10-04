import { staffApi } from '@/lib/api/server-client';
import type { TreatmentTemplate } from '../types/treatment-template.types';

/**
 * `GET /staff/templates/treatments`: "เทมเพลตแผนรักษาที่คลินิกทำไว้เอง", sorted by name. Needs
 * `consult:read`. Not paged: a clinic keeps tens of these. `species` returns that species' templates
 * plus those for any species.
 */
export async function listTreatmentTemplates(query: { species?: string; q?: string } = {}): Promise<TreatmentTemplate[]> {
  const { data } = await staffApi().get<{ data: TreatmentTemplate[] }>('/staff/templates/treatments', {
    query: { species: query.species, q: query.q },
  });

  return data;
}
