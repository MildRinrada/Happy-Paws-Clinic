import { getPet } from '@/features/pets/api/getPet';
import { listServices } from '@/features/service-catalog/api/listServices';
import { listStaffMembers } from '@/features/staff/api/listStaffMembers';
import { optional } from '@/lib/api/api-error';
import { getQueueBoard } from '../api/getQueueBoard';
import type { QueueView, TicketView } from '../types/queue.types';

/**
 * The board, with the names a person reads.
 *
 * The API's board is ids, and it is right to be: a ticket belongs to the queue, a patient to the
 * registry, and each is guarded by its own scope. So the names are gathered here, server-side, in
 * parallel, and every one of those reads is optional. A vet assistant holds `queue:read` and
 * `patient:read` but not `staff:read`; their board names the patients and shows the vet as an id.
 * The board itself is the one read that is not optional — without it there is no page.
 */
export async function loadQueueView(branchId: string): Promise<QueueView> {
  const board = await getQueueBoard(branchId);

  const petIds = [...new Set(board.tickets.map((ticket) => ticket.pet_id))];

  const [pets, services, staff] = await Promise.all([
    Promise.all(petIds.map((id) => optional(getPet(id)))),
    optional(listServices(branchId)),

    // Not narrowed to this branch. A member with no branch assignment works at every branch — an
    // owner, or anyone at a one-site clinic — and a branch filter would leave exactly those
    // people unnamed on the board. The backend already limits the list to what this session may see.
    optional(listStaffMembers()),
  ]);

  const petById = new Map(petIds.map((id, index) => [id, pets[index] ?? null]));
  const serviceById = new Map((services ?? []).map((service) => [service.id, service.name]));
  const staffById = new Map((staff ?? []).map((member) => [member.id, member.display_name]));

  const tickets: TicketView[] = board.tickets.map((ticket) => {
    const pet = petById.get(ticket.pet_id);

    return {
      ticket,
      petName: pet?.pet.name,
      petDetail: pet ? [pet.pet.species, pet.pet.breed, pet.pet.hospital_number].filter(Boolean).join(' · ') : undefined,
      hasAllergy: (pet?.allergies.length ?? 0) > 0,
      serviceName: serviceById.get(ticket.service_id),
      vetName: ticket.staff_id ? staffById.get(ticket.staff_id) : undefined,
    };
  });

  return { board, tickets };
}
