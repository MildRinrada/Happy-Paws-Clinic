import { getCustomer } from '@/features/customers/api/getCustomer';
import { optional } from '@/lib/api/api-error';
import { getPet } from '../api/getPet';
import { getPetHistory, getPetVaccinations, getPetWeights } from '../api/petRecords';
import type { PetDetail, PetTimelineEntry, PetVaccinations, WeightSeries } from '../types/pet.types';

export interface PetOwner {
  customerId: string;
  name?: string;
  phone?: string;
  isPrimary: boolean;
  mayViewHistory: boolean;
}

export interface PetProfile {
  detail: PetDetail;
  owners: PetOwner[];
  /** `null` when this session may not read the clinical timeline (`record:read`). */
  history: { entries: PetTimelineEntry[]; nextCursor?: string } | null;
  vaccinations: PetVaccinations | null;
  weights: WeightSeries | null;
}

/**
 * One patient's page. The patient is the read the page cannot do without; the timeline,
 * vaccinations, weights and owners' names are each read under their own scope and may be absent.
 * The timeline is the clinical file: reading it is audited by the backend.
 */
export async function loadPetProfile(petId: string, historyCursor?: string): Promise<PetProfile> {
  const detail = await getPet(petId);

  // Live links only: a revoked carer no longer brings the pet in.
  const carers = (detail.carers ?? []).filter((carer) => !carer.revoked_at);
  const links = carers.length > 0
    ? carers
    : [{ customer_id: detail.pet.primary_customer_id, is_primary: true, may_view_history: true }];

  const [history, vaccinations, weights, customers] = await Promise.all([
    optional(getPetHistory(petId, historyCursor)),
    optional(getPetVaccinations(petId)),
    optional(getPetWeights(petId)),
    Promise.all(links.map((link) => optional(getCustomer(link.customer_id)))),
  ]);

  return {
    detail,
    owners: links
      .map((link, index) => ({
        customerId: link.customer_id,
        name: customers[index]?.customer.display_name,
        phone: customers[index]?.customer.phone,
        isPrimary: link.is_primary,
        mayViewHistory: link.may_view_history,
      }))
      .sort((a, b) => Number(b.isPrimary) - Number(a.isPrimary)),
    history: history ? { entries: history.data, nextCursor: history.page.next_cursor } : null,
    vaccinations,
    weights,
  };
}
