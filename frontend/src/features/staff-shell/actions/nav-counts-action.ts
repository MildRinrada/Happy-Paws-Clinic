'use server';

import { loadStaffContext } from '@/features/account/lib/staff-context';
import { loadNavCounts } from '../lib/load-nav-counts';
import type { NavCounts } from '../types/staff-shell.types';

/**
 * The menu's badges read again, for the browser's once-a-minute refresh. Scopes and branch are
 * worked out here from the session, never taken from the caller.
 */
export async function refreshNavCountsAction(): Promise<NavCounts> {
  const context = await loadStaffContext();

  return context ? loadNavCounts(context) : {};
}
