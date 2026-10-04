import type { StaffContext } from '@/features/account/lib/staff-context';
import { optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { listPermissions, listRoles } from '../api/manage-roles';
import { listDepartments, listStaffPage, listStaffTypes } from '../api/manage-staff';
import type { Permission } from '../types/role.types';
import type { StaffDirectory } from '../types/staff-settings.types';
import type { StaffListFilters, StaffMember } from '../types/staff.types';

export interface StaffSettingsView {
  members: StaffMember[];
  nextCursor?: string;
  /** True when open invitations were put at the top of the "all" list. */
  invitedFirst: boolean;
  permissions: Permission[] | null;
  directory: StaffDirectory;
}

/**
 * Everything the staff page draws, in one round of parallel reads.
 *
 * The "all" list without a status is the backend's "everyone employed", active and deactivated;
 * open invitations live in a separate list with its own cursor. So on the first page of "all" the
 * invitations are read too and put on top, which is where an owner looks for "who still has not
 * joined". Later pages follow the members' cursor only.
 *
 * Roles and the catalogue are `role:read`, read only when held and degraded to `null` on a 403.
 * The list itself is not optional: a refusal there (a filter the backend rejects) is the page's
 * message to show, and is left for the page to catch.
 */
export async function loadStaffSettings(context: StaffContext, filters: StaffListFilters): Promise<StaffSettingsView> {
  const canReadRoles = context.can(Scope.RoleRead);
  const withInvited = filters.status === undefined && !filters.cursor;

  const [page, invited, roles, permissions, staffTypes, departments] = await Promise.all([
    listStaffPage(filters),
    withInvited ? listStaffPage({ ...filters, status: 'invited' }) : null,
    canReadRoles ? optional(listRoles()) : null,
    canReadRoles && context.can(Scope.RoleManage) ? optional(listPermissions()) : null,
    listStaffTypes(),
    listDepartments(),
  ]);

  const membership = context.me.memberships.find((entry) => entry.type === 'staff');
  // The backend's BranchGuard reading: `*` or no branch assignment means not branch-bound.
  const branchBound = !context.scopes.includes('*') && (membership?.branches.length ?? 0) > 0;

  return {
    members: [...(invited?.data ?? []), ...page.data],
    nextCursor: page.page?.next_cursor,
    invitedFirst: (invited?.data.length ?? 0) > 0,
    permissions,
    directory: {
      roles,
      staffTypes,
      departments,
      branches: Object.fromEntries(context.branches.map((branch) => [branch.id, branch.name])),
      branchBound,
      callerScopes: context.scopes,
      selfId: context.staffId,
      canManage: context.can(Scope.StaffManage),
    },
  };
}
