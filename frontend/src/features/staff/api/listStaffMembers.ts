import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { StaffMember } from '../types/staff.types';

/**
 * `GET /staff/members?branch=&status=active` — needs `staff:read`. One page of the largest size:
 * the people working at one branch, for naming the vet on a queue ticket.
 */
export async function listStaffMembers(branchId?: string): Promise<StaffMember[]> {
  const page = await staffApi().get<CursorPage<StaffMember>>('/staff/members', {
    query: { branch: branchId, status: 'active', limit: 100 },
  });

  return page.data;
}
