import type { Role } from './role.types';
import type { DepartmentOption, StaffTypeOption } from './staff.types';

/**
 * What every row and form on the staff page needs to know about the clinic and about the caller.
 * Built once by the page and handed down, so no client component has to ask the API.
 */
export interface StaffDirectory {
  /** `null` when this session cannot read roles (`role:read`). */
  roles: Role[] | null;
  staffTypes: StaffTypeOption[];
  departments: DepartmentOption[];
  /** Branch id to name, the branches this session can reach. */
  branches: Record<string, string>;
  /**
   * True when the caller is bound to particular branches. The backend then refuses an empty branch
   * list (it would mean "every branch", wider than the caller's own reach), so the picker is shown
   * even when there is only one branch to tick.
   */
  branchBound: boolean;
  /** The caller's own scopes: nobody may hand out a scope they do not hold. */
  callerScopes: string[];
  /** The caller's staff id: deactivating yourself is refused. */
  selfId: string;
  canManage: boolean;
}
