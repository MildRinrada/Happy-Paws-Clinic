import { listStaffMembers } from '@/features/staff/api/listStaffMembers';
import { optional } from '@/lib/api/api-error';
import type { StaffOption } from '../components/discount/approver-field';

/**
 * The active staff of a branch as name and id, for naming a discount's approver. Null when the
 * session may not read the staff list (`staff:read`, which a cashier does not hold by default) or
 * the read is refused: the bill then shows staff ids, and the approver is typed as an id.
 */
export async function loadStaffOptions(branchId: string, canReadStaff: boolean): Promise<StaffOption[] | null> {
  if (!canReadStaff) {
    return null;
  }

  const members = await optional(listStaffMembers(branchId));

  return members?.map((member) => ({ id: member.id, name: member.display_name })) ?? null;
}
