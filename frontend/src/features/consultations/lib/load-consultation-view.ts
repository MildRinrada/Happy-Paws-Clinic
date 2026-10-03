import { getCustomer } from '@/features/customers/api/getCustomer';
import { getPet } from '@/features/pets/api/getPet';
import type { PetDetail } from '@/features/pets/types/pet.types';
import { optional } from '@/lib/api/api-error';
import { getConsultation } from '../api/consultations';
import type { Consultation } from '../types/consultation.types';

export interface ConsultationView {
  consultation: Consultation;
  pet: PetDetail | null;
  ownerName?: string;
}

/**
 * The case, with the patient beside it. The patient is what puts the allergies on screen, so it is
 * read every time the case is, never cached from an earlier visit.
 */
export async function loadConsultationView(consultationId: string): Promise<ConsultationView> {
  const consultation = await getConsultation(consultationId);
  const [pet, customer] = await Promise.all([
    optional(getPet(consultation.pet_id)),
    optional(getCustomer(consultation.customer_id)),
  ]);

  return { consultation, pet, ownerName: customer?.customer.display_name };
}
