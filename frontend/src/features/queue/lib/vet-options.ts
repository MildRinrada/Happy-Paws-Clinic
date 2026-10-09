import type { StaffMember } from '@/features/staff/types/staff.types';
import type { VetOption } from '../types/queue.types';

/**
 * The vets a ticket at this branch can be given: active members of the veterinary department (or
 * holding the `veterinarian` position) who work here. A member with no branch works everywhere,
 * the same reading the backend's BranchGuard makes, so they are included too.
 *
 * Only a pick-list. The backend does not check who is put on a ticket, so this list is the one
 * thing that keeps a receptionist's name off the "vet" line.
 */
export function vetOptions(staff: StaffMember[], branchId: string): VetOption[] {
  return staff
    .filter((member) => member.department === 'veterinary' || member.staff_type === 'veterinarian')
    .filter((member) => member.branches.length === 0 || member.branches.includes(branchId))
    .map((member) => ({ id: member.id, name: member.display_name }));
}
