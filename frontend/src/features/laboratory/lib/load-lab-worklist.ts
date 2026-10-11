import { getCustomer } from '@/features/customers/api/getCustomer';
import { getPet } from '@/features/pets/api/getPet';
import { listStaffMembers } from '@/features/staff/api/listStaffMembers';
import { optional } from '@/lib/api/api-error';
import type { CursorPage } from '@/types/pagination';
import { listLabRequests } from '../api/lab-requests';
import type { LabRequest } from '../types/lab.types';
import type { LabFilters } from './lab-filters';
import { matchesLabFilters } from './match-lab-row';
import { sortLabRows } from './sort-lab-rows';

/** One page of a tab is up to this many requests: the API's largest page. */
export const LAB_PAGE_SIZE = 100;

export interface LabRow {
  request: LabRequest;
  /** The patient, when the session may read it (`patient:read`). */
  pet?: { name: string; hospitalNumber: string; species: string };
  /** The owner's name, when the session may read customers (`customer:read`). */
  ownerName?: string;
  /** Who sent the sample, when the session may read the staff list (`staff:read`). */
  vetName?: string;
}

export interface LabWorklistView {
  rows: LabRow[];
  /** The tab's page as the API answered it, before the search and destination filters. */
  page: CursorPage<LabRequest>;
  nextCursor?: string;
}

/** What else the session may read, to name the people around a request. */
export interface LabWorklistAccess {
  canReadCustomers: boolean;
  canReadStaff: boolean;
}

/**
 * One page of a tab, up to 100 requests, with the patient, the owner and the requesting vet beside
 * each, filtered by the search box and destination, in the tab's order.
 *
 * A request carries only ids. Each name is read once per page however many rows share it (pets,
 * then their owners, then one staff list), and each read the session may not make is left out:
 * a lab technician without `patient:read` sees the pet id, which is what is on the sample tube.
 */
export async function loadLabWorklist(filters: LabFilters, branchId: string, access: LabWorklistAccess): Promise<LabWorklistView> {
  const [page, staff] = await Promise.all([
    listLabRequests({
      status: filters.tab === 'all' ? undefined : filters.tab,
      requestedBy: filters.mine ? 'me' : undefined,
      branchId,
      cursor: filters.cursor,
      limit: LAB_PAGE_SIZE,
    }),
    access.canReadStaff ? optional(listStaffMembers()) : Promise.resolve(null),
  ]);

  const petIds = [...new Set(page.data.map((request) => request.pet_id))];
  const pets = await Promise.all(petIds.map((id) => optional(getPet(id))));
  const petById = new Map(petIds.map((id, index) => [id, pets[index]?.pet]));

  const ownerIds = access.canReadCustomers
    ? [...new Set(pets.flatMap((detail) => (detail ? [detail.pet.primary_customer_id] : [])))]
    : [];
  const owners = await Promise.all(ownerIds.map((id) => optional(getCustomer(id))));
  const ownerById = new Map(ownerIds.map((id, index) => [id, owners[index]?.customer.display_name]));
  const staffById = new Map((staff ?? []).map((member) => [member.id, member.display_name]));

  const rows = page.data.map((request): LabRow => {
    const pet = petById.get(request.pet_id);

    return {
      request,
      pet: pet ? { name: pet.name, hospitalNumber: pet.hospital_number, species: pet.species } : undefined,
      ownerName: pet ? ownerById.get(pet.primary_customer_id) : undefined,
      vetName: staffById.get(request.requested_by_staff_id),
    };
  });

  return {
    rows: sortLabRows(
      rows.filter((row) => matchesLabFilters(row, filters)),
      filters.tab,
    ),
    page,
    nextCursor: page.page.next_cursor,
  };
}
