import { getPet } from '@/features/pets/api/getPet';
import type { PetDetail, PetSummary } from '@/features/pets/types/pet.types';
import { optional } from '@/lib/api/api-error';
import { listPrescriptions } from '../api/listPrescriptions';
import type { PrescriptionItem } from '../types/prescription.types';
import { undispensedFirst } from './sort-prescriptions';

export interface PetPrescriptionsView {
  /** Null when this session may not read the patient (`patient:read`); the history still shows. */
  pet: PetDetail | null;
  items: PrescriptionItem[];
  nextCursor?: string;
}

/**
 * One patient's medication history, what is still to hand over first. The patient is read beside
 * it for the name and the allergies, which the person at the counter should see before handing
 * anything over.
 */
export async function loadPetPrescriptions(petId: string, cursor?: string): Promise<PetPrescriptionsView> {
  const [page, pet] = await Promise.all([listPrescriptions({ petId, cursor }), optional(getPet(petId))]);

  return { pet, items: undispensedFirst(page.data), nextCursor: page.page.next_cursor };
}

export interface QueueRow {
  prescription: PrescriptionItem;
  pet?: PetSummary;
}

export interface DispenseQueueView {
  rows: QueueRow[];
  nextCursor?: string;
}

/**
 * The pharmacy's work list: prescriptions in the caller's branches not yet handed over. A
 * prescription carries only a pet id, so the names are read from the register and are optional,
 * as on the billing list.
 */
export async function loadDispenseQueue(cursor?: string): Promise<DispenseQueueView> {
  const page = await listPrescriptions({ dispensed: false, cursor, limit: 25 });

  const petIds = [...new Set(page.data.map((item) => item.pet_id))];
  const pets = await Promise.all(petIds.map((id) => optional(getPet(id))));
  const petById = new Map(petIds.map((id, index) => [id, pets[index]?.pet]));

  return {
    rows: page.data.map((prescription) => ({ prescription, pet: petById.get(prescription.pet_id) })),
    nextCursor: page.page.next_cursor,
  };
}
