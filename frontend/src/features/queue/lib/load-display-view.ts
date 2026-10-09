import { listStaffMembers } from '@/features/staff/api/listStaffMembers';
import { optional } from '@/lib/api/api-error';
import { getQueueBoard } from '../api/getQueueBoard';
import type { DisplayView } from '../types/queue.types';
import { buildDisplayView } from './build-display-view';

/**
 * The waiting-room screen's data: today's live board and, if this session may read the staff
 * list, the vets' names. Vet names are already public on the clinic's team page; without
 * `staff:read` the screen simply shows rooms without them, never an id.
 *
 * No pet is looked up. The contract describes no public display and grants no audience for a
 * pet's name, so the screen carries ticket numbers only (docs/features/queue.md).
 */
export async function loadDisplayView(branchId: string, examRooms: string[]): Promise<DisplayView> {
  const [board, staff] = await Promise.all([getQueueBoard(branchId), optional(listStaffMembers())]);

  const names = new Map((staff ?? []).map((member) => [member.id, member.display_name]));

  return buildDisplayView(board, examRooms, (staffId) => (staffId ? names.get(staffId) : undefined));
}
