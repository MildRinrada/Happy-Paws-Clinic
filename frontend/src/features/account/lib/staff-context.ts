import { cookies } from 'next/headers';
import { cache } from 'react';
import { listBranches } from '@/features/clinic/api/listBranches';
import type { Branch } from '@/features/clinic/types/clinic.types';
import { authed } from '@/lib/auth/session';
import { allows } from '@/lib/auth/scopes';
import { getMe } from '../api/getMe';
import type { Me } from '../types/account.types';

/** Which branch the console is showing. Not a credential — the backend checks the branch itself. */
export const BRANCH_COOKIE = 'hp_branch';

export interface StaffContext {
  me: Me;
  staffId: string;
  scopes: string[];
  /** The branches this session may work at, in the clinic's own order. */
  branches: Branch[];
  /** The branch in view, or `undefined` when the clinic has none this session can reach. */
  branch?: Branch;
  /** True when the backend's BranchGuard limits this session to its assigned branches. */
  branchBound: boolean;
  can: (scope: string) => boolean;
}

/**
 * Who is signed in and where they are working, resolved once per request.
 *
 * Wrapped in `cache` so the layout and the page beneath it share one `GET /me` instead of each
 * asking. A session with no staff membership is a customer's: it holds `portal` and nothing this
 * console could use, so it is treated as not signed in here.
 */
export const loadStaffContext = cache(async (): Promise<StaffContext | null> => {
  const [me, allBranches] = await Promise.all([authed(getMe), listBranches()]);
  const staff = me.memberships.find((membership) => membership.type === 'staff');

  if (!staff?.staff_id) {
    return null;
  }

  // The same reading the backend's BranchGuard makes: `*` or no assignment means not branch-bound.
  const unbound = staff.scopes.includes('*') || staff.branches.length === 0;
  const branches = unbound ? allBranches : allBranches.filter((branch) => staff.branches.includes(branch.id));

  const chosen = (await cookies()).get(BRANCH_COOKIE)?.value;

  return {
    me,
    staffId: staff.staff_id,
    scopes: staff.scopes,
    branches,
    branch: branches.find((branch) => branch.id === chosen) ?? branches[0],
    branchBound: !unbound,
    can: (scope) => allows(staff.scopes, scope),
  };
});
