import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type {
  DepartmentOption,
  InviteStaffInput,
  StaffListFilters,
  StaffMember,
  StaffTypeOption,
  UpdateStaffInput,
} from '../types/staff.types';

/**
 * The staff list as the settings page manages it. Reads are `staff:read`, every change is
 * `staff:manage`; on top of the scope the backend holds a manager to their own branches and to
 * people who hold no more than they do.
 */

/** The page size the backend allows at most. */
const PAGE_SIZE = 100;

/**
 * `GET /staff/members` — one page. `status=invited` reads the open invitations, which page
 * separately from members because the two cannot share one cursor.
 */
export function listStaffPage(filters: StaffListFilters): Promise<CursorPage<StaffMember>> {
  return staffApi().get<CursorPage<StaffMember>>('/staff/members', {
    query: { ...filters, limit: PAGE_SIZE },
  });
}

/** `GET /staff/staff-types` — positions in use, plus the four the contract suggests. */
export async function listStaffTypes(): Promise<StaffTypeOption[]> {
  const { data } = await staffApi().get<{ data: StaffTypeOption[] }>('/staff/staff-types');
  return data;
}

/** `GET /staff/departments` — the fixed list of teams. Extension, § 10. */
export async function listDepartments(): Promise<DepartmentOption[]> {
  const { data } = await staffApi().get<{ data: DepartmentOption[] }>('/staff/departments');
  return data;
}

/**
 * `POST /staff/members/invitations`. Inviting an address with an open invitation re-issues it and
 * kills the old link. The answer describes the invitation and never contains the link.
 */
export function inviteStaffMember(input: InviteStaffInput): Promise<StaffMember> {
  return staffApi().post<StaffMember>('/staff/members/invitations', { body: input });
}

/** `PATCH /staff/members/{id}` — position, department, licence, roles, branches. */
export function updateStaffMember(id: string, version: number, input: UpdateStaffInput): Promise<StaffMember> {
  return staffApi().patch<StaffMember>(`/staff/members/${encodeURIComponent(id)}`, { body: input, ifMatch: version });
}

/** `POST /staff/members/{id}/deactivate` — revokes every session; the name stays on old records. */
export function deactivateStaffMember(id: string): Promise<StaffMember> {
  return staffApi().post<StaffMember>(`/staff/members/${encodeURIComponent(id)}/deactivate`);
}
