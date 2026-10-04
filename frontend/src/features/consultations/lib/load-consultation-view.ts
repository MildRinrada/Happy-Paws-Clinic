import { getCustomer } from '@/features/customers/api/getCustomer';
import { loadCaseLabRequests } from '@/features/laboratory/lib/load-case-lab-requests';
import type { LabRequest } from '@/features/laboratory/types/lab.types';
import { getPet } from '@/features/pets/api/getPet';
import type { PetDetail } from '@/features/pets/types/pet.types';
import { optional } from '@/lib/api/api-error';
import { getConsultation } from '../api/consultations';
import type { Consultation } from '../types/consultation.types';

export interface ConsultationView {
  consultation: Consultation;
  pet: PetDetail | null;
  ownerName?: string;
  /** The lab requests sent from this case; `null` when not read (no `lab:read`). */
  labRequests: LabRequest[] | null;
}

/**
 * The case, with the patient beside it. The patient is what puts the allergies on screen, so it is
 * read every time the case is, never cached from an earlier visit.
 *
 * The case does not carry its lab requests, so they are read beside it, for a session that may
 * read them (`withLab`).
 */
export async function loadConsultationView(consultationId: string, { withLab = false } = {}): Promise<ConsultationView> {
  const consultation = await getConsultation(consultationId);
  const [pet, customer, labRequests] = await Promise.all([
    optional(getPet(consultation.pet_id)),
    optional(getCustomer(consultation.customer_id)),
    withLab ? optional(loadCaseLabRequests(consultation.pet_id, consultation.id)) : Promise.resolve(null),
  ]);

  return { consultation, pet, ownerName: customer?.customer.display_name, labRequests };
}
