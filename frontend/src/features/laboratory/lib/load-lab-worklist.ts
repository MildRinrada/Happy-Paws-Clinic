import { getPet } from '@/features/pets/api/getPet';
import { optional } from '@/lib/api/api-error';
import { listLabRequests } from '../api/lab-requests';
import type { LabRequest } from '../types/lab.types';
import type { LabFilters } from './lab-filters';

export interface LabRow {
  request: LabRequest;
  /** The patient's name and HN, when the session may read the patient (`patient:read`). */
  pet?: { name: string; hospitalNumber: string };
}

export interface LabWorklistView {
  rows: LabRow[];
  nextCursor?: string;
}

/**
 * One page of the worklist, with the patient's name beside each request.
 *
 * A request carries only a pet id. A lab technician may not hold `patient:read`, so the name is
 * optional: their list then shows the id, which is what is written on the sample tube anyway.
 */
export async function loadLabWorklist(filters: LabFilters, branchId: string): Promise<LabWorklistView> {
  const page = await listLabRequests({
    status: filters.tab === 'all' ? undefined : filters.tab,
    requestedBy: filters.mine ? 'me' : undefined,
    branchId,
    cursor: filters.cursor,
  });

  const petIds = [...new Set(page.data.map((request) => request.pet_id))];
  const pets = await Promise.all(petIds.map((id) => optional(getPet(id))));
  const petById = new Map(petIds.map((id, index) => [id, pets[index]?.pet]));

  return {
    rows: page.data.map((request) => {
      const pet = petById.get(request.pet_id);
      return { request, pet: pet ? { name: pet.name, hospitalNumber: pet.hospital_number } : undefined };
    }),
    nextCursor: page.page.next_cursor,
  };
}
