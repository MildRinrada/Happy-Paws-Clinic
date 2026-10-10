import { consultationPaths } from '@/features/consultations/lib/consultation-paths';
import { prescriptionPaths } from '@/features/prescriptions/lib/prescription-paths';
import type { PetSummaryCitation } from '../types/staff-ai.types';

/**
 * Where a citation opens. A visit and a weight open the case that recorded them, a medicine the
 * pet's medication history. Allergies, labs and vaccines are on the pet's own page already, so
 * they stay as labels.
 */
export function citationHref(citation: PetSummaryCitation, petId: string): string | undefined {
  switch (citation.kind) {
    case 'visit':
    case 'weight':
      return consultationPaths.detail(citation.id);
    case 'medicine':
      return prescriptionPaths.forPet(petId);
    default:
      return undefined;
  }
}
